import type { JobStatus } from '../../lib/types'

/** Glyph per status so the state is legible without colour. */
function Glyph({ status }: { status: JobStatus }) {
  const common = { width: 12, height: 12, viewBox: '0 0 12 12', fill: 'none', 'aria-hidden': true } as const
  switch (status) {
    case 'Saved':
      return (
        <svg {...common}>
          <circle cx="6" cy="6" r="4.2" stroke="currentColor" strokeWidth="1.4" />
        </svg>
      )
    case 'Applied':
      return (
        <svg {...common}>
          <circle cx="6" cy="6" r="4.2" stroke="currentColor" strokeWidth="1.4" />
          <path d="M6 1.8a4.2 4.2 0 0 1 0 8.4Z" fill="currentColor" />
        </svg>
      )
    case 'Interview':
      return (
        <svg {...common}>
          <rect x="2.2" y="2.2" width="7.6" height="7.6" rx="1" stroke="currentColor" strokeWidth="1.4" transform="rotate(45 6 6)" />
        </svg>
      )
    case 'Offer':
      return (
        <svg {...common}>
          <circle cx="6" cy="6" r="4.8" fill="currentColor" />
          <path d="M3.9 6.1l1.6 1.6 2.7-3" stroke="var(--surface)" strokeWidth="1.3" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      )
    case 'Rejected':
      return (
        <svg {...common}>
          <path d="M2.5 2.5l7 7M9.5 2.5l-7 7" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
        </svg>
      )
  }
}

interface StatusMarkProps {
  status: JobStatus
  /** Tinted pill form, used where the mark is a control. */
  chip?: boolean
  className?: string
}

export function StatusMark({ status, chip, className }: StatusMarkProps) {
  const cls = [
    'status-mark',
    `status-mark--${status.toLowerCase()}`,
    chip ? 'status-mark--chip' : '',
    className ?? '',
  ]
    .filter(Boolean)
    .join(' ')
  return (
    <span className={cls}>
      <Glyph status={status} />
      {status}
    </span>
  )
}
