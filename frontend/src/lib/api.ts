import { frontendEnv } from './env'
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

  constructor(message: string, status: number, fieldErrors: FieldErrors = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.fieldErrors = fieldErrors
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
  const isJson = contentType.includes('application/json')

  if (!response.ok) {
    if (isJson) {
      const problem = (await response.json().catch(() => null)) as
        | ProblemDetails
        | null
      const title =
        problem?.title ??
        problem?.detail ??
        response.statusText ??
        'Request failed'
      const fieldErrors = normalizeFieldErrors(problem?.errors)
      throw new ApiError(title, response.status, fieldErrors)
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

export const exportApi = {
  async previewBase(): Promise<string> {
    const url = `${frontendEnv.apiBaseUrl}/api/export/base/preview`
    const token = getAccessToken()
    const headers: HeadersInit = { Accept: 'text/html' }
    if (token) headers['Authorization'] = `Bearer ${token}`

    const response = await fetch(url, { headers })
    if (!response.ok) {
      throw new ApiError(response.statusText || 'Preview failed', response.status)
    }
    return response.text()
  },

  async previewTailored(tailoredResumeId: string): Promise<string> {
    const url = `${frontendEnv.apiBaseUrl}/api/export/tailored/${tailoredResumeId}/preview`
    const token = getAccessToken()
    const headers: HeadersInit = { Accept: 'text/html' }
    if (token) headers['Authorization'] = `Bearer ${token}`

    const response = await fetch(url, { headers })
    if (!response.ok) {
      throw new ApiError(response.statusText || 'Preview failed', response.status)
    }
    return response.text()
  },

  async exportBasePdf(): Promise<Blob> {
    const url = `${frontendEnv.apiBaseUrl}/api/export/base/pdf`
    const token = getAccessToken()
    const headers: HeadersInit = { Accept: 'application/pdf' }
    if (token) headers['Authorization'] = `Bearer ${token}`

    const response = await fetch(url, { headers })
    if (!response.ok) {
      throw new ApiError(response.statusText || 'Export failed', response.status)
    }
    return response.blob()
  },

  async exportTailoredPdf(tailoredResumeId: string): Promise<Blob> {
    const url = `${frontendEnv.apiBaseUrl}/api/export/tailored/${tailoredResumeId}/pdf`
    const token = getAccessToken()
    const headers: HeadersInit = { Accept: 'application/pdf' }
    if (token) headers['Authorization'] = `Bearer ${token}`

    const response = await fetch(url, { headers })
    if (!response.ok) {
      throw new ApiError(response.statusText || 'Export failed', response.status)
    }
    return response.blob()
  },
}

export const tailoringApi = {
  async analyze(jobId: string): Promise<TailoringAnalysis> {
    const result = await request<TailoringAnalysis>(
      `/api/jobs/${jobId}/tailoring/analysis`,
    )
    if (!result) throw new ApiError('Empty response from analysis', 500)
    return result
  },

  async generateSuggestions(jobId: string): Promise<TailoringSuggestionBatch> {
    const result = await request<TailoringSuggestionBatch>(
      `/api/jobs/${jobId}/tailoring/suggestions`,
      { method: 'POST' },
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
        }),
      },
    )
    if (!saved) throw new ApiError('Empty response from save version', 500)
    return saved
  },
}
