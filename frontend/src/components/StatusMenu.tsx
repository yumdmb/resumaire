import { useEffect, useId, useLayoutEffect, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { JOB_STATUSES, type JobStatus } from '../lib/types'
import { StatusMark } from './ui/StatusMark'

interface StatusMenuProps {
  value: JobStatus
  onChange: (status: JobStatus) => void
  disabled?: boolean
  /** Used in the trigger's accessible name, e.g. the job title. */
  label?: string
  /** Small icon-only trigger, for places that already show the status (board cards). */
  compact?: boolean
}

interface Position {
  top?: number
  bottom?: number
  left: number
}

const MENU_HEIGHT_GUESS = 220

/**
 * Menu-button for changing a job's status. The menu renders in a portal, and every event that
 * originates here stops at this component, so it never navigates a surrounding link or starts a drag.
 */
export function StatusMenu({ value, onChange, disabled, label, compact }: StatusMenuProps) {
  const [open, setOpen] = useState(false)
  const [position, setPosition] = useState<Position | null>(null)
  const triggerRef = useRef<HTMLButtonElement>(null)
  const menuRef = useRef<HTMLUListElement>(null)
  const menuId = useId()

  function openMenu() {
    const rect = triggerRef.current?.getBoundingClientRect()
    if (rect) {
      const left = Math.max(8, Math.min(rect.left, window.innerWidth - 168))
      const fitsBelow = window.innerHeight - rect.bottom > MENU_HEIGHT_GUESS
      setPosition(
        fitsBelow
          ? { top: rect.bottom + 4, left }
          : { bottom: window.innerHeight - rect.top + 4, left },
      )
    }
    setOpen(true)
  }

  function close(returnFocus: boolean) {
    setOpen(false)
    if (returnFocus) triggerRef.current?.focus()
  }

  // Move focus into the menu (on the current status) when it opens.
  useLayoutEffect(() => {
    if (!open) return
    const items = menuRef.current?.querySelectorAll<HTMLButtonElement>('[role="menuitemradio"]')
    const current = Array.from(items ?? []).find((el) => el.getAttribute('aria-checked') === 'true')
    ;(current ?? items?.[0])?.focus()
  }, [open])

  useEffect(() => {
    if (!open) return
    function onPointerDown(event: PointerEvent) {
      const target = event.target as Node
      if (menuRef.current?.contains(target) || triggerRef.current?.contains(target)) return
      setOpen(false)
    }
    function onScrollOrResize() {
      setOpen(false)
    }
    document.addEventListener('pointerdown', onPointerDown)
    window.addEventListener('resize', onScrollOrResize)
    window.addEventListener('scroll', onScrollOrResize, true)
    return () => {
      document.removeEventListener('pointerdown', onPointerDown)
      window.removeEventListener('resize', onScrollOrResize)
      window.removeEventListener('scroll', onScrollOrResize, true)
    }
  }, [open])

  function select(status: JobStatus) {
    close(true)
    if (status !== value) onChange(status)
  }

  function onMenuKeyDown(event: React.KeyboardEvent) {
    event.stopPropagation()
    const items = Array.from(
      menuRef.current?.querySelectorAll<HTMLButtonElement>('[role="menuitemradio"]') ?? [],
    )
    const index = items.indexOf(document.activeElement as HTMLButtonElement)
    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault()
        items[(index + 1) % items.length]?.focus()
        break
      case 'ArrowUp':
        event.preventDefault()
        items[(index - 1 + items.length) % items.length]?.focus()
        break
      case 'Home':
        event.preventDefault()
        items[0]?.focus()
        break
      case 'End':
        event.preventDefault()
        items[items.length - 1]?.focus()
        break
      case 'Escape':
        event.preventDefault()
        close(true)
        break
      case 'Tab':
        close(false)
        break
    }
  }

  const stop = (event: React.SyntheticEvent) => event.stopPropagation()
  const stopProps = { onPointerDown: stop, onMouseDown: stop, onTouchStart: stop }

  return (
    <>
      <button
        ref={triggerRef}
        type="button"
        className={`status-menu-trigger${compact ? ' status-menu-trigger--compact' : ''}`}
        disabled={disabled}
        aria-haspopup="menu"
        aria-expanded={open}
        aria-controls={open ? menuId : undefined}
        aria-label={`${label ? `${label}: ` : ''}Status ${value}. Change status`}
        {...stopProps}
        onKeyDown={stop}
        onClick={(event) => {
          event.preventDefault()
          event.stopPropagation()
          if (open) close(false)
          else openMenu()
        }}
      >
        {compact ? (
          <svg width="14" height="14" viewBox="0 0 14 14" fill="none" aria-hidden="true">
            <path d="M2 5h9M8.5 2.5L11 5 8.5 7.5M12 9H3M5.5 6.5L3 9l2.5 2.5" stroke="currentColor" strokeWidth="1.3" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        ) : (
          <>
            <StatusMark status={value} chip />
            <svg width="8" height="8" viewBox="0 0 8 8" fill="none" aria-hidden="true">
              <path d="M1.5 3l2.5 2.5L6.5 3" stroke="currentColor" strokeWidth="1.3" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
          </>
        )}
      </button>
      {open && position
        ? createPortal(
            <ul
              ref={menuRef}
              id={menuId}
              role="menu"
              aria-label="Set status"
              className="status-menu"
              style={{ position: 'fixed', ...position }}
              {...stopProps}
              onClick={stop}
              onKeyDown={onMenuKeyDown}
            >
              {JOB_STATUSES.map((status) => (
                <li key={status} role="none">
                  <button
                    type="button"
                    role="menuitemradio"
                    aria-checked={status === value}
                    className="status-menu-item"
                    onClick={(event) => {
                      event.preventDefault()
                      event.stopPropagation()
                      select(status)
                    }}
                  >
                    <StatusMark status={status} />
                    {status === value ? (
                      <svg width="12" height="12" viewBox="0 0 12 12" fill="none" aria-hidden="true">
                        <path d="M2.5 6.3l2.4 2.4 4.6-5" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round" />
                      </svg>
                    ) : null}
                  </button>
                </li>
              ))}
            </ul>,
            document.body,
          )
        : null}
    </>
  )
}
