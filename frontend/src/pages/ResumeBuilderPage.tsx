import { useCallback, useEffect, useRef, useState } from 'react'
import { resumeApi } from '../lib/api'
import type { ResumeContent } from '../lib/types'
import { emptyResumeContent } from '../lib/types'
import {
  RESUME_SECTIONS,
  ResumeSectionEditor,
  type ResumeSectionId,
  type SectionUpdater,
} from '../components/ResumeSectionEditor'
import { Button, ButtonLink } from '../components/ui/Button'
import { EmptyState } from '../components/ui/EmptyState'
import { PageHeader } from '../components/ui/PageHeader'
import { Skeleton } from '../components/ui/Skeleton'

type LoadState =
  | { status: 'loading' }
  | { status: 'ready' }
  | { status: 'error'; message: string }

const sectionDomId = (id: ResumeSectionId) => `section-${id}`

export function ResumeBuilderPage() {
  const [loadState, setLoadState] = useState<LoadState>({ status: 'loading' })
  const [content, setContent] = useState<ResumeContent>(emptyResumeContent)
  const [openSections, setOpenSections] = useState<ReadonlySet<ResumeSectionId>>(
    () => new Set<ResumeSectionId>(['personal']),
  )
  const [spyId, setSpyId] = useState<ResumeSectionId>(RESUME_SECTIONS[0].id)
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

  // Scroll-spy: the section nearest the top of the scroll area is the current one.
  const ready = loadState.status === 'ready'
  useEffect(() => {
    if (!ready || typeof IntersectionObserver === 'undefined') return
    const observer = new IntersectionObserver(
      (entries) => {
        const visible = entries
          .filter((entry) => entry.isIntersecting)
          .sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top)
        const id = visible[0]?.target.getAttribute('data-section')
        if (id) setSpyId(id as ResumeSectionId)
      },
      { rootMargin: '-10% 0px -70% 0px' },
    )
    document.querySelectorAll('[data-section]').forEach((el) => observer.observe(el))
    return () => observer.disconnect()
  }, [ready])

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
    setOpenSections((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  function jumpTo(id: ResumeSectionId) {
    setOpenSections((prev) => new Set(prev).add(id))
    setSpyId(id)
    // Wait a frame so an opened section has its height before scrolling.
    requestAnimationFrame(() => {
      document.getElementById(sectionDomId(id))?.scrollIntoView?.({ behavior: 'smooth', block: 'start' })
    })
  }

  if (loadState.status === 'loading') {
    return (
      <div className="page" aria-busy="true">
        <ResumeHeader isSaving={false} onSave={handleSave} hasUnsavedChanges={false} />
        <div className="section-stack">
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="section-card" aria-hidden="true">
              <div className="section-row">
                <Skeleton width="30%" />
                <Skeleton width="15%" />
              </div>
            </div>
          ))}
        </div>
      </div>
    )
  }

  if (loadState.status === 'error') {
    return (
      <div className="page">
        <ResumeHeader isSaving={false} onSave={handleSave} hasUnsavedChanges={false} />
        <EmptyState
          title="Could not load resume"
          body={loadState.message}
          action={<Button onClick={handleRetry}>Retry</Button>}
        />
      </div>
    )
  }

  const hasUnsavedChanges = JSON.stringify(content) !== JSON.stringify(serverContent.current)

  return (
    <div className="page page--resume">
      <ResumeHeader
        isSaving={isSaving}
        onSave={handleSave}
        lastSaved={lastSaved}
        hasUnsavedChanges={hasUnsavedChanges}
      />

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

      <div className="resume-layout">
        <nav className="section-index" aria-label="Resume sections">
          <ol>
            {RESUME_SECTIONS.map((section) => (
              <li key={section.id}>
                <a
                  href={`#${sectionDomId(section.id)}`}
                  className={`section-index-link${spyId === section.id ? ' is-current' : ''}`}
                  aria-current={spyId === section.id ? 'location' : undefined}
                  onClick={(event) => {
                    event.preventDefault()
                    jumpTo(section.id)
                  }}
                >
                  {section.label}
                </a>
              </li>
            ))}
          </ol>
        </nav>

        <div className="section-stack">
          {RESUME_SECTIONS.map((section) => {
            const open = openSections.has(section.id)
            return (
              <section
                key={section.id}
                id={sectionDomId(section.id)}
                data-section={section.id}
                className={`section-card${open ? ' is-open' : ''}`}
              >
                <button
                  type="button"
                  className="section-row"
                  onClick={() => toggleSection(section.id)}
                  aria-expanded={open}
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

                {open && (
                  <div id={`section-editor-${section.id}`} className="section-editor">
                    <ResumeSectionEditor
                      sectionId={section.id}
                      content={content}
                      updateSection={updateSection}
                    />
                  </div>
                )}
              </section>
            )
          })}
        </div>
      </div>
    </div>
  )
}

function ResumeHeader({
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
  const saved = lastSaved
    ? `Last saved ${new Date(lastSaved).toLocaleDateString(undefined, {
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      })}`
    : 'Base resume used as source for all tailored versions'

  return (
    <PageHeader
      title="Resume"
      subtitle={saved}
      actions={
        <>
          {lastSaved && (
            <ButtonLink
              to="/resume/preview"
              variant="secondary"
              title={hasUnsavedChanges ? 'Preview shows last saved version' : undefined}
            >
              Preview
            </ButtonLink>
          )}
          <Button variant="primary" onClick={onSave} disabled={isSaving}>
            {isSaving ? 'Saving…' : 'Save'}
          </Button>
        </>
      }
    />
  )
}
