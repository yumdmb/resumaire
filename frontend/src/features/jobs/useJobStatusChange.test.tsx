import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useCallback, useState } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ToastProvider } from '../../components/ui/Toast'
import type { JobStatus } from '../../lib/types'
import { useJobStatusChange } from './useJobStatusChange'

const patchStatus = vi.fn()
vi.mock('../../lib/api', () => ({
  jobsApi: { patchStatus: (...args: unknown[]) => patchStatus(...args) },
}))

function Harness() {
  const [statuses, setStatuses] = useState<Record<string, JobStatus>>({ a: 'Saved' })
  const getStatus = useCallback((id: string) => statuses[id], [statuses])
  const setStatus = useCallback((id: string, s: JobStatus) => setStatuses((p) => ({ ...p, [id]: s })), [])
  const { changeStatus, pending } = useJobStatusChange({ getStatus, setStatus })
  return (
    <div>
      <p data-testid="status">{statuses.a}</p>
      <p data-testid="pending">{pending.has('a') ? 'yes' : 'no'}</p>
      <button type="button" onClick={() => void changeStatus('a', 'Interview')}>
        to interview
      </button>
      <button type="button" onClick={() => void changeStatus('a', 'Saved')}>
        to saved
      </button>
    </div>
  )
}

function renderHarness() {
  render(
    <ToastProvider>
      <Harness />
    </ToastProvider>,
  )
}

describe('useJobStatusChange', () => {
  beforeEach(() => patchStatus.mockReset())

  it('updates optimistically and keeps the change on success', async () => {
    patchStatus.mockResolvedValue({})
    renderHarness()
    await userEvent.click(screen.getByText('to interview'))
    expect(screen.getByTestId('status')).toHaveTextContent('Interview')
    await waitFor(() => expect(screen.getByTestId('pending')).toHaveTextContent('no'))
    expect(patchStatus).toHaveBeenCalledWith('a', 'Interview')
  })

  it('reverts and shows a toast with retry on failure', async () => {
    patchStatus.mockRejectedValueOnce(new Error('boom'))
    renderHarness()
    await userEvent.click(screen.getByText('to interview'))
    await waitFor(() => expect(screen.getByTestId('status')).toHaveTextContent('Saved'))
    expect(screen.getByText(/couldn't move this job to interview/i)).toBeInTheDocument()

    patchStatus.mockResolvedValueOnce({})
    await userEvent.click(screen.getByRole('button', { name: 'Retry' }))
    await waitFor(() => expect(patchStatus).toHaveBeenCalledTimes(2))
    expect(screen.getByTestId('status')).toHaveTextContent('Interview')
  })

  it('does nothing when the status is unchanged', async () => {
    renderHarness()
    await userEvent.click(screen.getByText('to saved'))
    expect(patchStatus).not.toHaveBeenCalled()
  })
})
