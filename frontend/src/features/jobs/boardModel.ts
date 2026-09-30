import { JOB_STATUSES, type JobStatus, type JobSummary } from '../../lib/types'

export type SortKey = 'updated' | 'company' | 'title' | 'applied'
export type ViewMode = 'board' | 'list'

export const SORT_LABELS: Record<SortKey, string> = {
  updated: 'Recently updated',
  applied: 'Date applied',
  company: 'Company A–Z',
  title: 'Title A–Z',
}

/** Case-insensitive match on title or company. An empty query matches everything. */
export function searchJobs(jobs: JobSummary[], query: string): JobSummary[] {
  const q = query.trim().toLowerCase()
  if (!q) return jobs
  return jobs.filter(
    (job) => job.title.toLowerCase().includes(q) || job.company.toLowerCase().includes(q),
  )
}

function timeOf(value: string | null): number {
  if (!value) return 0
  const t = Date.parse(value)
  return Number.isNaN(t) ? 0 : t
}

export function sortJobs(jobs: JobSummary[], key: SortKey): JobSummary[] {
  const copy = [...jobs]
  switch (key) {
    case 'company':
      return copy.sort((a, b) => a.company.localeCompare(b.company) || a.title.localeCompare(b.title))
    case 'title':
      return copy.sort((a, b) => a.title.localeCompare(b.title) || a.company.localeCompare(b.company))
    case 'applied':
      return copy.sort(
        (a, b) => timeOf(b.dateApplied) - timeOf(a.dateApplied) || timeOf(b.updatedAt) - timeOf(a.updatedAt),
      )
    case 'updated':
    default:
      return copy.sort((a, b) => timeOf(b.updatedAt) - timeOf(a.updatedAt))
  }
}

export type JobColumns = Record<JobStatus, JobSummary[]>

/** Groups jobs by status; every status is present, empty ones as []. */
export function groupJobs(jobs: JobSummary[]): JobColumns {
  const columns = Object.fromEntries(JOB_STATUSES.map((s) => [s, [] as JobSummary[]])) as JobColumns
  for (const job of jobs) columns[job.status].push(job)
  return columns
}

/** Search, sort, and (optionally) restrict to one status. */
export function prepareJobs(
  jobs: JobSummary[],
  options: { query: string; sort: SortKey; status: JobStatus | 'All' },
): JobSummary[] {
  const matched = searchJobs(jobs, options.query)
  const scoped = options.status === 'All' ? matched : matched.filter((j) => j.status === options.status)
  return sortJobs(scoped, options.sort)
}

export function isStatus(value: unknown): value is JobStatus {
  return typeof value === 'string' && (JOB_STATUSES as readonly string[]).includes(value)
}

/** A drop counts only when it lands on a status column. */
export function resolveDrop(
  activeId: string | number,
  overId: string | number | null | undefined,
): { jobId: string; status: JobStatus } | null {
  return isStatus(overId) ? { jobId: String(activeId), status: overId } : null
}
