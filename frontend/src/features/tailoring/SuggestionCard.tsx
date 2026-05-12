import { useState } from 'react'
import type { SuggestionDecision, SuggestionState } from './types'

interface Props {
  state: SuggestionState
  onDecision: (decision: SuggestionDecision) => void
  onEditContent: (content: string) => void
}

export function SuggestionCard({ state, onDecision, onEditContent }: Props) {
  const [isEditing, setIsEditing] = useState(false)
  const { suggestion, decision, editedContent } = state

  const cardClass = [
    'suggestion-card',
    decision === 'accepted' && 'suggestion-card--accepted',
    decision === 'rejected' && 'suggestion-card--rejected',
  ]
    .filter(Boolean)
    .join(' ')

  function handleEdit() {
    setIsEditing(true)
    onDecision('accepted')
  }

  function handleAccept() {
    setIsEditing(false)
    onDecision('accepted')
  }

  function handleReject() {
    setIsEditing(false)
    onDecision('rejected')
  }

  function handleReset() {
    setIsEditing(false)
    onDecision('pending')
  }

  return (
    <div className={cardClass}>
      <div className="suggestion-card-header">
        <span className="suggestion-section-tag">{suggestion.targetSection}</span>

        <div className="suggestion-decision-controls">
          {decision !== 'accepted' && (
            <button
              type="button"
              className="suggestion-btn suggestion-btn--accept"
              onClick={handleAccept}
            >
              Accept
            </button>
          )}
          {decision !== 'rejected' && (
            <button
              type="button"
              className="suggestion-btn suggestion-btn--reject"
              onClick={handleReject}
            >
              Reject
            </button>
          )}
          {!isEditing && decision !== 'rejected' && (
            <button
              type="button"
              className="suggestion-btn suggestion-btn--edit"
              onClick={handleEdit}
            >
              Edit
            </button>
          )}
          {decision !== 'pending' && (
            <button
              type="button"
              className="btn-icon"
              onClick={handleReset}
              aria-label="Reset decision"
              title="Reset to pending"
            >
              <svg width="12" height="12" viewBox="0 0 16 16" fill="none" aria-hidden="true">
                <path
                  d="M4 4l8 8M12 4l-8 8"
                  stroke="currentColor"
                  strokeWidth="1.4"
                  strokeLinecap="round"
                />
              </svg>
            </button>
          )}
        </div>
      </div>

      <div className="suggestion-card-body">
        {suggestion.originalContent && (
          <div className="suggestion-diff-row">
            <span className="suggestion-diff-label">Original</span>
            <p className="suggestion-original">{suggestion.originalContent}</p>
          </div>
        )}

        <div className="suggestion-diff-row">
          <span className="suggestion-diff-label">Suggested</span>
          {isEditing ? (
            <textarea
              className="form-input form-textarea suggestion-edit-area"
              value={editedContent}
              onChange={(e) => onEditContent(e.target.value)}
              rows={3}
              // eslint-disable-next-line jsx-a11y/no-autofocus
              autoFocus
            />
          ) : (
            <p
              className={[
                'suggestion-suggested',
                decision === 'accepted' && 'suggestion-suggested--accepted',
              ]
                .filter(Boolean)
                .join(' ')}
            >
              {editedContent}
            </p>
          )}
        </div>

        {suggestion.rationale && (
          <div className="suggestion-diff-row">
            <span className="suggestion-diff-label">Rationale</span>
            <p className="suggestion-rationale">{suggestion.rationale}</p>
          </div>
        )}

        {suggestion.sourceEvidence.length > 0 && (
          <div className="suggestion-diff-row">
            <span className="suggestion-diff-label">Source evidence</span>
            <div className="suggestion-evidence-list">
              {suggestion.sourceEvidence.map((ev, i) => (
                <span key={i} className="suggestion-evidence-item">
                  <span className="suggestion-evidence-section">{ev.section}</span>
                  <span className="suggestion-evidence-text">{ev.text}</span>
                </span>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
