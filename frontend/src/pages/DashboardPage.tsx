import { Link } from 'react-router-dom'
import { useState } from 'react'

type Status = 'All' | 'Saved' | 'Applied' | 'Interview' | 'Offer' | 'Rejected'

const STATUSES: Status[] = ['All', 'Saved', 'Applied', 'Interview', 'Offer', 'Rejected']

const BADGE_CLASS: Record<Exclude<Status, 'All'>, string> = {
  Saved: 'badge badge-saved',
  Applied: 'badge badge-applied',
  Interview: 'badge badge-interview',
  Offer: 'badge badge-offer',
  Rejected: 'badge badge-rejected',
}

// Placeholder rows — replaced by real data in task 4.1
const SAMPLE_JOBS = [
  { id: 'acme-product-engineer', title: 'Product Engineer', company: 'Acme Corp', status: 'Interview' as const, date: 'May 8' },
  { id: 'nova-frontend-dev', title: 'Frontend Developer', company: 'Nova Labs', status: 'Applied' as const, date: 'May 5' },
  { id: 'stripe-ux-engineer', title: 'UX Engineer', company: 'Stripe', status: 'Saved' as const, date: 'May 3' },
  { id: 'linear-software-eng', title: 'Software Engineer', company: 'Linear', status: 'Offer' as const, date: 'Apr 29' },
  { id: 'vercel-dx-eng', title: 'DX Engineer', company: 'Vercel', status: 'Rejected' as const, date: 'Apr 22' },
]

export function DashboardPage() {
  const [activeFilter, setActiveFilter] = useState<Status>('All')

  const filtered =
    activeFilter === 'All'
      ? SAMPLE_JOBS
      : SAMPLE_JOBS.filter((j) => j.status === activeFilter)

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1 className="page-title">Jobs</h1>
          <p className="page-subtitle">{SAMPLE_JOBS.length} applications</p>
        </div>
        <button className="btn btn-primary">Add job</button>
      </div>

      <div className="filter-tabs" role="tablist" aria-label="Filter by status">
        {STATUSES.map((s) => (
          <button
            key={s}
            role="tab"
            aria-selected={activeFilter === s}
            className={`filter-tab${activeFilter === s ? ' active' : ''}`}
            onClick={() => setActiveFilter(s)}
          >
            {s}
          </button>
        ))}
      </div>

      {filtered.length === 0 ? (
        <div className="empty-state">
          <p className="empty-state-title">No {activeFilter.toLowerCase()} jobs</p>
          <p className="empty-state-body">Jobs you add will appear here once you track them.</p>
        </div>
      ) : (
        <div className="job-list" role="list">
          {filtered.map((job) => (
            <Link
              key={job.id}
              to={`/jobs/${job.id}`}
              className="job-row"
              role="listitem"
            >
              <div>
                <div className="job-row-title">{job.title}</div>
                <div className="job-row-company">{job.company}</div>
              </div>
              <span className={BADGE_CLASS[job.status]}>{job.status}</span>
              <span className="job-row-date">{job.date}</span>
            </Link>
          ))}
        </div>
      )}
    </div>
  )
}
