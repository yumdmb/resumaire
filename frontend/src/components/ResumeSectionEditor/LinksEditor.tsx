import type { ResumeLink } from '../../lib/types'
import { emptyLink } from '../../lib/types'
import { CloseIcon } from './CloseIcon'

interface Props {
  value: ResumeLink[]
  onChange: (v: ResumeLink[]) => void
  idPrefix: string
}

export function LinksEditor({ value, onChange, idPrefix }: Props) {
  function updateEntry(index: number, updated: ResumeLink) {
    const next = [...value]
    next[index] = updated
    onChange(next)
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
            <button
              type="button"
              className="btn-icon"
              onClick={() => onChange(value.filter((_, idx) => idx !== i))}
              aria-label="Remove link"
            >
              <CloseIcon />
            </button>
          </div>
          <div className="form-row-two">
            <div className="form-field">
              <label className="form-label" htmlFor={`${idPrefix}link-${i}-label`}>
                Label *
              </label>
              <input
                id={`${idPrefix}link-${i}-label`}
                className="form-input"
                placeholder="e.g. GitHub"
                value={entry.label ?? ''}
                onChange={(e) =>
                  updateEntry(i, { ...entry, label: e.target.value || null })
                }
              />
            </div>
            <div className="form-field">
              <label className="form-label" htmlFor={`${idPrefix}link-${i}-url`}>
                URL *
              </label>
              <input
                id={`${idPrefix}link-${i}-url`}
                type="url"
                className="form-input"
                placeholder="https://…"
                value={entry.url ?? ''}
                onChange={(e) =>
                  updateEntry(i, { ...entry, url: e.target.value || null })
                }
              />
            </div>
          </div>
        </div>
      ))}
      <button
        type="button"
        className="btn btn-secondary editor-add-btn"
        onClick={() => onChange([...value, emptyLink()])}
      >
        + Add link
      </button>
    </div>
  )
}
