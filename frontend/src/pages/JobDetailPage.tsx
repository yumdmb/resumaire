import { Link, useNavigate, useParams } from 'react-router-dom'
import { useCallback, useEffect, useRef, useState } from 'react'
import { ApiError, exportApi, jobsApi } from '../lib/api'
import { formatLongDate } from '../lib/format'
import type { JobDetail, JobStatus, TailoredResumeVersion } from '../lib/types'
import { StatusMenu } from '../components/StatusMenu'
import { Button, ButtonLink } from '../components/ui/Button'
import { Card } from '../components/ui/Card'
import { EmptyState } from '../components/ui/EmptyState'
import { Skeleton } from '../components/ui/Skeleton'
import { StatusTimeline } from '../features/jobs/StatusTimeline'
import { useJobStatusChange } from '../features/jobs/useJobStatusChange'

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
  const [exportingVersions, setExportingVersions] = useState<Set<string>>(new Set())
  const [attachingVersion, setAttachingVersion] = useState<string | null>(null)
  const [exportError, setExportError] = useState<string | null>(null)

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

  const jobRef = useRef<JobDetail | null>(null)
  useEffect(() => {
    jobRef.current = state.status === 'ready' ? state.job : null
  }, [state])
  const getStatus = useCallback(
    (id: string) => (jobRef.current?.id === id ? jobRef.current.status : undefined),
    [],
  )
  const setStatus = useCallback((id: string, next: JobStatus) => {
    setState((current) =>
      current.status === 'ready' && current.job.id === id
        ? { status: 'ready', job: { ...current.job, status: next } }
        : current,
    )
  }, [])
  const { changeStatus, pending } = useJobStatusChange({ getStatus, setStatus })

  async function handleExportPdf(versionId: string) {
    setExportError(null)
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
    } catch (error) {
      setExportError(error instanceof Error ? error.message : 'PDF export failed')
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
            <Skeleton variant="heading" width="40%" style={{ marginTop: 8 }} />
            <Skeleton width="24%" style={{ marginTop: 10 }} />
          </div>
        </div>
        <div className="detail-grid">
          <div className="detail-main">
            <Skeleton variant="card" />
            <Skeleton variant="card" />
          </div>
          <div className="detail-aside">
            <Skeleton variant="card" />
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
        <EmptyState
          title="No job with this ID"
          body="It may have been removed, or the link is incorrect."
          action={
            <ButtonLink to="/" variant="secondary">
              Back to jobs
            </ButtonLink>
          }
        />
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
        <EmptyState
          title={state.message}
          action={<Button onClick={handleRetry}>Retry</Button>}
        />
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
          <p className="detail-company">{job.company}</p>
          <h1 className="page-title">{job.title}</h1>
        </div>
        <div className="page-actions">
          <ButtonLink to={`/jobs/${job.id}/edit`} variant="secondary">
            Edit
          </ButtonLink>
          <Button variant="danger" onClick={handleDelete} disabled={isDeleting}>
            {isDeleting ? 'Deleting' : 'Delete'}
          </Button>
          <ButtonLink to={`/tailor?jobId=${job.id}`} variant="primary">
            Tailor resume
          </ButtonLink>
        </div>
      </div>

      <div className="detail-status">
        <StatusTimeline status={status} />
        <StatusMenu
          value={status}
          onChange={(next) => changeStatus(job.id, next)}
          disabled={pending.has(job.id)}
          label={job.title}
        />
      </div>

      {exportError && (
        <div className="form-alert" role="alert" style={{ marginBottom: 16 }}>
          {exportError}
        </div>
      )}

      <div className="detail-grid">
        <div className="detail-main">
          <Card title="Job description">
            {job.description.trim().length > 0 ? (
              <p className="prose">{job.description}</p>
            ) : (
              <p className="prose-muted">No description provided.</p>
            )}
          </Card>

          <Card
            title="Tailored versions"
            actions={
              <ButtonLink to={`/tailor?jobId=${job.id}`} variant="secondary" small>
                New version
              </ButtonLink>
            }
          >
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
          </Card>
        </div>

        <div className="detail-aside">
          <Card title="Details">
            <div className="meta-list">
              <div className="meta-row">
                <span className="meta-label">Added</span>
                <span className="meta-value">{formatLongDate(job.createdAt)}</span>
              </div>
              {job.dateApplied ? (
                <div className="meta-row">
                  <span className="meta-label">Applied</span>
                  <span className="meta-value">{formatLongDate(job.dateApplied)}</span>
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
          </Card>

          <Card title="Attached resume">
            {selectedVersion ? (
              <div className="attached-resume">
                <div className="attached-resume-name">
                  {selectedVersion.name ?? `Version ${selectedVersion.versionNumber}`}
                </div>
                <div className="attached-resume-actions">
                  <ButtonLink
                    to={`/resume/preview/${selectedVersion.id}?jobId=${job.id}`}
                    variant="secondary"
                    small
                  >
                    Preview
                  </ButtonLink>
                  <Button
                    small
                    onClick={() => handleExportPdf(selectedVersion.id)}
                    disabled={exportingVersions.has(selectedVersion.id)}
                  >
                    {exportingVersions.has(selectedVersion.id) ? 'Exporting' : 'Export PDF'}
                  </Button>
                </div>
              </div>
            ) : (
              <p className="prose-muted">No version attached to this application.</p>
            )}
          </Card>

          <Card title="Notes">
            {job.notes && job.notes.trim().length > 0 ? (
              <p className="prose">{job.notes}</p>
            ) : (
              <p className="prose-muted">No notes yet.</p>
            )}
          </Card>
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
        <ButtonLink to={`/resume/preview/${version.id}?jobId=${jobId}`} variant="secondary" small>
          Preview
        </ButtonLink>
        <button
          type="button"
          className="btn btn-secondary btn-sm"
          onClick={() => onExport(version.id)}
          disabled={isExporting}
        >
          {isExporting ? 'Exporting' : 'Export PDF'}
        </button>
        {!isSelected && (
          <button
            type="button"
            className="btn btn-secondary btn-sm"
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
