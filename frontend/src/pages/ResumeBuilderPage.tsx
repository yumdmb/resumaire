import { useCallback, useEffect, useRef, useState } from 'react'
import { resumeApi } from '../lib/api'
import type {
  ResumeCertification,
  ResumeContent,
  ResumeEducation,
  ResumeExperience,
  ResumeLink,
  ResumePersonalInfo,
} from '../lib/types'
import {
  emptyCertification,
  emptyEducation,
  emptyExperience,
  emptyLink,
  emptyResumeContent,
} from '../lib/types'

// ─── Section definitions ────────────────────────────────────────

type SectionId =
  | 'personal'
  | 'summary'
  | 'skills'
  | 'experience'
  | 'education'
  | 'certifications'
  | 'links'

interface SectionDef {
  id: SectionId
  label: string
  meta: (content: ResumeContent) => string
}

const SECTIONS: SectionDef[] = [
  {
    id: 'personal',
    label: 'Personal info',
    meta: (c) => c.personalInfo?.fullName || 'Not set',
  },
  {
    id: 'summary',
    label: 'Summary',
    meta: (c) =>
      c.summary ? `${c.summary.slice(0, 48)}${c.summary.length > 48 ? '…' : ''}` : 'Not set',
  },
  {
    id: 'skills',
    label: 'Skills',
    meta: (c) => `${c.skills.length} skill${c.skills.length === 1 ? '' : 's'}`,
  },
  {
    id: 'experience',
    label: 'Experience',
    meta: (c) => `${c.experience.length} entr${c.experience.length === 1 ? 'y' : 'ies'}`,
  },
  {
    id: 'education',
    label: 'Education',
    meta: (c) => `${c.education.length} entr${c.education.length === 1 ? 'y' : 'ies'}`,
  },
  {
    id: 'certifications',
    label: 'Certifications',
    meta: (c) => `${c.certifications.length} entr${c.certifications.length === 1 ? 'y' : 'ies'}`,
  },
  {
    id: 'links',
    label: 'Links',
    meta: (c) => `${c.links.length} link${c.links.length === 1 ? '' : 's'}`,
  },
]

// ─── Page state ─────────────────────────────────────────────────

type LoadState =
  | { status: 'loading' }
  | { status: 'ready' }
  | { status: 'error'; message: string }

export function ResumeBuilderPage() {
  const [loadState, setLoadState] = useState<LoadState>({ status: 'loading' })
  const [content, setContent] = useState<ResumeContent>(emptyResumeContent)
  const [activeSection, setActiveSection] = useState<SectionId | null>(null)
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
        const message =
          error instanceof Error ? error.message : 'Could not load resume'
        setLoadState({ status: 'error', message })
      }
    })()
    return () => { cancelled = true }
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

  // Section-level updaters: only touch the relevant section, preserving others
  const updateSection = useCallback(
    <K extends keyof ResumeContent>(key: K, value: ResumeContent[K]) => {
      setContent((prev) => ({ ...prev, [key]: value }))
    },
    [],
  )

  function toggleSection(id: SectionId) {
    setActiveSection((prev) => (prev === id ? null : id))
  }

  // ─── Render states ──────────────────────────────────────────

  if (loadState.status === 'loading') {
    return (
      <div className="page" aria-busy="true">
        <PageHeader isSaving={false} onSave={handleSave} />
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
        <PageHeader isSaving={false} onSave={handleSave} />
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

  return (
    <div className="page">
      <PageHeader isSaving={isSaving} onSave={handleSave} lastSaved={lastSaved} />

      {saveError && (
        <div className="form-alert" role="alert" style={{ marginBottom: 16 }}>
          {saveError}
        </div>
      )}

      <div className="section-list" role="list">
        {SECTIONS.map((section) => (
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
              <div
                id={`section-editor-${section.id}`}
                className="section-editor"
              >
                <SectionEditor
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

// ─── Page header ────────────────────────────────────────────────

function PageHeader({
  isSaving,
  onSave,
  lastSaved,
}: {
  isSaving: boolean
  onSave: () => void
  lastSaved?: string | null
}) {
  return (
    <div className="page-header">
      <div>
        <h1 className="page-title">Resume</h1>
        <p className="page-subtitle">
          Base resume used as source for all tailored versions
          {lastSaved && (
            <span className="resume-saved-at">
              {' · '}Last saved {new Date(lastSaved).toLocaleDateString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })}
            </span>
          )}
        </p>
      </div>
      <div style={{ display: 'flex', gap: 8 }}>
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

// ─── Section editor router ──────────────────────────────────────

function SectionEditor({
  sectionId,
  content,
  updateSection,
}: {
  sectionId: SectionId
  content: ResumeContent
  updateSection: <K extends keyof ResumeContent>(key: K, value: ResumeContent[K]) => void
}) {
  switch (sectionId) {
    case 'personal':
      return (
        <PersonalInfoEditor
          value={content.personalInfo}
          onChange={(v) => updateSection('personalInfo', v)}
        />
      )
    case 'summary':
      return (
        <SummaryEditor
          value={content.summary}
          onChange={(v) => updateSection('summary', v)}
        />
      )
    case 'skills':
      return (
        <SkillsEditor
          value={content.skills}
          onChange={(v) => updateSection('skills', v)}
        />
      )
    case 'experience':
      return (
        <ExperienceEditor
          value={content.experience}
          onChange={(v) => updateSection('experience', v)}
        />
      )
    case 'education':
      return (
        <EducationEditor
          value={content.education}
          onChange={(v) => updateSection('education', v)}
        />
      )
    case 'certifications':
      return (
        <CertificationsEditor
          value={content.certifications}
          onChange={(v) => updateSection('certifications', v)}
        />
      )
    case 'links':
      return (
        <LinksEditor
          value={content.links}
          onChange={(v) => updateSection('links', v)}
        />
      )
  }
}


// ─── Personal info editor ───────────────────────────────────────

function PersonalInfoEditor({
  value,
  onChange,
}: {
  value: ResumePersonalInfo | null
  onChange: (v: ResumePersonalInfo) => void
}) {
  const info = value ?? {
    fullName: null,
    email: null,
    phone: null,
    location: null,
    headline: null,
    website: null,
  }

  function update(field: keyof ResumePersonalInfo, val: string) {
    onChange({ ...info, [field]: val || null })
  }

  return (
    <div className="editor-fields">
      <div className="form-row-two">
        <div className="form-field">
          <label className="form-label" htmlFor="pi-fullName">
            Full name <span className="form-required">*</span>
          </label>
          <input
            id="pi-fullName"
            className="form-input"
            value={info.fullName ?? ''}
            onChange={(e) => update('fullName', e.target.value)}
          />
        </div>
        <div className="form-field">
          <label className="form-label" htmlFor="pi-email">Email</label>
          <input
            id="pi-email"
            type="email"
            className="form-input"
            value={info.email ?? ''}
            onChange={(e) => update('email', e.target.value)}
          />
        </div>
      </div>
      <div className="form-row-two">
        <div className="form-field">
          <label className="form-label" htmlFor="pi-phone">Phone</label>
          <input
            id="pi-phone"
            className="form-input"
            value={info.phone ?? ''}
            onChange={(e) => update('phone', e.target.value)}
          />
        </div>
        <div className="form-field">
          <label className="form-label" htmlFor="pi-location">Location</label>
          <input
            id="pi-location"
            className="form-input"
            value={info.location ?? ''}
            onChange={(e) => update('location', e.target.value)}
          />
        </div>
      </div>
      <div className="form-row-two">
        <div className="form-field">
          <label className="form-label" htmlFor="pi-headline">Headline</label>
          <input
            id="pi-headline"
            className="form-input"
            placeholder="e.g. Senior Frontend Engineer"
            value={info.headline ?? ''}
            onChange={(e) => update('headline', e.target.value)}
          />
        </div>
        <div className="form-field">
          <label className="form-label" htmlFor="pi-website">Website</label>
          <input
            id="pi-website"
            type="url"
            className="form-input"
            placeholder="https://…"
            value={info.website ?? ''}
            onChange={(e) => update('website', e.target.value)}
          />
        </div>
      </div>
    </div>
  )
}

// ─── Summary editor ─────────────────────────────────────────────

function SummaryEditor({
  value,
  onChange,
}: {
  value: string | null
  onChange: (v: string | null) => void
}) {
  return (
    <div className="editor-fields">
      <div className="form-field">
        <label className="form-label" htmlFor="summary-text">
          Professional summary
        </label>
        <textarea
          id="summary-text"
          className="form-input form-textarea"
          rows={4}
          placeholder="A brief overview of your professional background and goals"
          value={value ?? ''}
          onChange={(e) => onChange(e.target.value || null)}
        />
        <p className="editor-hint">Keep it concise: 2–4 sentences.</p>
      </div>
    </div>
  )
}

// ─── Skills editor ──────────────────────────────────────────────

function SkillsEditor({
  value,
  onChange,
}: {
  value: string[]
  onChange: (v: string[]) => void
}) {
  const [draft, setDraft] = useState('')

  function addSkill() {
    const trimmed = draft.trim()
    if (!trimmed) return
    // Support comma-separated input
    const newSkills = trimmed
      .split(',')
      .map((s) => s.trim())
      .filter((s) => s.length > 0 && !value.includes(s))
    if (newSkills.length > 0) {
      onChange([...value, ...newSkills])
    }
    setDraft('')
  }

  function removeSkill(index: number) {
    onChange(value.filter((_, i) => i !== index))
  }

  function handleKeyDown(e: React.KeyboardEvent) {
    if (e.key === 'Enter') {
      e.preventDefault()
      addSkill()
    }
  }

  return (
    <div className="editor-fields">
      <div className="skills-input-row">
        <input
          className="form-input"
          placeholder="Add skills (comma-separated)"
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          onKeyDown={handleKeyDown}
          aria-label="New skill"
        />
        <button type="button" className="btn btn-secondary" onClick={addSkill}>
          Add
        </button>
      </div>
      {value.length > 0 && (
        <div className="skills-list">
          {value.map((skill, i) => (
            <span key={`${skill}-${i}`} className="skill-tag">
              {skill}
              <button
                type="button"
                className="skill-tag-remove"
                onClick={() => removeSkill(i)}
                aria-label={`Remove ${skill}`}
              >
                ×
              </button>
            </span>
          ))}
        </div>
      )}
    </div>
  )
}

// ─── Experience editor ──────────────────────────────────────────

function ExperienceEditor({
  value,
  onChange,
}: {
  value: ResumeExperience[]
  onChange: (v: ResumeExperience[]) => void
}) {
  function updateEntry(index: number, updated: ResumeExperience) {
    const next = [...value]
    next[index] = updated
    onChange(next)
  }

  function removeEntry(index: number) {
    onChange(value.filter((_, i) => i !== index))
  }

  function addEntry() {
    onChange([...value, emptyExperience()])
  }

  return (
    <div className="editor-fields">
      {value.map((entry, i) => (
        <ExperienceEntryEditor
          key={entry.id ?? i}
          entry={entry}
          index={i}
          onChange={(updated) => updateEntry(i, updated)}
          onRemove={() => removeEntry(i)}
        />
      ))}
      <button type="button" className="btn btn-secondary editor-add-btn" onClick={addEntry}>
        + Add experience
      </button>
    </div>
  )
}

function ExperienceEntryEditor({
  entry,
  index,
  onChange,
  onRemove,
}: {
  entry: ResumeExperience
  index: number
  onChange: (v: ResumeExperience) => void
  onRemove: () => void
}) {
  function update(field: keyof ResumeExperience, val: unknown) {
    onChange({ ...entry, [field]: val })
  }

  function updateBullet(bi: number, text: string) {
    const bullets = [...entry.bullets]
    bullets[bi] = text
    onChange({ ...entry, bullets })
  }

  function addBullet() {
    onChange({ ...entry, bullets: [...entry.bullets, ''] })
  }

  function removeBullet(bi: number) {
    onChange({ ...entry, bullets: entry.bullets.filter((_, i) => i !== bi) })
  }

  return (
    <div className="editor-entry">
      <div className="editor-entry-header">
        <span className="editor-entry-num">{index + 1}</span>
        <span className="editor-entry-title">
          {entry.role || entry.organization || 'New experience'}
        </span>
        <button type="button" className="btn-icon" onClick={onRemove} aria-label="Remove entry">
          <svg width="14" height="14" viewBox="0 0 16 16" fill="none" aria-hidden="true">
            <path d="M4 4l8 8M12 4l-8 8" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" />
          </svg>
        </button>
      </div>
      <div className="form-row-two">
        <div className="form-field">
          <label className="form-label" htmlFor={`exp-${index}-role`}>Role *</label>
          <input
            id={`exp-${index}-role`}
            className="form-input"
            value={entry.role ?? ''}
            onChange={(e) => update('role', e.target.value || null)}
          />
        </div>
        <div className="form-field">
          <label className="form-label" htmlFor={`exp-${index}-org`}>Organization *</label>
          <input
            id={`exp-${index}-org`}
            className="form-input"
            value={entry.organization ?? ''}
            onChange={(e) => update('organization', e.target.value || null)}
          />
        </div>
      </div>
      <div className="form-row-two">
        <div className="form-field">
          <label className="form-label" htmlFor={`exp-${index}-start`}>Start date</label>
          <input
            id={`exp-${index}-start`}
            className="form-input"
            placeholder="e.g. Jan 2022"
            value={entry.startDate ?? ''}
            onChange={(e) => update('startDate', e.target.value || null)}
          />
        </div>
        <div className="form-field">
          <label className="form-label" htmlFor={`exp-${index}-end`}>End date</label>
          <input
            id={`exp-${index}-end`}
            className="form-input"
            placeholder={entry.isCurrent ? 'Present' : 'e.g. Dec 2023'}
            disabled={entry.isCurrent}
            value={entry.isCurrent ? '' : (entry.endDate ?? '')}
            onChange={(e) => update('endDate', e.target.value || null)}
          />
        </div>
      </div>
      <div className="form-row-two">
        <div className="form-field">
          <label className="form-label" htmlFor={`exp-${index}-loc`}>Location</label>
          <input
            id={`exp-${index}-loc`}
            className="form-input"
            value={entry.location ?? ''}
            onChange={(e) => update('location', e.target.value || null)}
          />
        </div>
        <label className="editor-checkbox">
          <input
            type="checkbox"
            checked={entry.isCurrent}
            onChange={(e) => update('isCurrent', e.target.checked)}
          />
          Current role
        </label>
      </div>
      <div className="form-field">
        <span className="form-label">Bullets</span>
        <div className="bullets-list">
          {entry.bullets.map((bullet, bi) => (
            <div key={bi} className="bullet-row">
              <span className="bullet-dot">•</span>
              <input
                className="form-input bullet-input"
                value={bullet}
                onChange={(e) => updateBullet(bi, e.target.value)}
                aria-label={`Bullet ${bi + 1}`}
              />
              <button
                type="button"
                className="btn-icon"
                onClick={() => removeBullet(bi)}
                aria-label={`Remove bullet ${bi + 1}`}
              >
                <svg width="12" height="12" viewBox="0 0 16 16" fill="none" aria-hidden="true">
                  <path d="M4 4l8 8M12 4l-8 8" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" />
                </svg>
              </button>
            </div>
          ))}
          <button type="button" className="btn-text" onClick={addBullet}>
            + Add bullet
          </button>
        </div>
      </div>
    </div>
  )
}


// ─── Education editor ───────────────────────────────────────────

function EducationEditor({
  value,
  onChange,
}: {
  value: ResumeEducation[]
  onChange: (v: ResumeEducation[]) => void
}) {
  function updateEntry(index: number, updated: ResumeEducation) {
    const next = [...value]
    next[index] = updated
    onChange(next)
  }

  function removeEntry(index: number) {
    onChange(value.filter((_, i) => i !== index))
  }

  function addEntry() {
    onChange([...value, emptyEducation()])
  }

  return (
    <div className="editor-fields">
      {value.map((entry, i) => (
        <EducationEntryEditor
          key={entry.id ?? i}
          entry={entry}
          index={i}
          onChange={(updated) => updateEntry(i, updated)}
          onRemove={() => removeEntry(i)}
        />
      ))}
      <button type="button" className="btn btn-secondary editor-add-btn" onClick={addEntry}>
        + Add education
      </button>
    </div>
  )
}

function EducationEntryEditor({
  entry,
  index,
  onChange,
  onRemove,
}: {
  entry: ResumeEducation
  index: number
  onChange: (v: ResumeEducation) => void
  onRemove: () => void
}) {
  function update(field: keyof ResumeEducation, val: unknown) {
    onChange({ ...entry, [field]: val })
  }

  function updateDetail(di: number, text: string) {
    const details = [...entry.details]
    details[di] = text
    onChange({ ...entry, details })
  }

  function addDetail() {
    onChange({ ...entry, details: [...entry.details, ''] })
  }

  function removeDetail(di: number) {
    onChange({ ...entry, details: entry.details.filter((_, i) => i !== di) })
  }

  return (
    <div className="editor-entry">
      <div className="editor-entry-header">
        <span className="editor-entry-num">{index + 1}</span>
        <span className="editor-entry-title">
          {entry.institution || entry.degree || 'New education'}
        </span>
        <button type="button" className="btn-icon" onClick={onRemove} aria-label="Remove entry">
          <svg width="14" height="14" viewBox="0 0 16 16" fill="none" aria-hidden="true">
            <path d="M4 4l8 8M12 4l-8 8" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" />
          </svg>
        </button>
      </div>
      <div className="form-row-two">
        <div className="form-field">
          <label className="form-label" htmlFor={`edu-${index}-inst`}>Institution *</label>
          <input
            id={`edu-${index}-inst`}
            className="form-input"
            value={entry.institution ?? ''}
            onChange={(e) => update('institution', e.target.value || null)}
          />
        </div>
        <div className="form-field">
          <label className="form-label" htmlFor={`edu-${index}-degree`}>Degree</label>
          <input
            id={`edu-${index}-degree`}
            className="form-input"
            value={entry.degree ?? ''}
            onChange={(e) => update('degree', e.target.value || null)}
          />
        </div>
      </div>
      <div className="form-row-two">
        <div className="form-field">
          <label className="form-label" htmlFor={`edu-${index}-field`}>Field of study</label>
          <input
            id={`edu-${index}-field`}
            className="form-input"
            value={entry.field ?? ''}
            onChange={(e) => update('field', e.target.value || null)}
          />
        </div>
        <div className="form-field">
          <label className="form-label" htmlFor={`edu-${index}-loc`}>Location</label>
          <input
            id={`edu-${index}-loc`}
            className="form-input"
            value={entry.location ?? ''}
            onChange={(e) => update('location', e.target.value || null)}
          />
        </div>
      </div>
      <div className="form-row-two">
        <div className="form-field">
          <label className="form-label" htmlFor={`edu-${index}-start`}>Start date</label>
          <input
            id={`edu-${index}-start`}
            className="form-input"
            placeholder="e.g. Sep 2018"
            value={entry.startDate ?? ''}
            onChange={(e) => update('startDate', e.target.value || null)}
          />
        </div>
        <div className="form-field">
          <label className="form-label" htmlFor={`edu-${index}-end`}>End date</label>
          <input
            id={`edu-${index}-end`}
            className="form-input"
            placeholder="e.g. Jun 2022"
            value={entry.endDate ?? ''}
            onChange={(e) => update('endDate', e.target.value || null)}
          />
        </div>
      </div>
      <div className="form-field">
        <span className="form-label">Details</span>
        <div className="bullets-list">
          {entry.details.map((detail, di) => (
            <div key={di} className="bullet-row">
              <span className="bullet-dot">•</span>
              <input
                className="form-input bullet-input"
                value={detail}
                onChange={(e) => updateDetail(di, e.target.value)}
                aria-label={`Detail ${di + 1}`}
              />
              <button
                type="button"
                className="btn-icon"
                onClick={() => removeDetail(di)}
                aria-label={`Remove detail ${di + 1}`}
              >
                <svg width="12" height="12" viewBox="0 0 16 16" fill="none" aria-hidden="true">
                  <path d="M4 4l8 8M12 4l-8 8" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" />
                </svg>
              </button>
            </div>
          ))}
          <button type="button" className="btn-text" onClick={addDetail}>
            + Add detail
          </button>
        </div>
      </div>
    </div>
  )
}

// ─── Certifications editor ──────────────────────────────────────

function CertificationsEditor({
  value,
  onChange,
}: {
  value: ResumeCertification[]
  onChange: (v: ResumeCertification[]) => void
}) {
  function updateEntry(index: number, updated: ResumeCertification) {
    const next = [...value]
    next[index] = updated
    onChange(next)
  }

  function removeEntry(index: number) {
    onChange(value.filter((_, i) => i !== index))
  }

  function addEntry() {
    onChange([...value, emptyCertification()])
  }

  return (
    <div className="editor-fields">
      {value.map((entry, i) => (
        <CertificationEntryEditor
          key={entry.id ?? i}
          entry={entry}
          index={i}
          onChange={(updated) => updateEntry(i, updated)}
          onRemove={() => removeEntry(i)}
        />
      ))}
      <button type="button" className="btn btn-secondary editor-add-btn" onClick={addEntry}>
        + Add certification
      </button>
    </div>
  )
}

function CertificationEntryEditor({
  entry,
  index,
  onChange,
  onRemove,
}: {
  entry: ResumeCertification
  index: number
  onChange: (v: ResumeCertification) => void
  onRemove: () => void
}) {
  function update(field: keyof ResumeCertification, val: string | null) {
    onChange({ ...entry, [field]: val })
  }

  return (
    <div className="editor-entry">
      <div className="editor-entry-header">
        <span className="editor-entry-num">{index + 1}</span>
        <span className="editor-entry-title">
          {entry.name || 'New certification'}
        </span>
        <button type="button" className="btn-icon" onClick={onRemove} aria-label="Remove entry">
          <svg width="14" height="14" viewBox="0 0 16 16" fill="none" aria-hidden="true">
            <path d="M4 4l8 8M12 4l-8 8" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" />
          </svg>
        </button>
      </div>
      <div className="form-row-two">
        <div className="form-field">
          <label className="form-label" htmlFor={`cert-${index}-name`}>Name *</label>
          <input
            id={`cert-${index}-name`}
            className="form-input"
            value={entry.name ?? ''}
            onChange={(e) => update('name', e.target.value || null)}
          />
        </div>
        <div className="form-field">
          <label className="form-label" htmlFor={`cert-${index}-issuer`}>Issuer *</label>
          <input
            id={`cert-${index}-issuer`}
            className="form-input"
            value={entry.issuer ?? ''}
            onChange={(e) => update('issuer', e.target.value || null)}
          />
        </div>
      </div>
      <div className="form-row-two">
        <div className="form-field">
          <label className="form-label" htmlFor={`cert-${index}-issued`}>Issued date</label>
          <input
            id={`cert-${index}-issued`}
            className="form-input"
            placeholder="e.g. Mar 2023"
            value={entry.issuedDate ?? ''}
            onChange={(e) => update('issuedDate', e.target.value || null)}
          />
        </div>
        <div className="form-field">
          <label className="form-label" htmlFor={`cert-${index}-exp`}>Expiration date</label>
          <input
            id={`cert-${index}-exp`}
            className="form-input"
            placeholder="e.g. Mar 2026"
            value={entry.expirationDate ?? ''}
            onChange={(e) => update('expirationDate', e.target.value || null)}
          />
        </div>
      </div>
      <div className="form-row-two">
        <div className="form-field">
          <label className="form-label" htmlFor={`cert-${index}-cred`}>Credential ID</label>
          <input
            id={`cert-${index}-cred`}
            className="form-input"
            value={entry.credentialId ?? ''}
            onChange={(e) => update('credentialId', e.target.value || null)}
          />
        </div>
        <div className="form-field">
          <label className="form-label" htmlFor={`cert-${index}-url`}>URL</label>
          <input
            id={`cert-${index}-url`}
            type="url"
            className="form-input"
            placeholder="https://…"
            value={entry.url ?? ''}
            onChange={(e) => update('url', e.target.value || null)}
          />
        </div>
      </div>
    </div>
  )
}

// ─── Links editor ───────────────────────────────────────────────

function LinksEditor({
  value,
  onChange,
}: {
  value: ResumeLink[]
  onChange: (v: ResumeLink[]) => void
}) {
  function updateEntry(index: number, updated: ResumeLink) {
    const next = [...value]
    next[index] = updated
    onChange(next)
  }

  function removeEntry(index: number) {
    onChange(value.filter((_, i) => i !== index))
  }

  function addEntry() {
    onChange([...value, emptyLink()])
  }

  return (
    <div className="editor-fields">
      {value.map((entry, i) => (
        <div key={entry.id ?? i} className="editor-entry">
          <div className="editor-entry-header">
            <span className="editor-entry-num">{i + 1}</span>
            <span className="editor-entry-title">
              {entry.label || 'New link'}
            </span>
            <button type="button" className="btn-icon" onClick={() => removeEntry(i)} aria-label="Remove link">
              <svg width="14" height="14" viewBox="0 0 16 16" fill="none" aria-hidden="true">
                <path d="M4 4l8 8M12 4l-8 8" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" />
              </svg>
            </button>
          </div>
          <div className="form-row-two">
            <div className="form-field">
              <label className="form-label" htmlFor={`link-${i}-label`}>Label *</label>
              <input
                id={`link-${i}-label`}
                className="form-input"
                placeholder="e.g. GitHub"
                value={entry.label ?? ''}
                onChange={(e) => updateEntry(i, { ...entry, label: e.target.value || null })}
              />
            </div>
            <div className="form-field">
              <label className="form-label" htmlFor={`link-${i}-url`}>URL *</label>
              <input
                id={`link-${i}-url`}
                type="url"
                className="form-input"
                placeholder="https://…"
                value={entry.url ?? ''}
                onChange={(e) => updateEntry(i, { ...entry, url: e.target.value || null })}
              />
            </div>
          </div>
        </div>
      ))}
      <button type="button" className="btn btn-secondary editor-add-btn" onClick={addEntry}>
        + Add link
      </button>
    </div>
  )
}
