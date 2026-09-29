import { Button } from '../ui/Button'
import type { ResumeLink } from '../../lib/types'
import { emptyLink } from '../../lib/types'
import { EditorEntryHeader } from './EditorEntryHeader'

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
          <EditorEntryHeader
            num={i + 1}
            title={entry.label || 'New link'}
            onRemove={() => onChange(value.filter((_, idx) => idx !== i))}
            removeLabel="Remove link"
          />
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
                onChange={(e) => updateEntry(i, { ...entry, label: e.target.value || null })}
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
                onChange={(e) => updateEntry(i, { ...entry, url: e.target.value || null })}
              />
            </div>
          </div>
        </div>
      ))}
      <Button className="editor-add-btn"
        onClick={() => onChange([...value, emptyLink()])}
      >
        + Add link
      </Button>
    </div>
  )
}
