import type {
  ResumeContent,
  TailoredResumeDetail,
  TailoringAnalysis,
  TailoringSuggestion,
  TailoringSuggestionBatch,
} from '../../lib/types'

export type SuggestionDecision = 'pending' | 'accepted' | 'rejected'

export interface SuggestionState {
  suggestion: TailoringSuggestion
  decision: SuggestionDecision
  editedContent: string
}

export type WorkflowStep =
  | { step: 'select-job' }
  | { step: 'analyzing'; jobId: string }
  | {
      step: 'review-ai'
      jobId: string
      analysis: TailoringAnalysis
      batch: TailoringSuggestionBatch
      suggestions: SuggestionState[]
      baseContent: ResumeContent
    }
  | {
      step: 'manual-edit'
      jobId: string
      content: ResumeContent
    }
  | { step: 'saving'; jobId: string }
  | { step: 'saved'; jobId: string; version: TailoredResumeDetail }
