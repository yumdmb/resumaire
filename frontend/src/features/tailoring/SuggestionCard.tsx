import { useState } from 'react'
import { Button } from '../../components/ui/Button'
import type { SuggestionDecision, SuggestionState } from './types'

interface Props {
  state: SuggestionState
  onDecision: (decision: SuggestionDecision) => void
  onEditContent: (content: string) => void
}

const DECISION_LABEL: Record<SuggestionDecision, string> = {
  pending: 'Pending',
  accepted: 'Accepted',
  rejected: 'Rejected',
}

const DECISION_GLYPH: Record<SuggestionDecision, string> = {
  pending: '○',
  accepted: '✓',
  rejected: '✕',
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
    // Editing does not decide anything: the user still accepts the edited text explicitly.
    setIsEditing(true)
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
        {suggestion.targetPath && suggestion.targetPath !== suggestion.targetSection && (
          <code className="suggestion-target-path">{suggestion.targetPath}</code>
        )}
        <span className={`suggestion-state suggestion-state--${decision}`}>
          <span aria-hidden="true">{DECISION_GLYPH[decision]}</span> {DECISION_LABEL[decision]}
        </span>

        <div className="suggestion-decision-controls">
          {decision !== 'accepted' && (
            <Button small variant="primary" onClick={handleAccept}>
              Accept
            </Button>
          )}
          {decision !== 'rejected' && (
            <Button small onClick={handleReject}>
              Reject
            </Button>
          )}
          {!isEditing && decision !== 'rejected' && (
            <Button small variant="ghost" onClick={handleEdit}>
              Edit
            </Button>
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
        <div className={`suggestion-diff${suggestion.originalContent ? '' : ' suggestion-diff--single'}`}>
          {suggestion.originalContent && (
            <div className="suggestion-block suggestion-block--before">
              <span className="suggestion-diff-label">Before</span>
              <p className="suggestion-original">{suggestion.originalContent}</p>
            </div>
          )}
          <div className="suggestion-block suggestion-block--after">
            <span className="suggestion-diff-label">After</span>
            {isEditing ? (
              <textarea
                className="form-input form-textarea suggestion-edit-area"
                aria-label="Edit suggested text"
                value={editedContent}
                onChange={(e) => onEditContent(e.target.value)}
                rows={3}
                autoFocus
              />
            ) : (
              <p className="suggestion-suggested">{editedContent}</p>
            )}
          </div>
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
