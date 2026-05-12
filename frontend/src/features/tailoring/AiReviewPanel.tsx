import { useState } from 'react'
import type { ResumeContent, TailoringAnalysis, TailoringSuggestionBatch } from '../../lib/types'
import { applyAcceptedSuggestions } from './applyAcceptedSuggestions'
import { SuggestionCard } from './SuggestionCard'
import type { SuggestionDecision, SuggestionState } from './types'

interface Props {
  analysis: TailoringAnalysis
  batch: TailoringSuggestionBatch
  initialSuggestions: SuggestionState[]
  baseContent: ResumeContent
  onSave: (content: ResumeContent, suggestions: SuggestionState[], name: string) => void
  onCancel: () => void
}

export function AiReviewPanel({
  analysis,
  batch,
  initialSuggestions,
  baseContent,
  onSave,
  onCancel,
}: Props) {
  const [suggestions, setSuggestions] = useState<SuggestionState[]>(initialSuggestions)
  const [versionName, setVersionName] = useState('')
  const [activeTab, setActiveTab] = useState<'suggestions' | 'keywords'>('suggestions')

  const pendingCount = suggestions.filter((s) => s.decision === 'pending').length
  const acceptedCount = suggestions.filter((s) => s.decision === 'accepted').length

  function updateDecision(id: string, decision: SuggestionDecision) {
    setSuggestions((prev) =>
      prev.map((s) => (s.suggestion.id === id ? { ...s, decision } : s)),
    )
  }

  function updateEditedContent(id: string, content: string) {
    setSuggestions((prev) =>
      prev.map((s) => (s.suggestion.id === id ? { ...s, editedContent: content } : s)),
    )
  }

  function acceptAll() {
    setSuggestions((prev) =>
      prev.map((s) => ({ ...s, decision: 'accepted' as SuggestionDecision })),
    )
  }

  function rejectAll() {
    setSuggestions((prev) =>
      prev.map((s) => ({ ...s, decision: 'rejected' as SuggestionDecision })),
    )
  }

  const { comparison } = analysis

  return (
    <div className="tailor-review">
      {/* Summary bar */}
      <div className="tailor-summary-bar">
        <div className="tailor-summary-stats">
          <span className="tailor-stat">
            <span className="tailor-stat-num">{suggestions.length}</span>
            <span className="tailor-stat-label">suggestions</span>
          </span>
          <span className="tailor-stat-divider" />
          <span className="tailor-stat">
            <span className="tailor-stat-num tailor-stat-num--accepted">{acceptedCount}</span>
            <span className="tailor-stat-label">accepted</span>
          </span>
          <span className="tailor-stat-divider" />
          <span className="tailor-stat">
            <span className="tailor-stat-num tailor-stat-num--pending">{pendingCount}</span>
            <span className="tailor-stat-label">pending</span>
          </span>
        </div>
        <div className="tailor-summary-actions">
          <button
            type="button"
            className="btn btn-secondary"
            onClick={acceptAll}
            style={{ fontSize: 12, padding: '4px 10px' }}
          >
            Accept all
          </button>
          <button
            type="button"
            className="btn btn-secondary"
            onClick={rejectAll}
            style={{ fontSize: 12, padding: '4px 10px' }}
          >
            Reject all
          </button>
        </div>
      </div>

      {/* Tabs */}
      <div className="filter-tabs" style={{ marginBottom: 0 }}>
        <button
          type="button"
          className={`filter-tab${activeTab === 'suggestions' ? ' active' : ''}`}
          onClick={() => setActiveTab('suggestions')}
        >
          Suggestions
          {suggestions.length > 0 && (
            <span className="tailor-tab-count">{suggestions.length}</span>
          )}
        </button>
        <button
          type="button"
          className={`filter-tab${activeTab === 'keywords' ? ' active' : ''}`}
          onClick={() => setActiveTab('keywords')}
        >
          Keywords
        </button>
      </div>

      {activeTab === 'suggestions' && (
        <div className="tailor-suggestions">
          {suggestions.length === 0 ? (
            <div className="empty-state" style={{ marginTop: 16 }}>
              <p className="empty-state-title">No suggestions generated</p>
              <p className="empty-state-body">
                {batch.guardrailRejections.length > 0
                  ? batch.guardrailRejections.join(' ')
                  : 'The AI found no changes to suggest for this job.'}
              </p>
            </div>
          ) : (
            suggestions.map((s) => (
              <SuggestionCard
                key={s.suggestion.id}
                state={s}
                onDecision={(d) => updateDecision(s.suggestion.id, d)}
                onEditContent={(c) => updateEditedContent(s.suggestion.id, c)}
              />
            ))
          )}

          {batch.gapNotes.length > 0 && (
            <div className="tailor-gap-notes">
              <p className="tailor-gap-notes-heading">Missing keywords</p>
              <p className="tailor-gap-notes-desc">
                These keywords from the job description are not supported by your resume and were
                not added as suggestions.
              </p>
              <div className="tailor-gap-list">
                {batch.gapNotes.map((note) => (
                  <div key={note.keyword} className="tailor-gap-row">
                    <code className="tailor-gap-keyword">{note.keyword}</code>
                    <span className="tailor-gap-reason">{note.reason}</span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      )}

      {activeTab === 'keywords' && (
        <div className="tailor-keywords">
          <KeywordGroup
            label="Supported"
            count={comparison.supportedKeywords.length}
            variant="supported"
            keywords={comparison.supportedKeywords.map((sk) => sk.keyword.text)}
          />
          <KeywordGroup
            label="Missing"
            count={comparison.missingKeywords.length}
            variant="missing"
            keywords={comparison.missingKeywords.map((mk) => mk.keyword.text)}
          />
          <KeywordGroup
            label="Reorder opportunities"
            count={comparison.reorderOpportunities.length}
            variant="reorder"
            keywords={comparison.reorderOpportunities.map((ro) => ro.keyword.text)}
          />
          {comparison.supportedKeywords.length === 0 &&
            comparison.missingKeywords.length === 0 &&
            comparison.reorderOpportunities.length === 0 && (
              <p className="prose-muted" style={{ paddingTop: 16 }}>
                No keywords extracted.
              </p>
            )}
        </div>
      )}

      {/* Save bar */}
      <div className="tailor-save-bar">
        <div className="form-field" style={{ flex: 1, maxWidth: 320 }}>
          <label className="form-label" htmlFor="ai-version-name">
            Version name
          </label>
          <input
            id="ai-version-name"
            className="form-input"
            placeholder="e.g. Senior Frontend Engineer v1"
            value={versionName}
            onChange={(e) => setVersionName(e.target.value)}
          />
        </div>
        <div className="tailor-save-actions">
          <button type="button" className="btn btn-secondary" onClick={onCancel}>
            Cancel
          </button>
          <button
            type="button"
            className="btn btn-primary"
            onClick={() =>
              onSave(
                applyAcceptedSuggestions(baseContent, suggestions),
                suggestions,
                versionName,
              )
            }
          >
            Save version
          </button>
        </div>
      </div>
    </div>
  )
}

// ─── Keyword group ───────────────────────────────────────────────

function KeywordGroup({
  label,
  count,
  variant,
  keywords,
}: {
  label: string
  count: number
  variant: 'supported' | 'missing' | 'reorder'
  keywords: string[]
}) {
  if (count === 0) return null

  return (
    <div className="tailor-kw-group">
      <p className="tailor-kw-group-label">
        {label} ({count})
      </p>
      <div className="tailor-kw-list">
        {keywords.map((kw) => (
          <span key={kw} className={`tailor-kw-tag tailor-kw-tag--${variant}`}>
            {kw}
          </span>
        ))}
      </div>
    </div>
  )
}
