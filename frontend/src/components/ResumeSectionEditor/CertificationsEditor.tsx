import type { ResumeCertification } from '../../lib/types'
import { emptyCertification } from '../../lib/types'
import { CloseIcon } from './CloseIcon'

interface ListProps {
  value: ResumeCertification[]
  onChange: (v: ResumeCertification[]) => void
  idPrefix: string
}

export function CertificationsEditor({ value, onChange, idPrefix }: ListProps) {
  return (
    <div className="editor-fields">
      {value.map((entry, i) => (
        <CertificationEntryEditor
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
        onClick={() => onChange([...value, emptyCertification()])}
      >
        + Add certification
      </button>
    </div>
  )
}

interface EntryProps {
  entry: ResumeCertification
  index: number
  idPrefix: string
  onChange: (v: ResumeCertification) => void
  onRemove: () => void
}

function CertificationEntryEditor({
  entry,
  index,
  idPrefix,
  onChange,
  onRemove,
}: EntryProps) {
  const id = (suffix: string) => `${idPrefix}cert-${index}-${suffix}`

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
          <label className="form-label" htmlFor={id('issuer')}>
            Issuer *
          </label>
          <input
            id={id('issuer')}
            className="form-input"
            value={entry.issuer ?? ''}
            onChange={(e) => update('issuer', e.target.value || null)}
          />
        </div>
      </div>

      <div className="form-row-two">
        <div className="form-field">
          <label className="form-label" htmlFor={id('issued')}>
            Issued date
          </label>
          <input
            id={id('issued')}
            className="form-input"
            placeholder="e.g. Mar 2023"
            value={entry.issuedDate ?? ''}
            onChange={(e) => update('issuedDate', e.target.value || null)}
          />
        </div>
        <div className="form-field">
          <label className="form-label" htmlFor={id('exp')}>
            Expiration date
          </label>
          <input
            id={id('exp')}
            className="form-input"
            placeholder="e.g. Mar 2026"
            value={entry.expirationDate ?? ''}
            onChange={(e) => update('expirationDate', e.target.value || null)}
          />
        </div>
      </div>

      <div className="form-row-two">
        <div className="form-field">
          <label className="form-label" htmlFor={id('cred')}>
            Credential ID
          </label>
          <input
            id={id('cred')}
            className="form-input"
            value={entry.credentialId ?? ''}
            onChange={(e) => update('credentialId', e.target.value || null)}
          />
        </div>
        <div className="form-field">
          <label className="form-label" htmlFor={id('url')}>
            URL
          </label>
          <input
            id={id('url')}
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
