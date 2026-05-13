import type { ResumeEducation } from '../../lib/types'
import { emptyEducation } from '../../lib/types'
import { CloseIcon } from './CloseIcon'

interface ListProps {
  value: ResumeEducation[]
  onChange: (v: ResumeEducation[]) => void
  idPrefix: string
}

export function EducationEditor({ value, onChange, idPrefix }: ListProps) {
  return (
    <div className="editor-fields">
      {value.map((entry, i) => (
        <EducationEntryEditor
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
        onClick={() => onChange([...value, emptyEducation()])}
      >
        + Add education
      </button>
    </div>
  )
}

interface EntryProps {
  entry: ResumeEducation
  index: number
  idPrefix: string
  onChange: (v: ResumeEducation) => void
  onRemove: () => void
}

function EducationEntryEditor({
  entry,
  index,
  idPrefix,
  onChange,
  onRemove,
}: EntryProps) {
  const id = (suffix: string) => `${idPrefix}edu-${index}-${suffix}`

  function update(field: keyof ResumeEducation, val: unknown) {
    onChange({ ...entry, [field]: val })
  }

  function updateDetail(di: number, text: string) {
    const details = [...entry.details]
    details[di] = text
    onChange({ ...entry, details })
  }

  return (
    <div className="editor-entry">
      <div className="editor-entry-header">
        <span className="editor-entry-num">{index + 1}</span>
        <span className="editor-entry-title">
          {entry.institution || entry.degree || 'New education'}
        </span>
        <button
          type="button"
          className="btn-icon"
          onClick={onRemove}
          aria-label="Remove entry"
        >
          <CloseIcon />
        </button>
      </div>

      <div className="form-row-two">
        <div className="form-field">
          <label className="form-label" htmlFor={id('inst')}>
            Institution *
          </label>
          <input
            id={id('inst')}
            className="form-input"
            value={entry.institution ?? ''}
            onChange={(e) => update('institution', e.target.value || null)}
          />
        </div>
        <div className="form-field">
          <label className="form-label" htmlFor={id('degree')}>
            Degree
          </label>
          <input
            id={id('degree')}
            className="form-input"
            value={entry.degree ?? ''}
            onChange={(e) => update('degree', e.target.value || null)}
          />
        </div>
      </div>

      <div className="form-row-two">
        <div className="form-field">
          <label className="form-label" htmlFor={id('field')}>
            Field of study
          </label>
          <input
            id={id('field')}
            className="form-input"
            value={entry.field ?? ''}
            onChange={(e) => update('field', e.target.value || null)}
          />
        </div>
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
      </div>

      <div className="form-row-two">
        <div className="form-field">
          <label className="form-label" htmlFor={id('start')}>
            Start date
          </label>
          <input
            id={id('start')}
            className="form-input"
            placeholder="e.g. Sep 2018"
            value={entry.startDate ?? ''}
            onChange={(e) => update('startDate', e.target.value || null)}
          />
        </div>
        <div className="form-field">
          <label className="form-label" htmlFor={id('end')}>
            End date
          </label>
          <input
            id={id('end')}
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
                onClick={() =>
                  onChange({
                    ...entry,
                    details: entry.details.filter((_, k) => k !== di),
                  })
                }
                aria-label={`Remove detail ${di + 1}`}
              >
                <CloseIcon size={12} />
              </button>
            </div>
          ))}
          <button
            type="button"
            className="btn-text"
            onClick={() =>
              onChange({ ...entry, details: [...entry.details, ''] })
            }
          >
            + Add detail
          </button>
        </div>
      </div>
    </div>
  )
}
