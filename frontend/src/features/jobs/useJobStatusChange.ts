import { useCallback, useRef, useState } from 'react'
import { useToast } from '../../components/ui/toast-context'
import { jobsApi } from '../../lib/api'
import type { JobStatus } from '../../lib/types'

interface Store {
  getStatus: (jobId: string) => JobStatus | undefined
  setStatus: (jobId: string, status: JobStatus) => void
}

/**
 * Optimistic status change shared by the board, the list and the detail page.
 * On failure the job returns to its last server-confirmed status and a toast offers a retry.
 * A response for a change that has since been superseded is ignored.
 */
export function useJobStatusChange({ getStatus, setStatus }: Store) {
  const toast = useToast()
  const [pending, setPending] = useState<ReadonlySet<string>>(new Set())
  const confirmed = useRef(new Map<string, JobStatus>())
  const latest = useRef(new Map<string, number>())
  const counter = useRef(0)

  const changeStatus = useCallback(
    async function change(jobId: string, next: JobStatus): Promise<void> {
      const current = getStatus(jobId)
      if (current === undefined || current === next) return
      if (!confirmed.current.has(jobId)) confirmed.current.set(jobId, current)

      const ticket = ++counter.current
      latest.current.set(jobId, ticket)
      setStatus(jobId, next)
      setPending((prev) => new Set(prev).add(jobId))

      try {
        await jobsApi.patchStatus(jobId, next)
        if (latest.current.get(jobId) === ticket) confirmed.current.set(jobId, next)
      } catch {
        if (latest.current.get(jobId) !== ticket) return
        const back = confirmed.current.get(jobId) ?? current
        setStatus(jobId, back)
        toast.show({
          message: `Couldn't move this job to ${next}.`,
          tone: 'error',
          action: { label: 'Retry', onAction: () => void change(jobId, next) },
        })
      } finally {
        if (latest.current.get(jobId) === ticket) {
          setPending((prev) => {
            const copy = new Set(prev)
            copy.delete(jobId)
            return copy
          })
        }
      }
    },
    [getStatus, setStatus, toast],
  )

  return { changeStatus, pending }
}
