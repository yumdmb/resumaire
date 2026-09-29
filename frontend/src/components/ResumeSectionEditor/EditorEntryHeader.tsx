import { useState } from 'react'
import { CloseIcon } from './CloseIcon'

interface EditorEntryHeaderProps {
  num: number
  title: string
  onRemove: () => void
  removeLabel?: string
}

/**
 * Header of a repeatable entry (experience, education, ...). The toggle's aria-expanded drives
 * `.editor-entry:has(...)` in CSS, which hides the entry's fields while collapsed. Values stay in the DOM.
 */
export function EditorEntryHeader({
  num,
  title,
  onRemove,
  removeLabel = 'Remove entry',
}: EditorEntryHeaderProps) {
  const [expanded, setExpanded] = useState(true)

  return (
    <div className="editor-entry-header">
      <span className="editor-entry-num">{num}</span>
      <span className="editor-entry-title">{title}</span>
      <button
        type="button"
        className="btn-icon editor-entry-toggle"
        aria-expanded={expanded}
        aria-label={`${expanded ? 'Collapse' : 'Expand'} ${title}`}
        onClick={() => setExpanded((v) => !v)}
      >
        <svg width="14" height="14" viewBox="0 0 16 16" fill="none" aria-hidden="true">
          <path d="M4 6l4 4 4-4" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </button>
      <button type="button" className="btn-icon" onClick={onRemove} aria-label={removeLabel}>
        <CloseIcon />
      </button>
    </div>
  )
}
