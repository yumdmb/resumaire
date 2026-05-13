import { Link, useNavigate, useParams } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { ApiError, exportApi, jobsApi } from '../lib/api'
import { formatLongDate } from '../lib/format'
import type { JobDetail, JobStatus, TailoredResumeVersion } from '../lib/types'
import { StatusDropdown } from '../components/StatusDropdown'

type LoadState =
  | { status: 'loading' }
  | { status: 'ready'; job: JobDetail }
  | { status: 'not_found' }
  | { status: 'error'; message: string }

export function JobDetailPage() {
  const { jobId = '' } = useParams()
  const navigate = useNavigate()
  const [reloadToken, setReloadToken] = useState(0)
  const [state, setState] = useState<LoadState>({ status: 'loading' })
  const [isDeleting, setIsDeleting] = useState(false)
  const [isPatchingStatus, setIsPatchingStatus] = useState(false)
  const [exportingVersions, setExportingVersions] = useState<Set<string>>(new Set())
  const [attachingVersion, setAttachingVersion] = useState<string | null>(null)

  useEffect(() => {
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
  }, [jobId, reloadToken])

  function handleRetry() {
    setState({ status: 'loading' })
    setReloadToken((token) => token + 1)
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

  async function handleStatusChange(newStatus: JobStatus) {
    if (state.status !== 'ready') return

    const previousJob = state.job
    setState({ status: 'ready', job: { ...state.job, status: newStatus } })
    setIsPatchingStatus(true)

    try {
      await jobsApi.patchStatus(state.job.id, newStatus)
    } catch {
      setState({ status: 'ready', job: previousJob })
    } finally {
      setIsPatchingStatus(false)
    }
  }

  async function handleExportPdf(versionId: string) {
    setExportingVersions((prev) => new Set(prev).add(versionId))
    try {
      const blob = await exportApi.exportTailoredPdf(versionId)
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `resume-${versionId}.pdf`
      document.body.appendChild(a)
      a.click()
      document.body.removeChild(a)
      URL.revokeObjectURL(url)
    } catch {
      // Silently fail; user can retry
    } finally {
      setExportingVersions((prev) => {
        const next = new Set(prev)
        next.delete(versionId)
        return next
      })
    }
  }

  async function handleAttachVersion(versionId: string) {
    if (state.status !== 'ready') return

    const previousJob = state.job
    setState({
      status: 'ready',
      job: { ...state.job, selectedTailoredResumeId: versionId },
    })
    setAttachingVersion(versionId)

    try {
      await jobsApi.attachTailoredResume(state.job.id, state.job, versionId)
    } catch {
      setState({ status: 'ready', job: previousJob })
    } finally {
      setAttachingVersion(null)
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
  const selectedVersion = job.tailoredResumeVersions.find(
    (v) => v.id === job.selectedTailoredResumeId,
  )

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
          <StatusDropdown
            value={status}
            onChange={handleStatusChange}
            disabled={isPatchingStatus}
          />
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
          <Link
            to={`/tailor?jobId=${job.id}`}
            className="btn btn-primary"
          >
            Tailor resume
          </Link>
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
                to={`/tailor?jobId=${job.id}`}
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
                    <VersionRow
                      key={version.id}
                      version={version}
                      jobId={job.id}
                      isSelected={version.id === job.selectedTailoredResumeId}
                      isExporting={exportingVersions.has(version.id)}
                      isAttaching={attachingVersion === version.id}
                      onExport={handleExportPdf}
                      onAttach={handleAttachVersion}
                    />
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
                  <StatusDropdown
                    value={status}
                    onChange={handleStatusChange}
                    disabled={isPatchingStatus}
                  />
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
              <span className="card-title">Attached resume</span>
            </div>
            <div className="card-body">
              {selectedVersion ? (
                <div className="attached-resume">
                  <div className="attached-resume-name">
                    {selectedVersion.name ?? `Version ${selectedVersion.versionNumber}`}
                  </div>
                  <div className="attached-resume-actions">
                    <Link
                      to={`/resume/preview/${selectedVersion.id}?jobId=${job.id}`}
                      className="btn btn-secondary"
                      style={{ fontSize: 12, padding: '4px 10px' }}
                    >
                      Preview
                    </Link>
                    <button
                      type="button"
                      className="btn btn-secondary"
                      style={{ fontSize: 12, padding: '4px 10px' }}
                      onClick={() => handleExportPdf(selectedVersion.id)}
                      disabled={exportingVersions.has(selectedVersion.id)}
                    >
                      {exportingVersions.has(selectedVersion.id) ? 'Exporting' : 'Export PDF'}
                    </button>
                  </div>
                </div>
              ) : (
                <p className="prose-muted">No version attached to this application.</p>
              )}
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

interface VersionRowProps {
  version: TailoredResumeVersion
  jobId: string
  isSelected: boolean
  isExporting: boolean
  isAttaching: boolean
  onExport: (versionId: string) => void
  onAttach: (versionId: string) => void
}

function VersionRow({
  version,
  jobId,
  isSelected,
  isExporting,
  isAttaching,
  onExport,
  onAttach,
}: VersionRowProps) {
  return (
    <li className="version-row">
      <div>
        <div className="version-name">
          {version.name ?? `Version ${version.versionNumber}`}
          {isSelected && <span className="version-selected-indicator">Selected</span>}
        </div>
        <div className="version-meta">
          v{version.versionNumber} · saved {formatLongDate(version.createdAt)}
        </div>
      </div>
      <div className="version-actions">
        <Link
          to={`/resume/preview/${version.id}?jobId=${jobId}`}
          className="btn btn-secondary"
          style={{ fontSize: 12, padding: '4px 10px' }}
        >
          Preview
        </Link>
        <button
          type="button"
          className="btn btn-secondary"
          style={{ fontSize: 12, padding: '4px 10px' }}
          onClick={() => onExport(version.id)}
          disabled={isExporting}
        >
          {isExporting ? 'Exporting' : 'Export PDF'}
        </button>
        {!isSelected && (
          <button
            type="button"
            className="btn btn-secondary"
            style={{ fontSize: 12, padding: '4px 10px' }}
            onClick={() => onAttach(version.id)}
            disabled={isAttaching}
          >
            {isAttaching ? 'Attaching' : 'Use for application'}
          </button>
        )}
      </div>
    </li>
  )
}
