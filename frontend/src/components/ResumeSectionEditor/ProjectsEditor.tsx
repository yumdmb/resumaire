import { Button } from '../ui/Button'
import type { ResumeProject } from '../../lib/types'
import { emptyProject } from '../../lib/types'
import { BulletsEditor } from './BulletsEditor'
import { EditorEntryHeader } from './EditorEntryHeader'

interface ListProps {
  value: ResumeProject[]
  onChange: (v: ResumeProject[]) => void
  idPrefix: string
}

export function ProjectsEditor({ value, onChange, idPrefix }: ListProps) {
  return (
    <div className="editor-fields">
      {value.map((entry, i) => (
        <ProjectEntryEditor
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
      <Button className="editor-add-btn"
        onClick={() => onChange([...value, emptyProject()])}
      >
        + Add project
      </Button>
    </div>
  )
}

interface EntryProps {
  entry: ResumeProject
  index: number
  idPrefix: string
  onChange: (v: ResumeProject) => void
  onRemove: () => void
}

function ProjectEntryEditor({ entry, index, idPrefix, onChange, onRemove }: EntryProps) {
  const id = (suffix: string) => `${idPrefix}proj-${index}-${suffix}`

  function update(field: keyof ResumeProject, val: unknown) {
    onChange({ ...entry, [field]: val })
  }

  return (
    <div className="editor-entry">
      <EditorEntryHeader
        num={index + 1}
        title={entry.name || 'New project'}
        onRemove={onRemove}
        removeLabel="Remove entry"
      />

      <div className="form-row-two">
        <div className="form-field">
          <label className="form-label" htmlFor={id('name')}>
            Name *
          </label>
          <input
            id={id('name')}
            className="form-input"
            value={entry.name ?? ''}
            onChange={(e) => update('name', e.target.value || null)}
          />
        </div>
        <div className="form-field">
          <label className="form-label" htmlFor={id('url')}>
            URL
          </label>
          <input
            id={id('url')}
            className="form-input"
            placeholder="https://github.com/you/project"
            value={entry.url ?? ''}
            onChange={(e) => update('url', e.target.value || null)}
          />
        </div>
      </div>

      <div className="form-field">
        <label className="form-label" htmlFor={id('tech')}>
          Technologies
        </label>
        <input
          id={id('tech')}
          className="form-input"
          placeholder="e.g. React, Node.js, PostgreSQL"
          value={entry.technologies ?? ''}
          onChange={(e) => update('technologies', e.target.value || null)}
        />
      </div>

      <BulletsEditor value={entry.bullets} onChange={(bullets) => update('bullets', bullets)} />
    </div>
  )
}
