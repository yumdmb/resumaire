import { CloseIcon } from './CloseIcon'

interface Props {
  value: string[]
  onChange: (v: string[]) => void
}

export function BulletsEditor({ value, onChange }: Props) {
  return (
    <div className="form-field">
      <span className="form-label">Bullets</span>
      <div className="bullets-list">
        {value.map((bullet, bi) => (
          <div key={bi} className="bullet-row">
            <span className="bullet-dot">•</span>
            <input
              className="form-input bullet-input"
              value={bullet}
              onChange={(e) => {
                const next = [...value]
                next[bi] = e.target.value
                onChange(next)
              }}
              aria-label={`Bullet ${bi + 1}`}
            />
            <button
              type="button"
              className="btn-icon"
              onClick={() => onChange(value.filter((_, k) => k !== bi))}
              aria-label={`Remove bullet ${bi + 1}`}
            >
              <CloseIcon size={12} />
            </button>
          </div>
        ))}
        <button
          type="button"
          className="btn-text"
          onClick={() => onChange([...value, ''])}
        >
          + Add bullet
        </button>
      </div>
    </div>
  )
}
