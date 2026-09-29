import { ButtonLink } from '../components/ui/Button'
import { BrandMark } from '../components/ui/BrandMark'
import { ThemeToggle } from '../components/ui/ThemeToggle'

const STEPS = [
  {
    title: 'Build one honest resume',
    body: 'Keep your real experience in a single structured base resume.',
  },
  {
    title: 'Track every application',
    body: 'Move roles from saved to offer on a board, with notes and dates.',
  },
  {
    title: 'Tailor per role, then review',
    body: 'Get suggestions for a specific job and accept or reject each one.',
  },
]

export function LandingPage() {
  return (
    <div className="landing">
      <header className="landing-bar">
        <span className="landing-logo">
          <BrandMark size={22} />
          <span className="landing-wordmark">Resumaire</span>
        </span>
        <div className="landing-bar-actions">
          <ThemeToggle />
          <ButtonLink to="/login" variant="ghost">
            Sign in
          </ButtonLink>
        </div>
      </header>

      <main>
        <section className="landing-hero">
          <div className="landing-hero-copy">
            <p className="landing-eyebrow">Job search workspace</p>
            <h1 className="landing-heading">Track applications. Tailor resumes. Stay honest.</h1>
            <p className="landing-body">
              A structured workspace for managing job applications and generating tailored resume
              versions grounded in your real experience.
            </p>
            <div className="landing-actions">
              <ButtonLink to="/register" variant="primary">
                Get started
              </ButtonLink>
              <ButtonLink to="/login">Sign in</ButtonLink>
            </div>
          </div>
          <HeroIllustration />
        </section>

        <section className="landing-section" aria-labelledby="how-heading">
          <h2 id="how-heading" className="landing-section-title">
            How it works
          </h2>
          <ol className="landing-steps">
            {STEPS.map((step, i) => (
              <li key={step.title} className="landing-step">
                <span className="landing-step-num" aria-hidden="true">
                  {String(i + 1).padStart(2, '0')}
                </span>
                <h3 className="landing-step-title">{step.title}</h3>
                <p className="landing-step-body">{step.body}</p>
              </li>
            ))}
          </ol>
        </section>

        <section className="landing-section landing-honesty" aria-labelledby="honest-heading">
          <h2 id="honest-heading" className="landing-section-title">
            Nothing invented
          </h2>
          <p className="landing-honesty-text">
            Suggestions only reword and reorder what is already in your resume. Anything the job asks
            for that your resume does not support is flagged as a gap, never added. You decide what
            goes into every version.
          </p>
        </section>
      </main>

      <footer className="landing-footer">
        <ButtonLink to="/register" variant="primary">
          Create your account
        </ButtonLink>
      </footer>
    </div>
  )
}

/** A resume sheet with two lines picked out, pointing to the job requirement they answer. */
function HeroIllustration() {
  return (
    <svg
      className="landing-illustration"
      viewBox="0 0 420 300"
      role="img"
      aria-label="A resume with two highlighted lines matched to a job posting"
    >
      <g className="ill-sheet">
        <rect x="20" y="10" width="200" height="280" rx="3" />
      </g>
      <g className="ill-line">
        <rect x="40" y="34" width="90" height="9" rx="2" className="ill-strong" />
        <rect x="40" y="56" width="150" height="5" rx="2" />
        <rect x="40" y="88" width="60" height="6" rx="2" className="ill-strong" />
        <rect x="40" y="106" width="160" height="5" rx="2" />
        <rect x="40" y="120" width="130" height="5" rx="2" className="ill-hit" />
        <rect x="40" y="134" width="150" height="5" rx="2" />
        <rect x="40" y="166" width="60" height="6" rx="2" className="ill-strong" />
        <rect x="40" y="184" width="150" height="5" rx="2" className="ill-hit" />
        <rect x="40" y="198" width="120" height="5" rx="2" />
        <rect x="40" y="230" width="60" height="6" rx="2" className="ill-strong" />
        <rect x="40" y="248" width="140" height="5" rx="2" />
      </g>
      <g className="ill-link">
        <path d="M172 123 C 240 123, 240 70, 288 70" />
        <path d="M172 187 C 240 187, 240 70, 288 70" />
      </g>
      <g className="ill-job">
        <rect x="288" y="40" width="120" height="60" rx="3" />
        <rect x="300" y="54" width="70" height="7" rx="2" className="ill-strong" />
        <rect x="300" y="72" width="94" height="5" rx="2" />
        <rect x="300" y="84" width="60" height="5" rx="2" />
      </g>
    </svg>
  )
}
