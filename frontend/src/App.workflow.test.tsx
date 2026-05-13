import { cleanup, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import App from './App'
import { AuthProvider } from './lib/auth'
import type {
  BaseResumeResponse,
  JobDetail,
  JobSummary,
  ResumeContent,
  TailoredResumeDetail,
  TailoringAnalysis,
  TailoringSuggestionBatch,
} from './lib/types'

const AUTH_TOKEN_KEY = 'resumaire:accessToken'

describe('workflow verification UI states', () => {
  afterEach(() => {
    cleanup()
    window.localStorage.clear()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('filters dashboard jobs by status', async () => {
    window.localStorage.setItem(AUTH_TOKEN_KEY, 'token')

    const jobs = [
      createJobSummary({ id: 'job-1', title: 'Frontend Lead', company: 'Northwind', status: 'Interview' }),
      createJobSummary({ id: 'job-2', title: 'Backend Engineer', company: 'Contoso', status: 'Saved' }),
    ]

    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL) => {
        const url = new URL(requestUrl(input))

        if (url.pathname === '/api/users/me') {
          return jsonResponse({ data: { email: 'ada@example.test' }, message: null })
        }

        if (url.pathname === '/api/jobs') {
          const status = url.searchParams.get('status')
          const data = status ? jobs.filter((job) => job.status === status) : jobs
          return jsonResponse({ data, message: null })
        }

        return problemResponse(404)
      }),
    )

    renderApp('/')

    expect(await screen.findByText('Frontend Lead')).toBeInTheDocument()
    expect(screen.getByText('Backend Engineer')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('tab', { name: /^interview$/i }))

    await waitFor(() => {
      expect(screen.getByText('Frontend Lead')).toBeInTheDocument()
      expect(screen.queryByText('Backend Engineer')).not.toBeInTheDocument()
    })
  })

  it('shows job detail saved versions and exports a tailored PDF', async () => {
    window.localStorage.setItem(AUTH_TOKEN_KEY, 'token')
    stubDownloadUrl()

    const pdfRequests: string[] = []
    const job = createJobDetail({
      selectedTailoredResumeId: 'tailored-1',
      tailoredResumeVersions: [
        {
          id: 'tailored-1',
          versionNumber: 1,
          name: 'React role',
          sourceBaseResumeId: 'base-resume-1',
          sourceBaseResumeRevision: 2,
          schemaVersion: 1,
          createdAt: '2026-05-12T00:00:00Z',
          updatedAt: '2026-05-12T00:00:00Z',
        },
      ],
    })

    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL) => {
        const url = new URL(requestUrl(input))

        if (url.pathname === '/api/users/me') {
          return jsonResponse({ data: { email: 'ada@example.test' }, message: null })
        }

        if (url.pathname === '/api/jobs/job-1') {
          return jsonResponse({ data: job, message: null })
        }

        if (url.pathname === '/api/export/tailored/tailored-1/pdf') {
          pdfRequests.push(url.pathname)
          return pdfResponse()
        }

        return problemResponse(404)
      }),
    )

    renderApp('/jobs/job-1')

    expect(await screen.findByRole('heading', { name: /^senior frontend engineer$/i })).toBeInTheDocument()
    expect(screen.getAllByText('React role')).toHaveLength(2)
    expect(screen.getByText('Attached resume')).toBeInTheDocument()

    await userEvent.click(screen.getAllByRole('button', { name: /^export pdf$/i })[0])

    await waitFor(() => expect(pdfRequests).toEqual(['/api/export/tailored/tailored-1/pdf']))
  })

  it('loads the resume preview and disables export until preview HTML is ready', async () => {
    window.localStorage.setItem(AUTH_TOKEN_KEY, 'token')
    stubDownloadUrl()

    let resolvePreview!: () => void
    const previewResponse = new Promise<Response>((resolve) => {
      resolvePreview = () => resolve(htmlResponse('<main><h1>Ada Lovelace</h1></main>'))
    })
    const pdfRequests: string[] = []

    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL) => {
        const url = new URL(requestUrl(input))

        if (url.pathname === '/api/users/me') {
          return jsonResponse({ data: { email: 'ada@example.test' }, message: null })
        }

        if (url.pathname === '/api/export/tailored/tailored-1/preview') {
          return previewResponse
        }

        if (url.pathname === '/api/export/tailored/tailored-1/pdf') {
          pdfRequests.push(url.pathname)
          return pdfResponse()
        }

        return problemResponse(404)
      }),
    )

    renderApp('/resume/preview/tailored-1?jobId=job-1')

    const exportButton = await screen.findByRole('button', { name: /^export pdf$/i })
    expect(exportButton).toBeDisabled()

    resolvePreview()

    expect(await screen.findByTitle('Resume preview')).toBeInTheDocument()
    await userEvent.click(exportButton)

    await waitFor(() => expect(pdfRequests).toEqual(['/api/export/tailored/tailored-1/pdf']))
  })

  it('completes sign in, base resume, job, tailoring, saved version, and PDF export workflow', async () => {
    const user = userEvent.setup()
    const server = createWorkflowServer()
    stubDownloadUrl()
    vi.stubGlobal('fetch', server.fetch)

    renderApp('/login')

    await user.type(screen.getByLabelText(/^email$/i), 'ada@example.test')
    await user.type(screen.getByLabelText(/^password$/i), 'correct-password')
    await user.click(screen.getByRole('button', { name: /^sign in$/i }))

    expect(await screen.findByRole('heading', { name: /^jobs$/i })).toBeInTheDocument()

    await user.click(screen.getByRole('link', { name: /^resume$/i }))
    expect(await screen.findByRole('heading', { name: /^resume$/i })).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: /summary/i }))
    await user.type(
      screen.getByLabelText(/^professional summary$/i),
      'Builds reliable React and API workflows.',
    )
    await user.click(screen.getByRole('button', { name: /^save$/i }))
    expect(await screen.findByRole('link', { name: /^preview$/i })).toBeInTheDocument()

    await user.click(screen.getByRole('link', { name: /^jobs$/i }))
    await user.click((await screen.findAllByRole('link', { name: /^add job$/i }))[0])
    await user.type(screen.getByLabelText(/^company/i), 'Northwind')
    await user.type(screen.getByLabelText(/^title/i), 'Senior Frontend Engineer')
    await user.type(
      screen.getByLabelText(/^job description/i),
      'React role focused on accessible hiring workflows.',
    )
    await user.click(screen.getByRole('button', { name: /^add job$/i }))

    expect(await screen.findByRole('heading', { name: /^senior frontend engineer$/i })).toBeInTheDocument()
    await user.click(screen.getByRole('link', { name: /^tailor resume$/i }))

    const aiButton = await screen.findByRole('button', { name: /ai-assisted tailoring/i })
    await user.click(aiButton)

    expect(await screen.findByText('The job emphasizes React.')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: /^accept$/i }))
    await user.type(screen.getByLabelText(/^version name$/i), 'React role')
    await user.click(screen.getByRole('button', { name: /^save version$/i }))

    expect(await screen.findByText(/^version saved$/i)).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: /^view job$/i }))

    const versionNames = await screen.findAllByText('React role')
    expect(versionNames).toHaveLength(2)
    const versionRow = versionNames[0].closest('.version-row')
    expect(versionRow).not.toBeNull()
    await user.click(within(versionRow as HTMLElement).getByRole('button', { name: /^export pdf$/i }))

    await waitFor(() => {
      expect(server.pdfExports).toEqual(['/api/export/tailored/tailored-1/pdf'])
    })
  }, 15000)
})

function renderApp(initialEntry: string) {
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <AuthProvider>
        <App />
      </AuthProvider>
    </MemoryRouter>,
  )
}

function createWorkflowServer() {
  let baseResume: BaseResumeResponse | null = null
  let job: JobDetail | null = null
  let tailoredVersion: TailoredResumeDetail | null = null
  const pdfExports: string[] = []

  const fetch = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(requestUrl(input))
    const method = init?.method ?? 'GET'

    if (url.pathname === '/api/auth/login' && method === 'POST') {
      return jsonResponse({ data: { token: 'login-token' }, message: null })
    }

    if (url.pathname === '/api/users/me') {
      return jsonResponse({ data: { email: 'ada@example.test' }, message: null })
    }

    if (url.pathname === '/api/resume/base' && method === 'GET') {
      return baseResume
        ? jsonResponse({ data: baseResume, message: null })
        : problemResponse(404)
    }

    if (url.pathname === '/api/resume/base' && method === 'PUT') {
      const body = JSON.parse(String(init?.body)) as { content: ResumeContent }
      baseResume = createBaseResume(body.content, 1)
      return jsonResponse({ data: baseResume, message: null })
    }

    if (url.pathname === '/api/jobs' && method === 'GET') {
      return jsonResponse({ data: job ? [toSummary(job)] : [], message: null })
    }

    if (url.pathname === '/api/jobs' && method === 'POST') {
      const body = JSON.parse(String(init?.body)) as {
        company: string
        title: string
        description: string
        status: JobDetail['status']
        link: string | null
        dateApplied: string | null
        notes: string | null
      }
      job = createJobDetail({
        company: body.company,
        title: body.title,
        description: body.description,
        status: body.status,
        link: body.link,
        dateApplied: body.dateApplied,
        notes: body.notes,
        tailoredResumeVersions: [],
      })
      return jsonResponse({ data: job, message: null })
    }

    if (url.pathname === '/api/jobs/job-1/tailoring/analysis') {
      return jsonResponse({ data: createAnalysis(), message: null })
    }

    if (url.pathname === '/api/jobs/job-1/tailoring/suggestions' && method === 'POST') {
      return jsonResponse({ data: createSuggestionBatch(), message: null })
    }

    if (url.pathname === '/api/jobs/job-1/tailoring/versions' && method === 'POST') {
      if (!baseResume || !job) return problemResponse(400)
      const body = JSON.parse(String(init?.body)) as {
        name: string | null
        content: ResumeContent
      }
      tailoredVersion = {
        id: 'tailored-1',
        versionNumber: 1,
        name: body.name,
        jobId: 'job-1',
        sourceBaseResumeId: baseResume.id,
        sourceBaseResumeRevision: baseResume.revision,
        schemaVersion: 1,
        content: body.content,
        createdAt: '2026-05-12T00:00:00Z',
        updatedAt: '2026-05-12T00:00:00Z',
      }
      job = {
        ...job,
        selectedTailoredResumeId: 'tailored-1',
        tailoredResumeVersions: [toVersionSummary(tailoredVersion)],
      }
      return jsonResponse({ data: tailoredVersion, message: null })
    }

    if (url.pathname === '/api/jobs/job-1') {
      return job ? jsonResponse({ data: job, message: null }) : problemResponse(404)
    }

    if (url.pathname === '/api/export/tailored/tailored-1/pdf') {
      pdfExports.push(url.pathname)
      return pdfResponse()
    }

    return problemResponse(404)
  })

  return { fetch, pdfExports }
}

function createBaseResume(content: ResumeContent, revision: number): BaseResumeResponse {
  return {
    id: 'base-resume-1',
    schemaVersion: 1,
    revision,
    content,
    createdAt: '2026-05-12T00:00:00Z',
    updatedAt: '2026-05-12T00:00:00Z',
  }
}

function createAnalysis(): TailoringAnalysis {
  return {
    jobId: 'job-1',
    baseResumeId: 'base-resume-1',
    baseResumeRevision: 1,
    extractedKeywords: [{ text: 'React', category: 'skill', aliases: [], mentionCount: 1 }],
    comparison: {
      supportedKeywords: [
        {
          keyword: { text: 'React', category: 'skill', aliases: [], mentionCount: 1 },
          evidence: [{ section: 'Summary', path: 'Summary', text: 'Builds reliable React and API workflows.' }],
        },
      ],
      missingKeywords: [],
      reorderOpportunities: [],
    },
  }
}

function createSuggestionBatch(): TailoringSuggestionBatch {
  return {
    jobId: 'job-1',
    baseResumeId: 'base-resume-1',
    baseResumeRevision: 1,
    suggestions: [
      {
        id: 'suggestion-1',
        reviewState: 'Pending',
        targetSection: 'Summary',
        originalContent: 'Builds reliable React and API workflows.',
        suggestedContent: 'Builds accessible React workflows for hiring teams.',
        rationale: 'The job emphasizes React.',
        aiNotes: null,
        sourceEvidence: [
          {
            section: 'Summary',
            path: 'Summary',
            text: 'Builds reliable React and API workflows.',
          },
        ],
        createdAt: '2026-05-12T00:00:00Z',
        reviewedAt: null,
      },
    ],
    gapNotes: [],
    guardrailRejections: [],
  }
}

function createJobSummary(overrides: Partial<JobSummary> = {}): JobSummary {
  return {
    id: 'job-1',
    company: 'Northwind',
    title: 'Senior Frontend Engineer',
    link: null,
    status: 'Saved',
    dateApplied: null,
    notes: null,
    selectedBaseResumeId: null,
    selectedTailoredResumeId: null,
    createdAt: '2026-05-12T00:00:00Z',
    updatedAt: '2026-05-12T00:00:00Z',
    ...overrides,
  }
}

function createJobDetail(overrides: Partial<JobDetail> = {}): JobDetail {
  return {
    ...createJobSummary(),
    description: 'React role focused on accessible hiring workflows.',
    tailoredResumeVersions: [],
    ...overrides,
  }
}

function toSummary(job: JobDetail): JobSummary {
  const { tailoredResumeVersions: _tailoredResumeVersions, description: _description, ...summary } = job
  return summary
}

function toVersionSummary(detail: TailoredResumeDetail) {
  return {
    id: detail.id,
    versionNumber: detail.versionNumber,
    name: detail.name,
    sourceBaseResumeId: detail.sourceBaseResumeId,
    sourceBaseResumeRevision: detail.sourceBaseResumeRevision,
    schemaVersion: detail.schemaVersion,
    createdAt: detail.createdAt,
    updatedAt: detail.updatedAt,
  }
}

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function htmlResponse(html: string): Response {
  return new Response(html, {
    status: 200,
    headers: { 'Content-Type': 'text/html' },
  })
}

function pdfResponse(): Response {
  return new Response(new Blob(['%PDF-1.4'], { type: 'application/pdf' }), {
    status: 200,
    headers: { 'Content-Type': 'application/pdf' },
  })
}

function problemResponse(status: number): Response {
  return jsonResponse({ title: 'Not found', status }, status)
}

function requestUrl(input: RequestInfo | URL): string {
  if (typeof input === 'string') return input
  if (input instanceof URL) return input.toString()
  return input.url
}

function stubDownloadUrl() {
  Object.defineProperty(URL, 'createObjectURL', {
    configurable: true,
    value: vi.fn(() => 'blob:resume-pdf'),
  })
  Object.defineProperty(URL, 'revokeObjectURL', {
    configurable: true,
    value: vi.fn(),
  })
}
