import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { jobsApi } from '../../lib/api'
import type { JobSummary } from '../../lib/types'

type LoadState =
  | { status: 'loading' }
  | { status: 'ready'; jobs: JobSummary[] }
  | { status: 'error'; message: string }

interface Props {
  preselectedJobId: string | null
  onStartAi: (jobId: string) => void
  onStartManual: (jobId: string) => void
}

export function JobSelector({ preselectedJobId, onStartAi, onStartManual }: Props) {
  const [loadState, setLoadState] = useState<LoadState>({ status: 'loading' })
  const [selectedJobId, setSelectedJobId] = useState<string>(preselectedJobId ?? '')

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const jobs = await jobsApi.list()
        if (cancelled) return
        setLoadState({ status: 'ready', jobs })
        if (preselectedJobId && jobs.some((j) => j.id === preselectedJobId)) {
          setSelectedJobId(preselectedJobId)
        }
      } catch (err) {
        if (cancelled) return
        setLoadState({
          status: 'error',
          message: err instanceof Error ? err.message : 'Could not load jobs',
        })
      }
    })()
    return () => {
      cancelled = true
    }
  }, [preselectedJobId])

  if (loadState.status === 'loading') {
    return (
      <div className="tailor-select-card">
        <div className="skeleton skeleton-heading" style={{ width: '40%' }} />
        <div className="skeleton skeleton-card" style={{ height: 80 }} />
      </div>
    )
  }

  if (loadState.status === 'error') {
    return (
      <div className="empty-state">
        <p className="empty-state-title">Could not load jobs</p>
        <p className="empty-state-body">{loadState.message}</p>
      </div>
    )
  }

  const { jobs } = loadState

  if (jobs.length === 0) {
    return (
      <div className="empty-state">
        <p className="empty-state-title">No jobs yet</p>
        <p className="empty-state-body">Add a job before tailoring your resume.</p>
        <Link to="/jobs/new" className="btn btn-primary">
          Add a job
        </Link>
      </div>
    )
  }

  const selectedJob = jobs.find((j) => j.id === selectedJobId) ?? null

  return (
    <div className="tailor-select-card">
      <div className="form-field">
        <label className="form-label" htmlFor="tailor-job-select">
          Select a job <span className="form-required">*</span>
        </label>
        <select
          id="tailor-job-select"
          className="form-input"
          value={selectedJobId}
          onChange={(e) => setSelectedJobId(e.target.value)}
        >
          <option value="">Choose a job…</option>
          {jobs.map((job) => (
            <option key={job.id} value={job.id}>
              {job.title} — {job.company}
            </option>
          ))}
        </select>
      </div>

      {selectedJob && (
        <div className="tailor-job-preview">
          <span className="tailor-job-preview-title">{selectedJob.title}</span>
          <span className="tailor-job-preview-company">{selectedJob.company}</span>
        </div>
      )}

      <div className="tailor-mode-grid">
        <button
          type="button"
          className="tailor-mode-card"
          disabled={!selectedJobId}
          onClick={() => selectedJobId && onStartAi(selectedJobId)}
        >
          <span className="tailor-mode-icon" aria-hidden="true">
            <svg width="18" height="18" viewBox="0 0 16 16" fill="none">
              <path
                d="M2 8h3l2-5 2 10 2-5h3"
                stroke="currentColor"
                strokeWidth="1.4"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </svg>
          </span>
          <span className="tailor-mode-label">AI-assisted tailoring</span>
          <span className="tailor-mode-desc">
            Extract keywords, compare against your resume, and review AI suggestions before saving.
          </span>
        </button>

        <button
          type="button"
          className="tailor-mode-card"
          disabled={!selectedJobId}
          onClick={() => selectedJobId && onStartManual(selectedJobId)}
        >
          <span className="tailor-mode-icon" aria-hidden="true">
            <svg width="18" height="18" viewBox="0 0 16 16" fill="none">
              <path
                d="M11 2.5a1.5 1.5 0 0 1 2.12 2.12L5 12.75 2 13.5l.75-3L11 2.5z"
                stroke="currentColor"
                strokeWidth="1.4"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </svg>
          </span>
          <span className="tailor-mode-label">Manual editing</span>
          <span className="tailor-mode-desc">
            Edit your resume sections directly for this role without AI suggestions.
          </span>
        </button>
      </div>
    </div>
  )
}
