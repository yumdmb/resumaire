import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { ResumeContent, TailoringAnalysis, TailoringSuggestionBatch } from '../lib/types'
import { TailoringPage } from './TailoringPage'

const AUTH_TOKEN_KEY = 'resumaire:accessToken'
const JOB_ID = '11111111-1111-1111-1111-111111111111'

describe('TailoringPage', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    window.localStorage.clear()
  })

  it('keeps every decision and edit when saving fails, and sends edits on retry', async () => {
    window.localStorage.setItem(AUTH_TOKEN_KEY, 'token')
    const user = userEvent.setup()
    const saveBodies: Array<Record<string, unknown>> = []
    let failNextSave = true

    stubApi(async (url, init) => {
      if (url.endsWith('/tailoring/versions') && init?.method === 'POST') {
        saveBodies.push(JSON.parse(String(init.body)) as Record<string, unknown>)
        if (failNextSave) {
          failNextSave = false
          return problem(409, 'Suggestions can no longer be saved', 'The base resume changed after these suggestions were generated.')
        }
        return json({ data: savedVersion(), message: null }, 201)
      }
      return null
    })

    await startReview(user)

    await user.click(screen.getByRole('button', { name: /^edit$/i }))
    const editArea = screen.getByDisplayValue('Builds reliable React APIs.')
    await user.clear(editArea)
    await user.type(editArea, 'Builds reliable React APIs for hiring teams.')
    await user.click(screen.getByRole('button', { name: /^accept$/i }))
    await user.click(screen.getByRole('button', { name: /^save version$/i }))

    // The server's reason is shown and the review is still on screen with its edit.
    expect(await screen.findByRole('alert')).toHaveTextContent(/base resume changed/i)
    expect(screen.getByText('Builds reliable React APIs for hiring teams.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /^save version$/i })).toBeEnabled()

    await user.click(screen.getByRole('button', { name: /^save version$/i }))

    await screen.findByText('Version saved')
    expect(saveBodies).toHaveLength(2)
    expect(saveBodies[1]).toMatchObject({
      acceptedSuggestionIds: ['suggestion-1'],
      suggestionEdits: { 'suggestion-1': 'Builds reliable React APIs for hiring teams.' },
    })
  })

  it('exposes the current step and advances it through review and save', async () => {
    window.localStorage.setItem(AUTH_TOKEN_KEY, 'token')
    const user = userEvent.setup()
    stubApi((url, init) =>
      url.endsWith('/tailoring/versions') && init?.method === 'POST'
        ? json({ data: savedVersion(), message: null }, 201)
        : null,
    )

    render(
      <MemoryRouter initialEntries={[`/tailor?jobId=${JOB_ID}`]}>
        <TailoringPage />
      </MemoryRouter>,
    )
    const steps = () => screen.getByRole('list', { name: /tailoring steps/i })
    expect(within(steps()).getByText('Choose job').closest('li')).toHaveAttribute('aria-current', 'step')

    await user.click(await screen.findByRole('button', { name: /ai-assisted tailoring/i }))
    await screen.findByRole('button', { name: /^save version$/i })
    expect(within(steps()).getByText('Review').closest('li')).toHaveAttribute('aria-current', 'step')

    await user.click(screen.getByRole('button', { name: /^save version$/i }))
    await screen.findByText('Version saved')
    await waitFor(() =>
      expect(within(steps()).getByText('Saved').closest('li')).toHaveAttribute('aria-current', 'step'),
    )
  })

  it('returns to job selection without an error when generation is cancelled', async () => {
    window.localStorage.setItem(AUTH_TOKEN_KEY, 'token')
    const user = userEvent.setup()

    stubApi((url, init) => {
      if (url.endsWith('/tailoring/suggestions') && init?.method === 'POST') {
        return new Promise<Response>((_resolve, reject) => {
          init.signal?.addEventListener('abort', () =>
            reject(new DOMException('Aborted', 'AbortError')),
          )
        })
      }
      return null
    })

    render(
      <MemoryRouter initialEntries={[`/tailor?jobId=${JOB_ID}`]}>
        <TailoringPage />
      </MemoryRouter>,
    )

    await user.click(await screen.findByRole('button', { name: /ai-assisted tailoring/i }))
    await user.click(await screen.findByRole('button', { name: /^cancel$/i }))

    expect(await screen.findByLabelText(/select a job/i)).toBeInTheDocument()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })
})

async function startReview(user: ReturnType<typeof userEvent.setup>) {
  render(
    <MemoryRouter initialEntries={[`/tailor?jobId=${JOB_ID}`]}>
      <TailoringPage />
    </MemoryRouter>,
  )

  await user.click(await screen.findByRole('button', { name: /ai-assisted tailoring/i }))
  await screen.findByRole('button', { name: /^save version$/i })
}

/** Routes fetch by URL. The handler may claim a request by returning a response or promise. */
function stubApi(
  handler: (
    url: string,
    init?: RequestInit,
  ) => Promise<Response | null> | Response | null,
) {
  vi.stubGlobal(
    'fetch',
    vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = new URL(typeof input === 'string' ? input : input instanceof URL ? input.href : input.url)
      const claimed = await handler(url.pathname, init)
      if (claimed) return claimed

      if (url.pathname === '/api/jobs') {
        return json({ data: [job()], message: null })
      }
      if (url.pathname.endsWith('/tailoring/analysis')) {
        return json({ data: analysis(), message: null })
      }
      if (url.pathname.endsWith('/tailoring/suggestions')) {
        return json({ data: batch(), message: null })
      }
      if (url.pathname === '/api/resume/base') {
        return json({ data: baseResume(), message: null })
      }
      return problem(404, 'Not found')
    }),
  )
}

function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function problem(status: number, title: string, detail?: string): Response {
  return new Response(JSON.stringify({ title, detail, status }), {
    status,
    headers: { 'Content-Type': 'application/problem+json' },
  })
}

function job() {
  return {
    id: JOB_ID,
    company: 'Example Co',
    title: 'Frontend Engineer',
    link: null,
    status: 'Saved',
    dateApplied: null,
    notes: null,
    selectedBaseResumeId: null,
    selectedTailoredResumeId: null,
    createdAt: '2026-05-01T00:00:00Z',
    updatedAt: '2026-05-01T00:00:00Z',
  }
}

function content(): ResumeContent {
  return {
    personalInfo: {
      fullName: 'Ada Lovelace',
      email: null,
      phone: null,
      location: null,
      headline: null,
      website: null,
    },
    summary: 'Builds reliable APIs.',
    skills: [],
    experience: [],
    education: [],
    certifications: [],
    links: [],
  }
}

function baseResume() {
  return {
    id: 'base-1',
    schemaVersion: 2,
    revision: 1,
    content: content(),
    createdAt: '2026-05-01T00:00:00Z',
    updatedAt: '2026-05-01T00:00:00Z',
  }
}

function analysis(): TailoringAnalysis {
  return {
    jobId: JOB_ID,
    baseResumeId: 'base-1',
    baseResumeRevision: 1,
    extractedKeywords: [],
    comparison: { supportedKeywords: [], missingKeywords: [], reorderOpportunities: [] },
  }
}

function batch(): TailoringSuggestionBatch {
  return {
    jobId: JOB_ID,
    baseResumeId: 'base-1',
    baseResumeRevision: 1,
    suggestions: [
      {
        id: 'suggestion-1',
        reviewState: 'Pending',
        targetSection: 'Summary',
        targetPath: 'Summary',
        operation: 'Replace',
        originalContent: 'Builds reliable APIs.',
        suggestedContent: 'Builds reliable React APIs.',
        rationale: 'The job mentions React.',
        aiNotes: null,
        sourceEvidence: [],
        createdAt: '2026-05-12T00:00:00Z',
        reviewedAt: null,
      },
    ],
    gapNotes: [],
    guardrailRejections: [],
  }
}

function savedVersion() {
  return {
    id: 'tailored-1',
    versionNumber: 1,
    name: null,
    jobId: JOB_ID,
    sourceBaseResumeId: 'base-1',
    sourceBaseResumeRevision: 1,
    schemaVersion: 2,
    content: content(),
    createdAt: '2026-05-12T00:00:00Z',
    updatedAt: '2026-05-12T00:00:00Z',
  }
}

