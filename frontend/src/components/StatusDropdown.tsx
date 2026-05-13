import { useEffect, useRef, useState } from 'react'
import { JOB_STATUSES, type JobStatus } from '../lib/types'

const BADGE_CLASS: Record<JobStatus, string> = {
  Saved: 'badge badge-saved',
  Applied: 'badge badge-applied',
  Interview: 'badge badge-interview',
  Offer: 'badge badge-offer',
  Rejected: 'badge badge-rejected',
}

interface StatusDropdownProps {
  value: JobStatus
  onChange: (status: JobStatus) => void
  disabled?: boolean
}

export function StatusDropdown({ value, onChange, disabled }: StatusDropdownProps) {
  const [open, setOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) return

    function handleClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false)
      }
    }

    function handleEscape(e: KeyboardEvent) {
      if (e.key === 'Escape') {
        setOpen(false)
      }
    }

    document.addEventListener('mousedown', handleClickOutside)
    document.addEventListener('keydown', handleEscape)
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
      document.removeEventListener('keydown', handleEscape)
    }
  }, [open])

  function handleTriggerClick(e: React.MouseEvent) {
    e.preventDefault()
    e.stopPropagation()
    if (!disabled) {
      setOpen((prev) => !prev)
    }
  }

  function handleSelect(status: JobStatus) {
    if (status !== value) {
      onChange(status)
    }
    setOpen(false)
  }

  return (
    <div className="status-dropdown" ref={containerRef}>
      <button
        type="button"
        className={`status-dropdown-trigger ${BADGE_CLASS[value]}`}
        onClick={handleTriggerClick}
        disabled={disabled}
        aria-expanded={open}
        aria-haspopup="listbox"
        aria-label={`Status: ${value}. Click to change.`}
      >
        {value}
      </button>
      {open && (
        <ul className="status-dropdown-menu" role="listbox" aria-label="Select status">
          {JOB_STATUSES.map((status) => (
            <li
              key={status}
              role="option"
              aria-selected={status === value}
              className={`status-dropdown-item${status === value ? ' status-dropdown-item--active' : ''}`}
              onClick={(e) => {
                e.stopPropagation()
                handleSelect(status)
              }}
            >
              <span className={BADGE_CLASS[status]}>{status}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
