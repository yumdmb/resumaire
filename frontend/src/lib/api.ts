import { frontendEnv } from './env'
import type { AiSettings, AiConnectionInput, AiCatalog, AiTestResult } from './ai-types'
import type {
  BaseResumeResponse,
  FieldErrors,
  JobDetail,
  JobFormValues,
  JobStatus,
  JobSummary,
  ResumeContent,
  TailoredResumeDetail,
  TailoringAnalysis,
  TailoringSuggestionBatch,
} from './types'

const AUTH_TOKEN_KEY = 'resumaire:accessToken'

export const aiSettingsApi = {
  async get(signal?: AbortSignal): Promise<AiSettings> {
    const result = await request<AiSettings>('/api/settings/ai/', { signal })
    if (!result) throw new ApiError('Could not load AI settings', 500)
    return result
  },
  async discover(input: AiConnectionInput, signal?: AbortSignal): Promise<AiCatalog> {
    const result = await request<AiCatalog>('/api/settings/ai/models', { method: 'POST', body: JSON.stringify(input), signal })
    if (!result) throw new ApiError('Could not load models', 500)
    return result
  },
  async test(input: AiConnectionInput, signal?: AbortSignal): Promise<AiTestResult> {
    const result = await request<AiTestResult>('/api/settings/ai/test', { method: 'POST', body: JSON.stringify(input), signal })
    if (!result) throw new ApiError('Could not test connection', 500)
    return result
  },
  async save(input: AiConnectionInput, signal?: AbortSignal): Promise<AiSettings> {
    const result = await request<AiSettings>('/api/settings/ai/', { method: 'PUT', body: JSON.stringify(input), signal })
    if (!result) throw new ApiError('Could not save AI settings', 500)
    return result
  },
  async remove(signal?: AbortSignal): Promise<void> {
    await request('/api/settings/ai/key', { method: 'DELETE', signal })
  },
}

export function getAccessToken(): string | null {
  try {
    return window.localStorage.getItem(AUTH_TOKEN_KEY)
  } catch {
    return null
  }
}

export function hasAccessToken(): boolean {
  return !!getAccessToken()
}

export class ApiError extends Error {
  readonly status: number
  readonly fieldErrors: FieldErrors
  readonly code?: string

  constructor(message: string, status: number, fieldErrors: FieldErrors = {}, code?: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.fieldErrors = fieldErrors
    this.code = code
  }

  get isUnauthorized(): boolean {
    return this.status === 401
  }

  get isNotFound(): boolean {
    return this.status === 404
  }
}

interface ApiResponseEnvelope<T> {
  data: T
  message: string | null
}

interface ProblemDetails {
  code?: string
  title?: string
  detail?: string
  status?: number
  errors?: Record<string, string[]>
}

async function request<T>(
  path: string,
  init: RequestInit = {},
): Promise<T | null> {
  const url = `${frontendEnv.apiBaseUrl}${path}`
  const token = getAccessToken()
  const headers = new Headers(init.headers)

  if (init.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }
  headers.set('Accept', 'application/json')
  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  let response: Response
  try {
    response = await fetch(url, { ...init, headers })
  } catch (error) {
    throw new ApiError(
      error instanceof Error ? error.message : 'Network error',
      0,
    )
  }

  if (response.status === 204) {
    return null
  }

  const contentType = response.headers.get('Content-Type') ?? ''
  // ASP.NET Core sends ProblemDetails as application/problem+json, so match any JSON media type.
  const isJson = contentType.includes('json')

  if (!response.ok) {
    if (isJson) {
      const problem = (await response.json().catch(() => null)) as
        | ProblemDetails
        | null
      const title =
        problem?.detail ??
        problem?.title ??
        response.statusText ??
        'Request failed'
      const fieldErrors = normalizeFieldErrors(problem?.errors)
      throw new ApiError(title, response.status, fieldErrors, problem?.code)
    }
    throw new ApiError(response.statusText || 'Request failed', response.status)
  }

  if (!isJson) {
    return null
  }

  const envelope = (await response.json()) as ApiResponseEnvelope<T>
  return envelope.data
}

function normalizeFieldErrors(
  errors: Record<string, string[]> | undefined,
): FieldErrors {
  if (!errors) return {}
  const out: FieldErrors = {}
  for (const [rawKey, messages] of Object.entries(errors)) {
    const key = rawKey.charAt(0).toLowerCase() + rawKey.slice(1)
    ;(out as Record<string, string[]>)[key] = messages
  }
  return out
}

function jobRequestBody(values: JobFormValues) {
  return JSON.stringify({
    company: values.company.trim(),
    title: values.title.trim(),
    link: values.link.trim() || null,
    description: values.description.trim(),
    status: values.status,
    dateApplied: values.dateApplied || null,
    notes: values.notes.trim() || null,
    selectedBaseResumeId: null,
    selectedTailoredResumeId: null,
  })
}

export const jobsApi = {
  async list(status?: JobStatus | 'All'): Promise<JobSummary[]> {
    const query =
      status && status !== 'All' ? `?status=${encodeURIComponent(status)}` : ''
    return (await request<JobSummary[]>(`/api/jobs${query}`)) ?? []
  },

  async get(jobId: string): Promise<JobDetail | null> {
    return request<JobDetail>(`/api/jobs/${jobId}`)
  },

  async create(values: JobFormValues): Promise<JobDetail> {
    const created = await request<JobDetail>(`/api/jobs`, {
      method: 'POST',
      body: jobRequestBody(values),
    })
    if (!created) throw new ApiError('Empty response from create', 500)
    return created
  },

  async update(jobId: string, values: JobFormValues): Promise<JobDetail> {
    const updated = await request<JobDetail>(`/api/jobs/${jobId}`, {
      method: 'PUT',
      body: jobRequestBody(values),
    })
    if (!updated) throw new ApiError('Empty response from update', 500)
    return updated
  },

  async delete(jobId: string): Promise<void> {
    await request(`/api/jobs/${jobId}`, { method: 'DELETE' })
  },

  async patchStatus(jobId: string, status: JobStatus): Promise<JobSummary> {
    const updated = await request<JobSummary>(`/api/jobs/${jobId}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ status }),
    })
    if (!updated) throw new ApiError('Empty response from patch status', 500)
    return updated
  },

  async attachTailoredResume(
    jobId: string,
    job: JobDetail,
    selectedTailoredResumeId: string | null,
  ): Promise<JobDetail> {
    const updated = await request<JobDetail>(`/api/jobs/${jobId}`, {
      method: 'PUT',
      body: JSON.stringify({
        company: job.company,
        title: job.title,
        link: job.link,
        description: job.description,
        status: job.status,
        dateApplied: job.dateApplied,
        notes: job.notes,
        selectedBaseResumeId: job.selectedBaseResumeId,
        selectedTailoredResumeId,
      }),
    })
    if (!updated) throw new ApiError('Empty response from update', 500)
    return updated
  },
}

export const resumeApi = {
  async get(): Promise<BaseResumeResponse | null> {
    try {
      return await request<BaseResumeResponse>('/api/resume/base')
    } catch (error) {
      if (error instanceof ApiError && error.isNotFound) return null
      throw error
    }
  },

  async save(content: ResumeContent): Promise<BaseResumeResponse> {
    const saved = await request<BaseResumeResponse>('/api/resume/base', {
      method: 'PUT',
      body: JSON.stringify({ content }),
    })
    if (!saved) throw new ApiError('Empty response from save', 500)
    return saved
  },
}

async function fetchPdf(path: string, failureMessage: string): Promise<Blob> {
  const token = getAccessToken()
  const headers: HeadersInit = { Accept: 'application/pdf' }
  if (token) headers['Authorization'] = `Bearer ${token}`

  const response = await fetch(`${frontendEnv.apiBaseUrl}${path}`, { headers })
  if (!response.ok) {
    const contentType = response.headers.get('Content-Type') ?? ''
    if (contentType.includes('json')) {
      const problem = (await response.json().catch(() => null)) as ProblemDetails | null
      throw new ApiError(
        problem?.detail ?? problem?.title ?? failureMessage,
        response.status,
      )
    }
    throw new ApiError(response.statusText || failureMessage, response.status)
  }
  return response.blob()
}

// Preview and export are the same Typst-rendered PDF; preview is shown inline.
export const exportApi = {
  previewBase(): Promise<Blob> {
    return fetchPdf('/api/export/base/preview', 'Preview failed')
  },

  previewTailored(tailoredResumeId: string): Promise<Blob> {
    return fetchPdf(`/api/export/tailored/${tailoredResumeId}/preview`, 'Preview failed')
  },

  exportBasePdf(): Promise<Blob> {
    return fetchPdf('/api/export/base/pdf', 'Export failed')
  },

  exportTailoredPdf(tailoredResumeId: string): Promise<Blob> {
    return fetchPdf(`/api/export/tailored/${tailoredResumeId}/pdf`, 'Export failed')
  },
}

export const tailoringApi = {
  async analyze(jobId: string, signal?: AbortSignal): Promise<TailoringAnalysis> {
    const result = await request<TailoringAnalysis>(
      `/api/jobs/${jobId}/tailoring/analysis`,
      { signal },
    )
    if (!result) throw new ApiError('Empty response from analysis', 500)
    return result
  },

  async generateSuggestions(
    jobId: string,
    signal?: AbortSignal,
  ): Promise<TailoringSuggestionBatch> {
    const result = await request<TailoringSuggestionBatch>(
      `/api/jobs/${jobId}/tailoring/suggestions`,
      { method: 'POST', signal },
    )
    if (!result) throw new ApiError('Empty response from suggestions', 500)
    return result
  },

  async listVersions(jobId: string): Promise<TailoredResumeDetail[]> {
    return (
      (await request<TailoredResumeDetail[]>(
        `/api/jobs/${jobId}/tailoring/versions`,
      )) ?? []
    )
  },

  async saveVersion(
    jobId: string,
    params: {
      name?: string
      content: ResumeContent
      acceptedSuggestionIds?: string[]
      rejectedSuggestionIds?: string[]
      /** Text the user edited, by suggestion id. The server applies these to the accepted suggestions. */
      suggestionEdits?: Record<string, string>
    },
  ): Promise<TailoredResumeDetail> {
    const saved = await request<TailoredResumeDetail>(
      `/api/jobs/${jobId}/tailoring/versions`,
      {
        method: 'POST',
        body: JSON.stringify({
          name: params.name ?? null,
          content: params.content,
          acceptedSuggestionIds: params.acceptedSuggestionIds ?? [],
          rejectedSuggestionIds: params.rejectedSuggestionIds ?? [],
          suggestionEdits: params.suggestionEdits ?? {},
        }),
      },
    )
    if (!saved) throw new ApiError('Empty response from save version', 500)
    return saved
  },
}
