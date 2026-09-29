import { useRef, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { resumeApi, tailoringApi } from '../lib/api'
import { emptyResumeContent, type ResumeContent } from '../lib/types'
import {
  AiReviewPanel,
  JobSelector,
  ManualEditPanel,
  SavedConfirmation,
  type SuggestionState,
  type WorkflowStep,
} from '../features/tailoring'

export function TailoringPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const [workflow, setWorkflow] = useState<WorkflowStep>({ step: 'select-job' })
  const [error, setError] = useState<string | null>(null)
  const [busyLabel, setBusyLabel] = useState('Analyzing job…')
  const [isSaving, setIsSaving] = useState(false)
  const [canCancel, setCanCancel] = useState(false)
  const abortRef = useRef<AbortController | null>(null)

  const preselectedJobId = searchParams.get('jobId')

  async function handleStartAi(jobId: string) {
    setError(null)
    setBusyLabel('Generating suggestions. This can take up to a minute…')
    setWorkflow({ step: 'analyzing', jobId })
    const controller = new AbortController()
    abortRef.current = controller
    setCanCancel(true)
    try {
      const [analysis, batch, baseResume] = await Promise.all([
        tailoringApi.analyze(jobId, controller.signal),
        tailoringApi.generateSuggestions(jobId, controller.signal),
        resumeApi.get(),
      ])
      const baseContent = baseResume?.content ?? emptyResumeContent()
      const suggestions: SuggestionState[] = batch.suggestions.map((s) => ({
        suggestion: s,
        decision: 'pending',
        editedContent: s.suggestedContent,
      }))
      setWorkflow({ step: 'review-ai', jobId, analysis, batch, suggestions, baseContent })
    } catch (err) {
      // Cancelling is a choice, not a failure.
      if (!controller.signal.aborted) {
        setError(err instanceof Error ? err.message : 'Could not start tailoring')
      }
      setWorkflow({ step: 'select-job' })
    } finally {
      abortRef.current = null
      setCanCancel(false)
    }
  }

  function handleCancelAnalysis() {
    abortRef.current?.abort()
  }

  async function handleStartManual(jobId: string) {
    setError(null)
    setBusyLabel('Loading your resume…')
    setWorkflow({ step: 'analyzing', jobId })
    try {
      const baseResume = await resumeApi.get()
      const baseContent = baseResume?.content ?? emptyResumeContent()
      setWorkflow({ step: 'manual-edit', jobId, content: baseContent })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not load resume')
      setWorkflow({ step: 'select-job' })
    }
  }

  async function handleSaveFromAi(
    jobId: string,
    content: ResumeContent,
    suggestions: SuggestionState[],
    versionName: string,
  ) {
    setError(null)
    setIsSaving(true)
    try {
      const accepted = suggestions.filter((s) => s.decision === 'accepted')
      const rejected = suggestions.filter((s) => s.decision === 'rejected')
      const version = await tailoringApi.saveVersion(jobId, {
        name: versionName || undefined,
        content,
        acceptedSuggestionIds: accepted.map((s) => s.suggestion.id),
        rejectedSuggestionIds: rejected.map((s) => s.suggestion.id),
        suggestionEdits: Object.fromEntries(
          accepted.map((s) => [s.suggestion.id, s.editedContent.trim()]),
        ),
      })
      setWorkflow({ step: 'saved', jobId, version })
    } catch (err) {
      // Stay on the review step so every accept, reject and edit is kept for a retry.
      setError(err instanceof Error ? err.message : 'Could not save version')
    } finally {
      setIsSaving(false)
    }
  }

  async function handleSaveManual(jobId: string, content: ResumeContent, versionName: string) {
    setError(null)
    setIsSaving(true)
    try {
      const version = await tailoringApi.saveVersion(jobId, {
        name: versionName || undefined,
        content,
      })
      setWorkflow({ step: 'saved', jobId, version })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not save version')
    } finally {
      setIsSaving(false)
    }
  }

  function handleReset() {
    setError(null)
    setWorkflow({ step: 'select-job' })
  }

  if (workflow.step === 'analyzing' || workflow.step === 'saving') {
    const label = workflow.step === 'analyzing' ? busyLabel : 'Saving version…'
    return (
      <div className="page">
        <PageHeader />
        <div className="tailor-loading" aria-busy="true">
          <div className="auth-loading-spinner" />
          <span className="tailor-loading-label">{label}</span>
          {workflow.step === 'analyzing' && canCancel && (
            <button type="button" className="btn btn-secondary" onClick={handleCancelAnalysis}>
              Cancel
            </button>
          )}
        </div>
      </div>
    )
  }

  if (workflow.step === 'saved') {
    return (
      <div className="page">
        <PageHeader />
        <SavedConfirmation
          version={workflow.version}
          onTailorAnother={handleReset}
          onViewJob={() => navigate(`/jobs/${workflow.jobId}`)}
        />
      </div>
    )
  }

  if (workflow.step === 'review-ai') {
    return (
      <div className="page">
        <PageHeader onBack={handleReset} />
        {error && (
          <div className="form-alert" role="alert" style={{ marginBottom: 16 }}>
            {error}
          </div>
        )}
        <AiReviewPanel
          analysis={workflow.analysis}
          batch={workflow.batch}
          initialSuggestions={workflow.suggestions}
          baseContent={workflow.baseContent}
          onSave={(content, suggestions, name) =>
            handleSaveFromAi(workflow.jobId, content, suggestions, name)
          }
          onCancel={handleReset}
          isSaving={isSaving}
        />
      </div>
    )
  }

  if (workflow.step === 'manual-edit') {
    return (
      <div className="page">
        <PageHeader onBack={handleReset} />
        {error && (
          <div className="form-alert" role="alert" style={{ marginBottom: 16 }}>
            {error}
          </div>
        )}
        <ManualEditPanel
          initialContent={workflow.content}
          onSave={(content, name) => handleSaveManual(workflow.jobId, content, name)}
          onCancel={handleReset}
          isSaving={isSaving}
        />
      </div>
    )
  }

  return (
    <div className="page">
      <PageHeader />
      {error && (
        <div className="form-alert" role="alert" style={{ marginBottom: 16 }}>
          {error}
        </div>
      )}
      <JobSelector
        preselectedJobId={preselectedJobId}
        onStartAi={handleStartAi}
        onStartManual={handleStartManual}
      />
    </div>
  )
}

function PageHeader({ onBack }: { onBack?: () => void }) {
  return (
    <div className="page-header">
      <div>
        {onBack && (
          <button
            type="button"
            className="back-link"
            onClick={onBack}
            style={{ background: 'none', border: 'none', cursor: 'pointer', padding: 0 }}
          >
            ← Tailoring
          </button>
        )}
        <h1 className="page-title" style={{ marginTop: onBack ? 4 : 0 }}>
          Tailoring
        </h1>
        <p className="page-subtitle">Tailor your base resume for a specific role</p>
      </div>
    </div>
  )
}
