import { useState } from 'react'
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

  const preselectedJobId = searchParams.get('jobId')

  async function handleStartAi(jobId: string) {
    setError(null)
    setWorkflow({ step: 'analyzing', jobId })
    try {
      const [analysis, batch, baseResume] = await Promise.all([
        tailoringApi.analyze(jobId),
        tailoringApi.generateSuggestions(jobId),
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
      setError(err instanceof Error ? err.message : 'Could not start tailoring')
      setWorkflow({ step: 'select-job' })
    }
  }

  async function handleStartManual(jobId: string) {
    setError(null)
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
    setWorkflow({ step: 'saving', jobId })
    try {
      const accepted = suggestions
        .filter((s) => s.decision === 'accepted')
        .map((s) => s.suggestion.id)
      const rejected = suggestions
        .filter((s) => s.decision === 'rejected')
        .map((s) => s.suggestion.id)
      const version = await tailoringApi.saveVersion(jobId, {
        name: versionName || undefined,
        content,
        acceptedSuggestionIds: accepted,
        rejectedSuggestionIds: rejected,
      })
      setWorkflow({ step: 'saved', jobId, version })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not save version')
      setWorkflow({ step: 'select-job' })
    }
  }

  async function handleSaveManual(jobId: string, content: ResumeContent, versionName: string) {
    setWorkflow({ step: 'saving', jobId })
    try {
      const version = await tailoringApi.saveVersion(jobId, {
        name: versionName || undefined,
        content,
      })
      setWorkflow({ step: 'saved', jobId, version })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not save version')
      setWorkflow({ step: 'select-job' })
    }
  }

  function handleReset() {
    setError(null)
    setWorkflow({ step: 'select-job' })
  }

  if (workflow.step === 'analyzing' || workflow.step === 'saving') {
    const label = workflow.step === 'analyzing' ? 'Analyzing job…' : 'Saving version…'
    return (
      <div className="page">
        <PageHeader />
        <div className="tailor-loading" aria-busy="true">
          <div className="auth-loading-spinner" />
          <span className="tailor-loading-label">{label}</span>
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
