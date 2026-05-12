import { Link, useNavigate, useParams } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { ApiError, hasAccessToken, jobsApi } from '../lib/api'
import { formatLongDate } from '../lib/format'
import type { JobDetail, JobStatus } from '../lib/types'

const BADGE_CLASS: Record<JobStatus, string> = {
  Saved: 'badge badge-saved',
  Applied: 'badge badge-applied',
  Interview: 'badge badge-interview',
  Offer: 'badge badge-offer',
  Rejected: 'badge badge-rejected',
}

type LoadState =
  | { status: 'loading' }
  | { status: 'ready'; job: JobDetail }
  | { status: 'unauthorized' }
  | { status: 'not_found' }
  | { status: 'error'; message: string }

export function JobDetailPage() {
  const { jobId = '' } = useParams()
  const navigate = useNavigate()
  const [reloadToken, setReloadToken] = useState(0)
  const [state, setState] = useState<LoadState>(() =>
    hasAccessToken() ? { status: 'loading' } : { status: 'unauthorized' },
  )
  const [isDeleting, setIsDeleting] = useState(false)

  useEffect(() => {
    // Skip fetch when not authenticated — initial state already reflects this
    if (state.status === 'unauthorized') return
    let cancelled = false
    ;(async () => {
      try {
        const job = await jobsApi.get(jobId)
        if (cancelled) return
        if (!job) {
          setState({ status: 'not_found' })
          return
        }
        setState({ status: 'ready', job })
      } catch (error) {
        if (cancelled) return
        if (error instanceof ApiError) {
          if (error.isUnauthorized) {
            setState({ status: 'unauthorized' })
            return
          }
          if (error.isNotFound) {
            setState({ status: 'not_found' })
            return
          }
          setState({ status: 'error', message: error.message })
          return
        }
        setState({
          status: 'error',
          message:
            error instanceof Error ? error.message : 'Could not load this job',
        })
      }
    })()
    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [jobId, reloadToken])

  function handleRetry() {
    if (hasAccessToken()) {
      setState({ status: 'loading' })
      setReloadToken((token) => token + 1)
    }
  }

  async function handleDelete() {
    if (state.status !== 'ready') return
    const confirmed = window.confirm(
      `Delete "${state.job.title} at ${state.job.company}"? This cannot be undone.`,
    )
    if (!confirmed) return

    setIsDeleting(true)
    try {
      await jobsApi.delete(state.job.id)
      navigate('/', { replace: true })
    } catch (error) {
      setIsDeleting(false)
      const message =
        error instanceof Error ? error.message : 'Could not delete this job'
      window.alert(message)
    }
  }

  if (state.status === 'loading') {
    return (
      <div className="page" aria-busy="true">
        <div className="page-header">
          <div>
            <Link to="/" className="back-link">
              ← Jobs
            </Link>
            <div
              className="skeleton skeleton-heading"
              style={{ width: '40%', marginTop: 8 }}
            />
            <div
              className="skeleton skeleton-line"
              style={{ width: '24%', marginTop: 10 }}
            />
          </div>
        </div>
        <div className="detail-grid">
          <div className="detail-main">
            <div className="skeleton skeleton-card" />
            <div className="skeleton skeleton-card" />
          </div>
          <div className="detail-aside">
            <div className="skeleton skeleton-card" />
          </div>
        </div>
      </div>
    )
  }

  if (state.status === 'unauthorized') {
    return (
      <div className="page">
        <div className="page-header">
          <div>
            <Link to="/" className="back-link">
              ← Jobs
            </Link>
            <h1 className="page-title" style={{ marginTop: 4 }}>
              Sign in required
            </h1>
          </div>
        </div>
        <div className="empty-state">
          <p className="empty-state-title">You need to sign in</p>
          <p className="empty-state-body">
            Authentication is not wired into the frontend yet. Job details are
            scoped to the signed-in user.
          </p>
        </div>
      </div>
    )
  }

  if (state.status === 'not_found') {
    return (
      <div className="page">
        <div className="page-header">
          <div>
            <Link to="/" className="back-link">
              ← Jobs
            </Link>
            <h1 className="page-title" style={{ marginTop: 4 }}>
              Job not found
            </h1>
          </div>
        </div>
        <div className="empty-state">
          <p className="empty-state-title">No job with this ID</p>
          <p className="empty-state-body">
            It may have been removed, or the link is incorrect.
          </p>
          <Link to="/" className="btn btn-secondary">
            Back to jobs
          </Link>
        </div>
      </div>
    )
  }

  if (state.status === 'error') {
    return (
      <div className="page">
        <div className="page-header">
          <div>
            <Link to="/" className="back-link">
              ← Jobs
            </Link>
            <h1 className="page-title" style={{ marginTop: 4 }}>
              Could not load this job
            </h1>
          </div>
        </div>
        <div className="empty-state">
          <p className="empty-state-title">{state.message}</p>
          <button type="button" className="btn btn-secondary" onClick={handleRetry}>
            Retry
          </button>
        </div>
      </div>
    )
  }

  const { job } = state
  const status = job.status

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <Link to="/" className="back-link">
            ← Jobs
          </Link>
          <h1 className="page-title" style={{ marginTop: 4 }}>
            {job.title}
          </h1>
          <p className="page-subtitle">{job.company}</p>
        </div>
        <div className="page-actions">
          <span className={BADGE_CLASS[status]}>{status}</span>
          <Link to={`/jobs/${job.id}/edit`} className="btn btn-secondary">
            Edit
          </Link>
          <button
            type="button"
            className="btn btn-secondary"
            onClick={handleDelete}
            disabled={isDeleting}
          >
            {isDeleting ? 'Deleting' : 'Delete'}
          </button>
          <button type="button" className="btn btn-primary">
            Tailor resume
          </button>
        </div>
      </div>

      <div className="detail-grid">
        <div className="detail-main">
          <div className="card">
            <div className="card-header">
              <span className="card-title">Job description</span>
            </div>
            <div className="card-body">
              {job.description.trim().length > 0 ? (
                <p className="prose">{job.description}</p>
              ) : (
                <p className="prose-muted">No description provided.</p>
              )}
            </div>
          </div>

          <div className="card">
            <div className="card-header">
              <span className="card-title">Tailored versions</span>
              <Link
                to="/tailor"
                className="btn btn-secondary"
                style={{ fontSize: 12, padding: '4px 10px' }}
              >
                New version
              </Link>
            </div>
            <div className="card-body">
              {job.tailoredResumeVersions.length === 0 ? (
                <div className="empty-inline">
                  <p className="empty-state-title">No tailored versions yet</p>
                  <p className="empty-state-body">
                    Tailor your resume for this role to save a version.
                  </p>
                </div>
              ) : (
                <ul className="version-list">
                  {job.tailoredResumeVersions.map((version) => (
                    <li key={version.id} className="version-row">
                      <div>
                        <div className="version-name">
                          {version.name ?? `Version ${version.versionNumber}`}
                        </div>
                        <div className="version-meta">
                          v{version.versionNumber} · saved{' '}
                          {formatLongDate(version.createdAt)}
                        </div>
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </div>
        </div>

        <div className="detail-aside">
          <div className="card">
            <div className="card-header">
              <span className="card-title">Details</span>
            </div>
            <div className="card-body">
              <div className="meta-list">
                <div className="meta-row">
                  <span className="meta-label">Status</span>
                  <span className={BADGE_CLASS[status]}>{status}</span>
                </div>
                <div className="meta-row">
                  <span className="meta-label">Added</span>
                  <span className="meta-value">
                    {formatLongDate(job.createdAt)}
                  </span>
                </div>
                {job.dateApplied ? (
                  <div className="meta-row">
                    <span className="meta-label">Applied</span>
                    <span className="meta-value">
                      {formatLongDate(job.dateApplied)}
                    </span>
                  </div>
                ) : null}
                {job.link ? (
                  <div className="meta-row">
                    <span className="meta-label">Posting</span>
                    <a
                      href={job.link}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="meta-link"
                    >
                      View ↗
                    </a>
                  </div>
                ) : null}
              </div>
            </div>
          </div>

          <div className="card">
            <div className="card-header">
              <span className="card-title">Notes</span>
            </div>
            <div className="card-body">
              {job.notes && job.notes.trim().length > 0 ? (
                <p className="prose">{job.notes}</p>
              ) : (
                <p className="prose-muted">No notes yet.</p>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
