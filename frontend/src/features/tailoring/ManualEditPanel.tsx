import { useCallback, useState } from 'react'
import type { ResumeContent } from '../../lib/types'
import {
  RESUME_SECTIONS,
  ResumeSectionEditor,
  type ResumeSectionId,
  type SectionUpdater,
} from '../../components/ResumeSectionEditor'

interface Props {
  initialContent: ResumeContent
  onSave: (content: ResumeContent, name: string) => void
  onCancel: () => void
}

/**
 * Manual tailored resume editing. Uses the same section editors as the base
 * resume builder. Saves a tailored version without AI suggestions.
 */
export function ManualEditPanel({ initialContent, onSave, onCancel }: Props) {
  const [content, setContent] = useState<ResumeContent>(initialContent)
  const [activeSection, setActiveSection] = useState<ResumeSectionId | null>('summary')
  const [versionName, setVersionName] = useState('')

  const updateSection = useCallback<SectionUpdater>((key, value) => {
    setContent((prev) => ({ ...prev, [key]: value }))
  }, [])

  function toggleSection(id: ResumeSectionId) {
    setActiveSection((prev) => (prev === id ? null : id))
  }

  return (
    <div className="tailor-manual">
      <p className="tailor-manual-intro">
        Edit your resume for this role. A new version is saved when you click Save.
      </p>

      <div className="section-list" role="list">
        {RESUME_SECTIONS.map((section) => (
          <div key={section.id} role="listitem">
            <button
              type="button"
              className={`section-row${activeSection === section.id ? ' section-row--active' : ''}`}
              onClick={() => toggleSection(section.id)}
              aria-expanded={activeSection === section.id}
              aria-controls={`manual-section-${section.id}`}
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
              <div id={`manual-section-${section.id}`} className="section-editor">
                <ResumeSectionEditor
                  sectionId={section.id}
                  content={content}
                  updateSection={updateSection}
                  idPrefix="m-"
                />
              </div>
            )}
          </div>
        ))}
      </div>

      <div className="tailor-save-bar">
        <div className="form-field" style={{ flex: 1, maxWidth: 320 }}>
          <label className="form-label" htmlFor="manual-version-name">
            Version name
          </label>
          <input
            id="manual-version-name"
            className="form-input"
            placeholder="e.g. Senior Frontend Engineer v1"
            value={versionName}
            onChange={(e) => setVersionName(e.target.value)}
          />
        </div>
        <div className="tailor-save-actions">
          <button type="button" className="btn btn-secondary" onClick={onCancel}>
            Cancel
          </button>
          <button
            type="button"
            className="btn btn-primary"
            onClick={() => onSave(content, versionName)}
          >
            Save version
          </button>
        </div>
      </div>
    </div>
  )
}
