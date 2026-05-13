import type { ResumePersonalInfo } from '../../lib/types'

interface Props {
  value: ResumePersonalInfo | null
  onChange: (v: ResumePersonalInfo) => void
  idPrefix: string
}

const EMPTY_INFO: ResumePersonalInfo = {
  fullName: null,
  email: null,
  phone: null,
  location: null,
  headline: null,
  website: null,
}

export function PersonalInfoEditor({ value, onChange, idPrefix }: Props) {
  const info = value ?? EMPTY_INFO
  const id = (suffix: string) => `${idPrefix}pi-${suffix}`

  function update(field: keyof ResumePersonalInfo, val: string) {
    onChange({ ...info, [field]: val || null })
  }

  return (
    <div className="editor-fields">
      <div className="form-row-two">
        <div className="form-field">
          <label className="form-label" htmlFor={id('fullName')}>
            Full name <span className="form-required">*</span>
          </label>
          <input
            id={id('fullName')}
            className="form-input"
            value={info.fullName ?? ''}
            onChange={(e) => update('fullName', e.target.value)}
          />
        </div>
        <div className="form-field">
          <label className="form-label" htmlFor={id('email')}>
            Email
          </label>
          <input
            id={id('email')}
            type="email"
            className="form-input"
            value={info.email ?? ''}
            onChange={(e) => update('email', e.target.value)}
          />
        </div>
      </div>
      <div className="form-row-two">
        <div className="form-field">
          <label className="form-label" htmlFor={id('phone')}>
            Phone
          </label>
          <input
            id={id('phone')}
            className="form-input"
            value={info.phone ?? ''}
            onChange={(e) => update('phone', e.target.value)}
          />
        </div>
        <div className="form-field">
          <label className="form-label" htmlFor={id('location')}>
            Location
          </label>
          <input
            id={id('location')}
            className="form-input"
            value={info.location ?? ''}
            onChange={(e) => update('location', e.target.value)}
          />
        </div>
      </div>
      <div className="form-row-two">
        <div className="form-field">
          <label className="form-label" htmlFor={id('headline')}>
            Headline
          </label>
          <input
            id={id('headline')}
            className="form-input"
            placeholder="e.g. Senior Frontend Engineer"
            value={info.headline ?? ''}
            onChange={(e) => update('headline', e.target.value)}
          />
        </div>
        <div className="form-field">
          <label className="form-label" htmlFor={id('website')}>
            Website
          </label>
          <input
            id={id('website')}
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
