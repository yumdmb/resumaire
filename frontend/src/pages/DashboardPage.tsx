import { Link } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { ApiError, jobsApi } from '../lib/api'
import { formatShortDate } from '../lib/format'
import { JOB_STATUSES, type JobStatus, type JobSummary } from '../lib/types'
import { StatusDropdown } from '../components/StatusDropdown'

type StatusFilter = JobStatus | 'All'

const FILTERS: StatusFilter[] = ['All', ...JOB_STATUSES]

type LoadState =
  | { status: 'loading' }
  | { status: 'ready'; jobs: JobSummary[] }
  | { status: 'error'; message: string }

export function DashboardPage() {
  const [activeFilter, setActiveFilter] = useState<StatusFilter>('All')
  const [reloadToken, setReloadToken] = useState(0)
  const [state, setState] = useState<LoadState>({ status: 'loading' })
  const [patchingJobs, setPatchingJobs] = useState<Set<string>>(new Set())

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const jobs = await jobsApi.list(activeFilter)
        if (cancelled) return
        setState({ status: 'ready', jobs })
      } catch (error) {
        if (cancelled) return
        const message =
          error instanceof ApiError ? error.message : 'Could not load jobs'
        setState({ status: 'error', message })
      }
    })()
    return () => {
      cancelled = true
    }
  }, [activeFilter, reloadToken])

  function handleFilterChange(next: StatusFilter) {
    setActiveFilter(next)
    setState({ status: 'loading' })
  }

  function handleRetry() {
    setState({ status: 'loading' })
    setReloadToken((token) => token + 1)
  }

  async function handleStatusChange(jobId: string, newStatus: JobStatus) {
    if (state.status !== 'ready') return

    const previousJobs = state.jobs
    // Optimistic update
    setState({
      status: 'ready',
      jobs: state.jobs.map((j) =>
        j.id === jobId ? { ...j, status: newStatus } : j,
      ),
    })
    setPatchingJobs((prev) => new Set(prev).add(jobId))

    try {
      await jobsApi.patchStatus(jobId, newStatus)
    } catch {
      // Revert on failure
      setState({ status: 'ready', jobs: previousJobs })
    } finally {
      setPatchingJobs((prev) => {
        const next = new Set(prev)
        next.delete(jobId)
        return next
      })
    }
  }

  const jobs = state.status === 'ready' ? state.jobs : []
  const total = state.status === 'ready' ? state.jobs.length : 0

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1 className="page-title">Jobs</h1>
          <p className="page-subtitle">
            {state.status === 'ready'
              ? `${total} ${total === 1 ? 'application' : 'applications'}`
              : state.status === 'loading'
                ? 'Loading'
                : ''}
          </p>
        </div>
        <Link to="/jobs/new" className="btn btn-primary">
          Add job
        </Link>
      </div>

      <div className="filter-tabs" role="tablist" aria-label="Filter by status">
        {FILTERS.map((filter) => (
          <button
            key={filter}
            type="button"
            role="tab"
            aria-selected={activeFilter === filter}
            className={`filter-tab${activeFilter === filter ? ' active' : ''}`}
            onClick={() => handleFilterChange(filter)}
          >
            {filter}
          </button>
        ))}
      </div>

      {state.status === 'loading' ? (
        <DashboardSkeleton />
      ) : state.status === 'error' ? (
        <div className="empty-state">
          <p className="empty-state-title">Could not load jobs</p>
          <p className="empty-state-body">{state.message}</p>
          <button
            type="button"
            className="btn btn-secondary"
            onClick={handleRetry}
          >
            Retry
          </button>
        </div>
      ) : jobs.length === 0 ? (
        <div className="empty-state">
          <p className="empty-state-title">
            {activeFilter === 'All'
              ? 'No jobs yet'
              : `No ${activeFilter.toLowerCase()} jobs`}
          </p>
          <p className="empty-state-body">
            {activeFilter === 'All'
              ? 'Add the first role you want to track.'
              : `Jobs you mark as ${activeFilter} will appear here.`}
          </p>
          {activeFilter === 'All' ? (
            <Link to="/jobs/new" className="btn btn-secondary">
              Add job
            </Link>
          ) : null}
        </div>
      ) : (
        <div className="job-list" role="list">
          {jobs.map((job) => (
            <Link
              key={job.id}
              to={`/jobs/${job.id}`}
              className="job-row"
              role="listitem"
            >
              <div>
                <div className="job-row-title">{job.title}</div>
                <div className="job-row-company">{job.company}</div>
              </div>
              <StatusDropdown
                value={job.status}
                onChange={(newStatus) => handleStatusChange(job.id, newStatus)}
                disabled={patchingJobs.has(job.id)}
              />
              <span className="job-row-date">
                {formatShortDate(job.dateApplied ?? job.updatedAt)}
              </span>
            </Link>
          ))}
        </div>
      )}
    </div>
  )
}

function DashboardSkeleton() {
  return (
    <div className="job-list" aria-busy="true">
      {Array.from({ length: 3 }).map((_, index) => (
        <div key={index} className="job-row" aria-hidden="true">
          <div>
            <div className="skeleton skeleton-line" style={{ width: '42%' }} />
            <div
              className="skeleton skeleton-line"
              style={{ width: '24%', marginTop: 6 }}
            />
          </div>
          <div className="skeleton skeleton-pill" />
          <div className="skeleton skeleton-line" style={{ width: 40 }} />
        </div>
      ))}
    </div>
  )
}
