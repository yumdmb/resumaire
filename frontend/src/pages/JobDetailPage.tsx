import { Link, useParams } from 'react-router-dom'

// Placeholder data — replaced by real API data in task 4.3
const SAMPLE: Record<string, { title: string; company: string; status: string; date: string; link: string }> = {
  'acme-product-engineer': {
    title: 'Product Engineer',
    company: 'Acme Corp',
    status: 'Interview',
    date: 'May 8, 2026',
    link: 'https://example.com/jobs/acme',
  },
}

const BADGE_CLASS: Record<string, string> = {
  Saved: 'badge badge-saved',
  Applied: 'badge badge-applied',
  Interview: 'badge badge-interview',
  Offer: 'badge badge-offer',
  Rejected: 'badge badge-rejected',
}

export function JobDetailPage() {
  const { jobId = '' } = useParams()
  const job = SAMPLE[jobId]

  if (!job) {
    return (
      <div className="page">
        <div className="page-header">
          <div>
            <Link to="/" className="page-subtitle" style={{ display: 'inline-flex', alignItems: 'center', gap: 4 }}>
              ← Jobs
            </Link>
            <h1 className="page-title" style={{ marginTop: 4 }}>Job not found</h1>
          </div>
        </div>
        <div className="empty-state">
          <p className="empty-state-title">No job with this ID</p>
          <p className="empty-state-body">It may have been removed or the link is incorrect.</p>
        </div>
      </div>
    )
  }

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <Link to="/" className="page-subtitle" style={{ display: 'inline-flex', alignItems: 'center', gap: 4 }}>
            ← Jobs
          </Link>
          <h1 className="page-title" style={{ marginTop: 4 }}>{job.title}</h1>
          <p className="page-subtitle">{job.company}</p>
        </div>
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          <span className={BADGE_CLASS[job.status] ?? 'badge badge-saved'}>{job.status}</span>
          <button className="btn btn-secondary">Edit</button>
          <button className="btn btn-primary">Tailor resume</button>
        </div>
      </div>

      <div className="detail-grid">
        <div className="detail-main">
          <div className="card">
            <div className="card-header">
              <span className="card-title">Job description</span>
            </div>
            <div className="card-body">
              <p style={{ fontSize: 13, color: 'var(--text-secondary)', lineHeight: 1.7 }}>
                Job description will appear here once connected to the backend.
              </p>
            </div>
          </div>

          <div className="card">
            <div className="card-header">
              <span className="card-title">Tailored versions</span>
              <button className="btn btn-secondary" style={{ fontSize: 12, padding: '4px 10px' }}>New version</button>
            </div>
            <div className="card-body">
              <div className="empty-state" style={{ border: 'none', padding: '24px 0' }}>
                <p className="empty-state-title">No tailored versions yet</p>
                <p className="empty-state-body">Tailor your resume for this role to create a version.</p>
              </div>
            </div>
          </div>
        </div>

        <div className="detail-aside">
          <div className="card">
            <div className="card-header">
              <span className="card-title">Details</span>
            </div>
            <div className="card-body">
              <div className="meta-list">
                <div className="meta-row">
                  <span className="meta-label">Status</span>
                  <span className={BADGE_CLASS[job.status] ?? 'badge badge-saved'}>{job.status}</span>
                </div>
                <div className="meta-row">
                  <span className="meta-label">Added</span>
                  <span className="meta-value">{job.date}</span>
                </div>
                <div className="meta-row">
                  <span className="meta-label">Posting</span>
                  <a
                    href={job.link}
                    target="_blank"
                    rel="noopener noreferrer"
                    style={{ fontSize: 13, color: 'var(--accent-text)', fontWeight: 500 }}
                  >
                    View ↗
                  </a>
                </div>
              </div>
            </div>
          </div>

          <div className="card">
            <div className="card-header">
              <span className="card-title">Notes</span>
            </div>
            <div className="card-body">
              <p style={{ fontSize: 13, color: 'var(--text-tertiary)' }}>No notes yet.</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
