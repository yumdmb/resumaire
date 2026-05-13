import { useCallback, useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { resumeApi } from '../lib/api'
import type { ResumeContent } from '../lib/types'
import { emptyResumeContent } from '../lib/types'
import {
  RESUME_SECTIONS,
  ResumeSectionEditor,
  type ResumeSectionId,
  type SectionUpdater,
} from '../components/ResumeSectionEditor'

type LoadState =
  | { status: 'loading' }
  | { status: 'ready' }
  | { status: 'error'; message: string }

export function ResumeBuilderPage() {
  const [loadState, setLoadState] = useState<LoadState>({ status: 'loading' })
  const [content, setContent] = useState<ResumeContent>(emptyResumeContent)
  const [activeSection, setActiveSection] = useState<ResumeSectionId | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [lastSaved, setLastSaved] = useState<string | null>(null)
  const [reloadToken, setReloadToken] = useState(0)

  // Track the server-side content to detect unsaved changes
  const serverContent = useRef<ResumeContent>(emptyResumeContent())

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const resume = await resumeApi.get()
        if (cancelled) return
        if (resume) {
          setContent(resume.content)
          serverContent.current = resume.content
          setLastSaved(resume.updatedAt)
        }
        setLoadState({ status: 'ready' })
      } catch (error) {
        if (cancelled) return
        const message = error instanceof Error ? error.message : 'Could not load resume'
        setLoadState({ status: 'error', message })
      }
    })()
    return () => {
      cancelled = true
    }
  }, [reloadToken])

  function handleRetry() {
    setLoadState({ status: 'loading' })
    setReloadToken((t) => t + 1)
  }

  async function handleSave() {
    setIsSaving(true)
    setSaveError(null)
    try {
      const saved = await resumeApi.save(content)
      serverContent.current = saved.content
      setContent(saved.content)
      setLastSaved(saved.updatedAt)
    } catch (error) {
      setSaveError(error instanceof Error ? error.message : 'Save failed')
    } finally {
      setIsSaving(false)
    }
  }

  const updateSection = useCallback<SectionUpdater>((key, value) => {
    setContent((prev) => ({ ...prev, [key]: value }))
  }, [])

  function toggleSection(id: ResumeSectionId) {
    setActiveSection((prev) => (prev === id ? null : id))
  }

  if (loadState.status === 'loading') {
    return (
      <div className="page" aria-busy="true">
        <PageHeader isSaving={false} onSave={handleSave} hasUnsavedChanges={false} />
        <div className="section-list">
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="section-row" aria-hidden="true">
              <div className="skeleton skeleton-line" style={{ width: '30%' }} />
              <div className="skeleton skeleton-line" style={{ width: '15%' }} />
            </div>
          ))}
        </div>
      </div>
    )
  }

  if (loadState.status === 'error') {
    return (
      <div className="page">
        <PageHeader isSaving={false} onSave={handleSave} hasUnsavedChanges={false} />
        <div className="empty-state">
          <p className="empty-state-title">Could not load resume</p>
          <p className="empty-state-body">{loadState.message}</p>
          <button type="button" className="btn btn-secondary" onClick={handleRetry}>
            Retry
          </button>
        </div>
      </div>
    )
  }

  const hasUnsavedChanges = JSON.stringify(content) !== JSON.stringify(serverContent.current)

  return (
    <div className="page">
      <PageHeader isSaving={isSaving} onSave={handleSave} lastSaved={lastSaved} hasUnsavedChanges={hasUnsavedChanges} />

      {hasUnsavedChanges && (
        <div className="preview-unsaved-notice" role="status">
          You have unsaved changes. Preview and export use the last saved version.
        </div>
      )}

      {saveError && (
        <div className="form-alert" role="alert" style={{ marginBottom: 16 }}>
          {saveError}
        </div>
      )}

      <div className="section-list" role="list">
        {RESUME_SECTIONS.map((section) => (
          <div key={section.id} role="listitem">
            <button
              type="button"
              className={`section-row${activeSection === section.id ? ' section-row--active' : ''}`}
              onClick={() => toggleSection(section.id)}
              aria-expanded={activeSection === section.id}
              aria-controls={`section-editor-${section.id}`}
            >
              <span className="section-row-name">{section.label}</span>
              <span className="section-row-meta">{section.meta(content)}</span>
              <svg
                className="section-row-chevron"
                width="14"
                height="14"
                viewBox="0 0 16 16"
                fill="none"
                aria-hidden="true"
              >
                <path
                  d="M4 6l4 4 4-4"
                  stroke="currentColor"
                  strokeWidth="1.4"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
            </button>

            {activeSection === section.id && (
              <div id={`section-editor-${section.id}`} className="section-editor">
                <ResumeSectionEditor
                  sectionId={section.id}
                  content={content}
                  updateSection={updateSection}
                />
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  )
}

function PageHeader({
  isSaving,
  onSave,
  lastSaved,
  hasUnsavedChanges,
}: {
  isSaving: boolean
  onSave: () => void
  lastSaved?: string | null
  hasUnsavedChanges: boolean
}) {
  return (
    <div className="page-header">
      <div>
        <h1 className="page-title">Resume</h1>
        <p className="page-subtitle">
          Base resume used as source for all tailored versions
          {lastSaved && (
            <span className="resume-saved-at">
              {' · '}Last saved{' '}
              {new Date(lastSaved).toLocaleDateString(undefined, {
                month: 'short',
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit',
              })}
            </span>
          )}
        </p>
      </div>
      <div style={{ display: 'flex', gap: 8 }}>
        {lastSaved && (
          <Link
            to="/resume/preview"
            className="btn btn-secondary"
            title={hasUnsavedChanges ? 'Preview shows last saved version' : undefined}
          >
            Preview
          </Link>
        )}
        <button
          type="button"
          className="btn btn-primary"
          onClick={onSave}
          disabled={isSaving}
        >
          {isSaving ? 'Saving…' : 'Save'}
        </button>
      </div>
    </div>
  )
}
