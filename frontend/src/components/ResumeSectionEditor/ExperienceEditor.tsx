import type { ResumeExperience } from '../../lib/types'
import { emptyExperience } from '../../lib/types'
import { CloseIcon } from './CloseIcon'

interface ListProps {
  value: ResumeExperience[]
  onChange: (v: ResumeExperience[]) => void
  idPrefix: string
}

export function ExperienceEditor({ value, onChange, idPrefix }: ListProps) {
  return (
    <div className="editor-fields">
      {value.map((entry, i) => (
        <ExperienceEntryEditor
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
        onClick={() => onChange([...value, emptyExperience()])}
      >
        + Add experience
      </button>
    </div>
  )
}

interface EntryProps {
  entry: ResumeExperience
  index: number
  idPrefix: string
  onChange: (v: ResumeExperience) => void
  onRemove: () => void
}

function ExperienceEntryEditor({
  entry,
  index,
  idPrefix,
  onChange,
  onRemove,
}: EntryProps) {
  const id = (suffix: string) => `${idPrefix}exp-${index}-${suffix}`

  function update(field: keyof ResumeExperience, val: unknown) {
    onChange({ ...entry, [field]: val })
  }

  function updateBullet(bi: number, text: string) {
    const bullets = [...entry.bullets]
    bullets[bi] = text
    onChange({ ...entry, bullets })
  }

  return (
    <div className="editor-entry">
      <div className="editor-entry-header">
        <span className="editor-entry-num">{index + 1}</span>
        <span className="editor-entry-title">
          {entry.role || entry.organization || 'New experience'}
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
          <label className="form-label" htmlFor={id('role')}>
            Role *
          </label>
          <input
            id={id('role')}
            className="form-input"
            value={entry.role ?? ''}
            onChange={(e) => update('role', e.target.value || null)}
          />
        </div>
        <div className="form-field">
          <label className="form-label" htmlFor={id('org')}>
            Organization *
          </label>
          <input
            id={id('org')}
            className="form-input"
            value={entry.organization ?? ''}
            onChange={(e) => update('organization', e.target.value || null)}
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
            placeholder="e.g. Jan 2022"
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
            placeholder={entry.isCurrent ? 'Present' : 'e.g. Dec 2023'}
            disabled={entry.isCurrent}
            value={entry.isCurrent ? '' : entry.endDate ?? ''}
            onChange={(e) => update('endDate', e.target.value || null)}
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
                onClick={() =>
                  onChange({
                    ...entry,
                    bullets: entry.bullets.filter((_, k) => k !== bi),
                  })
                }
                aria-label={`Remove bullet ${bi + 1}`}
              >
                <CloseIcon size={12} />
              </button>
            </div>
          ))}
          <button
            type="button"
            className="btn-text"
            onClick={() =>
              onChange({ ...entry, bullets: [...entry.bullets, ''] })
            }
          >
            + Add bullet
          </button>
        </div>
      </div>
    </div>
  )
}
