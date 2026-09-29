import type { ResumeActivity } from '../../lib/types'
import { emptyActivity } from '../../lib/types'
import { BulletsEditor } from './BulletsEditor'
import { EditorEntryHeader } from './EditorEntryHeader'

interface ListProps {
  value: ResumeActivity[]
  onChange: (v: ResumeActivity[]) => void
  idPrefix: string
}

export function ActivitiesEditor({ value, onChange, idPrefix }: ListProps) {
  return (
    <div className="editor-fields">
      {value.map((entry, i) => (
        <ActivityEntryEditor
          key={entry.id ?? i}
          entry={entry}
          index={i}
          idPrefix={idPrefix}
          onChange={(updated) => {
            const next = [...value]
            next[i] = updated
            onChange(next)
          }}
          onRemove={() => onChange(value.filter((_, idx) => idx !== i))}
        />
      ))}
      <button
        type="button"
        className="btn btn-secondary editor-add-btn"
        onClick={() => onChange([...value, emptyActivity()])}
      >
        + Add activity
      </button>
    </div>
  )
}

interface EntryProps {
  entry: ResumeActivity
  index: number
  idPrefix: string
  onChange: (v: ResumeActivity) => void
  onRemove: () => void
}

function ActivityEntryEditor({ entry, index, idPrefix, onChange, onRemove }: EntryProps) {
  const id = (suffix: string) => `${idPrefix}act-${index}-${suffix}`

  function update(field: keyof ResumeActivity, val: unknown) {
    onChange({ ...entry, [field]: val })
  }

  return (
    <div className="editor-entry">
      <EditorEntryHeader
        num={index + 1}
        title={entry.title || entry.role || 'New activity'}
        onRemove={onRemove}
        removeLabel="Remove entry"
      />

      <div className="form-row-two">
        <div className="form-field">
          <label className="form-label" htmlFor={id('title')}>
            Event / organization *
          </label>
          <input
            id={id('title')}
            className="form-input"
            value={entry.title ?? ''}
            onChange={(e) => update('title', e.target.value || null)}
          />
        </div>
        <div className="form-field">
          <label className="form-label" htmlFor={id('role')}>
            Role
          </label>
          <input
            id={id('role')}
            className="form-input"
            value={entry.role ?? ''}
            onChange={(e) => update('role', e.target.value || null)}
          />
        </div>
      </div>

      <div className="form-row-two">
        <div className="form-field">
          <label className="form-label" htmlFor={id('loc')}>
            Location
          </label>
          <input
            id={id('loc')}
            className="form-input"
            value={entry.location ?? ''}
            onChange={(e) => update('location', e.target.value || null)}
          />
        </div>
        <div className="form-field">
          <label className="form-label" htmlFor={id('date')}>
            Date
          </label>
          <input
            id={id('date')}
            className="form-input"
            placeholder="e.g. 2024"
            value={entry.date ?? ''}
            onChange={(e) => update('date', e.target.value || null)}
          />
        </div>
      </div>

      <BulletsEditor value={entry.bullets} onChange={(bullets) => update('bullets', bullets)} />
    </div>
  )
}
