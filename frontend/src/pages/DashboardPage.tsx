import { Link } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { ApiError, hasAccessToken, jobsApi } from '../lib/api'
import { formatShortDate } from '../lib/format'
import { JOB_STATUSES, type JobStatus, type JobSummary } from '../lib/types'

type StatusFilter = JobStatus | 'All'

const FILTERS: StatusFilter[] = ['All', ...JOB_STATUSES]

const BADGE_CLASS: Record<JobStatus, string> = {
  Saved: 'badge badge-saved',
  Applied: 'badge badge-applied',
  Interview: 'badge badge-interview',
  Offer: 'badge badge-offer',
  Rejected: 'badge badge-rejected',
}

type LoadState =
  | { status: 'loading' }
  | { status: 'ready'; jobs: JobSummary[] }
  | { status: 'unauthorized' }
  | { status: 'error'; message: string }

export function DashboardPage() {
  const [activeFilter, setActiveFilter] = useState<StatusFilter>('All')
  const [reloadToken, setReloadToken] = useState(0)
  const [state, setState] = useState<LoadState>(() =>
    hasAccessToken() ? { status: 'loading' } : { status: 'unauthorized' },
  )

  useEffect(() => {
    // Skip fetch when not authenticated — initial state already reflects this
    if (state.status === 'unauthorized') return
    let cancelled = false
    ;(async () => {
      try {
        const jobs = await jobsApi.list(activeFilter)
        if (cancelled) return
        setState({ status: 'ready', jobs })
      } catch (error) {
        if (cancelled) return
        if (error instanceof ApiError && error.isUnauthorized) {
          setState({ status: 'unauthorized' })
          return
        }
        const message =
          error instanceof Error ? error.message : 'Could not load jobs'
        setState({ status: 'error', message })
      }
    })()
    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [activeFilter, reloadToken])

  function handleFilterChange(next: StatusFilter) {
    setActiveFilter(next)
    if (hasAccessToken()) {
      setState({ status: 'loading' })
    }
  }

  function handleRetry() {
    if (hasAccessToken()) {
      setState({ status: 'loading' })
      setReloadToken((token) => token + 1)
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

      {state.status === 'unauthorized' ? (
        <div className="empty-state">
          <p className="empty-state-title">Sign in to see your jobs</p>
          <p className="empty-state-body">
            Authentication is not wired into the frontend yet. Once it is, your
            tracked jobs will appear here.
          </p>
        </div>
      ) : state.status === 'loading' ? (
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
              <span className={BADGE_CLASS[job.status]}>{job.status}</span>
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
