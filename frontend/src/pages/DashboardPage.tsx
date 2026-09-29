import { Link } from 'react-router-dom'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { ApiError, jobsApi } from '../lib/api'
import { formatShortDate } from '../lib/format'
import { usePersistedState } from '../lib/usePersistedState'
import { JOB_STATUSES, type JobStatus, type JobSummary } from '../lib/types'
import { StatusMenu } from '../components/StatusMenu'
import { ButtonLink, Button } from '../components/ui/Button'
import { EmptyState } from '../components/ui/EmptyState'
import { PageHeader } from '../components/ui/PageHeader'
import { Skeleton } from '../components/ui/Skeleton'
import { JobBoard } from '../features/jobs/JobBoard'
import {
  SORT_LABELS,
  groupJobs,
  prepareJobs,
  type SortKey,
  type ViewMode,
} from '../features/jobs/boardModel'
import { useJobStatusChange } from '../features/jobs/useJobStatusChange'

type StatusFilter = JobStatus | 'All'

const FILTERS: StatusFilter[] = ['All', ...JOB_STATUSES]

const isView = (v: string): v is ViewMode => v === 'board' || v === 'list'
const isSort = (v: string): v is SortKey => v in SORT_LABELS

type LoadState =
  | { status: 'loading' }
  | { status: 'ready'; jobs: JobSummary[] }
  | { status: 'error'; message: string }

export function DashboardPage() {
  const [view, setView] = usePersistedState<ViewMode>('resumaire:jobs:view', 'board', isView)
  const [sort, setSort] = usePersistedState<SortKey>('resumaire:jobs:sort', 'updated', isSort)
  const [activeFilter, setActiveFilter] = useState<StatusFilter>('All')
  const [query, setQuery] = useState('')
  const [reloadToken, setReloadToken] = useState(0)
  const [state, setState] = useState<LoadState>({ status: 'loading' })

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        // The board needs every status at once, so filtering happens client-side.
        const jobs = await jobsApi.list('All')
        if (cancelled) return
        setState({ status: 'ready', jobs })
      } catch (error) {
        if (cancelled) return
        const message = error instanceof ApiError ? error.message : 'Could not load jobs'
        setState({ status: 'error', message })
      }
    })()
    return () => {
      cancelled = true
    }
  }, [reloadToken])

  const jobs = useMemo(() => (state.status === 'ready' ? state.jobs : []), [state])

  const jobsRef = useRef(jobs)
  useEffect(() => {
    jobsRef.current = jobs
  }, [jobs])

  const getStatus = useCallback(
    (jobId: string) => jobsRef.current.find((j) => j.id === jobId)?.status,
    [],
  )
  const setStatus = useCallback((jobId: string, status: JobStatus) => {
    setState((current) =>
      current.status === 'ready'
        ? { status: 'ready', jobs: current.jobs.map((j) => (j.id === jobId ? { ...j, status } : j)) }
        : current,
    )
  }, [])
  const { changeStatus, pending } = useJobStatusChange({ getStatus, setStatus })

  function handleRetry() {
    setState({ status: 'loading' })
    setReloadToken((token) => token + 1)
  }

  // Search and sort apply to both views; the status filter decides which columns/rows are shown.
  const searched = useMemo(() => prepareJobs(jobs, { query, sort, status: 'All' }), [jobs, query, sort])
  const columns = useMemo(() => groupJobs(searched), [searched])
  const visibleStatuses = activeFilter === 'All' ? JOB_STATUSES : [activeFilter]
  const listJobs = useMemo(
    () => (activeFilter === 'All' ? searched : searched.filter((j) => j.status === activeFilter)),
    [searched, activeFilter],
  )

  const total = jobs.length
  const hasNoJobs = state.status === 'ready' && total === 0
  const subtitle =
    state.status === 'ready'
      ? `${total} ${total === 1 ? 'application' : 'applications'}`
      : state.status === 'loading'
        ? 'Loading'
        : ''

  return (
    <div className={`page${view === 'board' ? ' page--wide' : ''}`}>
      <PageHeader
        title="Jobs"
        subtitle={subtitle}
        actions={
          <ButtonLink to="/jobs/new" variant="primary">
            Add job
          </ButtonLink>
        }
      />

      <div className="jobs-toolbar">
        <div className="jobs-search">
          <svg width="14" height="14" viewBox="0 0 14 14" fill="none" aria-hidden="true">
            <circle cx="6" cy="6" r="4.2" stroke="currentColor" strokeWidth="1.4" />
            <path d="M9.2 9.2L12 12" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" />
          </svg>
          <input
            type="search"
            className="form-input"
            placeholder="Search title or company"
            aria-label="Search jobs"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
          />
        </div>
        <select
          className="form-input jobs-sort"
          aria-label="Sort jobs"
          value={sort}
          onChange={(event) => setSort(event.target.value as SortKey)}
        >
          {(Object.keys(SORT_LABELS) as SortKey[]).map((key) => (
            <option key={key} value={key}>
              {SORT_LABELS[key]}
            </option>
          ))}
        </select>
        <div className="view-toggle" role="group" aria-label="View">
          <button type="button" aria-pressed={view === 'board'} onClick={() => setView('board')}>
            Board
          </button>
          <button type="button" aria-pressed={view === 'list'} onClick={() => setView('list')}>
            List
          </button>
        </div>
      </div>

      <div className="filter-tabs" role="group" aria-label="Filter by status">
        {FILTERS.map((filter) => (
          <button
            key={filter}
            type="button"
            aria-pressed={activeFilter === filter}
            className={`filter-tab${activeFilter === filter ? ' active' : ''}`}
            onClick={() => setActiveFilter(filter)}
          >
            {filter}
          </button>
        ))}
      </div>

      {state.status === 'loading' ? (
        <DashboardSkeleton />
      ) : state.status === 'error' ? (
        <EmptyState
          title="Could not load jobs"
          body={state.message}
          action={<Button onClick={handleRetry}>Retry</Button>}
        />
      ) : hasNoJobs ? (
        <EmptyState
          title="No jobs yet"
          body="Add the first role you want to track."
          action={
            <ButtonLink to="/jobs/new" variant="secondary">
              Add job
            </ButtonLink>
          }
        />
      ) : view === 'board' ? (
        <JobBoard columns={columns} visible={visibleStatuses} pending={pending} onMove={changeStatus} />
      ) : listJobs.length === 0 ? (
        <EmptyState
          title={query.trim() ? 'No matches' : `No ${activeFilter.toLowerCase()} jobs`}
          body={
            query.trim()
              ? 'Try a different search.'
              : `Jobs you mark as ${activeFilter} will appear here.`
          }
        />
      ) : (
        <div className="job-list" role="list">
          {listJobs.map((job) => (
            <div key={job.id} className="job-row" role="listitem">
              <div>
                <Link to={`/jobs/${job.id}`} className="job-row-title job-row-link">
                  {job.title}
                </Link>
                <div className="job-row-company">{job.company}</div>
              </div>
              <StatusMenu
                value={job.status}
                onChange={(next) => changeStatus(job.id, next)}
                disabled={pending.has(job.id)}
                label={`${job.title} at ${job.company}`}
              />
              <span className="job-row-date">
                {formatShortDate(job.dateApplied ?? job.updatedAt)}
              </span>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

function DashboardSkeleton() {
  return (
    <div className="board" aria-busy="true">
      {JOB_STATUSES.map((status) => (
        <div key={status} className="board-column">
          <div className="board-column-head">
            <Skeleton width={70} />
          </div>
          <div className="board-column-body">
            <Skeleton variant="card" style={{ height: 84 }} />
            <Skeleton variant="card" style={{ height: 84 }} />
          </div>
        </div>
      ))}
    </div>
  )
}
