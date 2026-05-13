import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type {
  ResumeContent,
  TailoringAnalysis,
  TailoringSuggestionBatch,
} from '../../lib/types'
import { AiReviewPanel } from './AiReviewPanel'
import type { SuggestionState } from './types'

describe('AiReviewPanel', () => {
  it('saves accepted edited suggestions into resume content and leaves rejected suggestions out', async () => {
    const user = userEvent.setup()
    const onSave = vi.fn()
    const baseContent = createResumeContent()
    const initialSuggestions: SuggestionState[] = [
      {
        suggestion: {
          id: 'suggestion-1',
          reviewState: 'Pending',
          targetSection: 'Experience',
          originalContent: 'Built API workflows.',
          suggestedContent: 'Built React API workflows for hiring teams.',
          rationale: 'The job emphasizes React.',
          aiNotes: null,
          sourceEvidence: [
            {
              section: 'Experience',
              path: 'Experience[0].Bullets[0]',
              text: 'Built API workflows.',
            },
          ],
          createdAt: '2026-05-12T00:00:00Z',
          reviewedAt: null,
        },
        decision: 'pending',
        editedContent: 'Built React API workflows for hiring teams.',
      },
      {
        suggestion: {
          id: 'suggestion-2',
          reviewState: 'Pending',
          targetSection: 'Summary',
          originalContent: 'Builds reliable APIs.',
          suggestedContent: 'Builds reliable React and API experiences.',
          rationale: 'The job mentions React.',
          aiNotes: null,
          sourceEvidence: [
            {
              section: 'Summary',
              path: 'Summary',
              text: 'Builds reliable APIs.',
            },
          ],
          createdAt: '2026-05-12T00:00:00Z',
          reviewedAt: null,
        },
        decision: 'pending',
        editedContent: 'Builds reliable React and API experiences.',
      },
    ]

    render(
      <AiReviewPanel
        analysis={createAnalysis()}
        batch={createBatch()}
        initialSuggestions={initialSuggestions}
        baseContent={baseContent}
        onSave={onSave}
        onCancel={vi.fn()}
      />,
    )

    await user.click(screen.getAllByRole('button', { name: /^edit$/i })[0])
    const editArea = screen.getByDisplayValue('Built React API workflows for hiring teams.')
    await user.clear(editArea)
    await user.type(
      editArea,
      'Led React API workflow improvements for hiring teams.',
    )
    await user.click(screen.getAllByRole('button', { name: /^reject$/i })[1])
    await user.click(screen.getByLabelText(/^version name$/i))
    await user.type(screen.getByLabelText(/^version name$/i), 'React role')
    await user.click(screen.getByRole('button', { name: /^save version$/i }))

    expect(onSave).toHaveBeenCalledTimes(1)
    const [savedContent, savedSuggestions, versionName] = onSave.mock.calls[0] as [
      ResumeContent,
      SuggestionState[],
      string,
    ]

    expect(savedContent.experience[0].bullets).toEqual([
      'Led React API workflow improvements for hiring teams.',
    ])
    expect(savedContent.summary).toBe('Builds reliable APIs.')
    expect(savedSuggestions.map((suggestion) => suggestion.decision)).toEqual([
      'accepted',
      'rejected',
    ])
    expect(versionName).toBe('React role')
  })
})

function createBatch(): TailoringSuggestionBatch {
  return {
    jobId: 'job-1',
    baseResumeId: 'base-resume-1',
    baseResumeRevision: 3,
    suggestions: [],
    gapNotes: [
      {
        keyword: 'Docker',
        reason: 'Docker is not supported by the resume.',
      },
    ],
    guardrailRejections: [],
  }
}

function createAnalysis(): TailoringAnalysis {
  return {
    jobId: 'job-1',
    baseResumeId: 'base-resume-1',
    baseResumeRevision: 3,
    extractedKeywords: [],
    comparison: {
      supportedKeywords: [],
      missingKeywords: [],
      reorderOpportunities: [],
    },
  }
}

function createResumeContent(): ResumeContent {
  return {
    personalInfo: {
      fullName: 'Ada Lovelace',
      email: 'ada@example.test',
      phone: '',
      location: 'London',
      headline: 'Backend Engineer',
      website: 'https://example.test',
    },
    summary: 'Builds reliable APIs.',
    skills: ['ASP.NET Core', 'PostgreSQL'],
    experience: [
      {
        id: 'exp-1',
        role: 'Backend Engineer',
        organization: 'Example Co',
        location: 'Remote',
        startDate: '2024-01',
        endDate: '',
        isCurrent: true,
        bullets: ['Built API workflows.'],
      },
    ],
    education: [],
    certifications: [],
    links: [],
  }
}
