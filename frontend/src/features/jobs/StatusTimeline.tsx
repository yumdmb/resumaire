import type { JobStatus } from '../../lib/types'

const STAGES: JobStatus[] = ['Saved', 'Applied', 'Interview', 'Offer']

/**
 * Where an application stands. Saved → Applied → Interview → Offer is the happy path;
 * Rejected is a separate end state, so when it is current no stage on the path is marked.
 */
export function StatusTimeline({ status }: { status: JobStatus }) {
  const rejected = status === 'Rejected'
  const currentIndex = rejected ? -1 : STAGES.indexOf(status)

  return (
    <ol className="timeline" aria-label="Application progress">
      {STAGES.map((stage, index) => {
        const state =
          index < currentIndex ? 'done' : index === currentIndex ? 'current' : 'upcoming'
        return (
          <li
            key={stage}
            className={`timeline-step timeline-step--${state}`}
            aria-current={state === 'current' ? 'step' : undefined}
          >
            <span className="timeline-dot" aria-hidden="true" />
            <span className="timeline-label">{stage}</span>
            {state === 'done' ? <span className="visually-hidden"> (reached)</span> : null}
          </li>
        )
      })}
      <li
        className={`timeline-step timeline-step--end${rejected ? ' timeline-step--current' : ''}`}
        aria-current={rejected ? 'step' : undefined}
      >
        <span className="timeline-dot" aria-hidden="true" />
        <span className="timeline-label">Rejected</span>
      </li>
    </ol>
  )
}
