import { Link } from 'react-router-dom'

export function LandingPage() {
  return (
    <div className="landing">
      <div className="landing-content">
        <div className="landing-logo">
          <svg width="28" height="28" viewBox="0 0 16 16" fill="none" aria-hidden="true">
            <rect x="1.5" y="1.5" width="13" height="13" rx="3" stroke="currentColor" strokeWidth="1.5" />
            <path d="M4.5 8h7M4.5 5.5h4M4.5 10.5h5.5" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" />
          </svg>
          <span className="landing-wordmark">Resumaire</span>
        </div>

        <h1 className="landing-heading">
          Track applications. Tailor resumes. Stay honest.
        </h1>

        <p className="landing-body">
          A structured workspace for managing job applications and generating
          tailored resume versions grounded in your real experience.
        </p>

        <div className="landing-actions">
          <Link to="/register" className="btn btn-primary">
            Get started
          </Link>
          <Link to="/login" className="btn btn-secondary">
            Sign in
          </Link>
        </div>
      </div>
    </div>
  )
}
