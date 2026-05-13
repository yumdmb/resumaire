import { formatLongDate } from '../../lib/format'
import type { TailoredResumeDetail } from '../../lib/types'

interface Props {
  version: TailoredResumeDetail
  onTailorAnother: () => void
  onViewJob: () => void
}

export function SavedConfirmation({ version, onTailorAnother, onViewJob }: Props) {
  return (
    <div className="tailor-saved">
      <div className="tailor-saved-icon" aria-hidden="true">
        <svg width="20" height="20" viewBox="0 0 16 16" fill="none">
          <path
            d="M3 8.5l3 3 7-7"
            stroke="currentColor"
            strokeWidth="1.8"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      </div>
      <div className="tailor-saved-body">
        <p className="tailor-saved-title">Version saved</p>
        <p className="tailor-saved-meta">
          {version.name ?? `Version ${version.versionNumber}`} · v{version.versionNumber} ·{' '}
          {formatLongDate(version.createdAt)}
        </p>
      </div>
      <div className="tailor-saved-actions">
        <button type="button" className="btn btn-secondary" onClick={onTailorAnother}>
          Tailor another
        </button>
        <button type="button" className="btn btn-primary" onClick={onViewJob}>
          View job
        </button>
      </div>
    </div>
  )
}
