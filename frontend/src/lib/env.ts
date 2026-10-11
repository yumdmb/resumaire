const fallbackApiBaseUrl = 'http://localhost:5194'

function normalizeBaseUrl(value: string | undefined): string {
  const candidate = value?.trim()

  if (!candidate) {
    return import.meta.env.PROD ? '' : fallbackApiBaseUrl
  }

  return candidate.replace(/\/+$/, '')
}

export const frontendEnv = {
  apiBaseUrl: normalizeBaseUrl(import.meta.env.VITE_API_BASE_URL),
}
