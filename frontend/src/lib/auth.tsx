import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from 'react'
import type { ReactNode } from 'react'
import { frontendEnv } from './env'
import { AuthError } from './auth-error'

export { AuthError } from './auth-error'

const AUTH_TOKEN_KEY = 'resumaire:accessToken'

interface AuthUser {
  email: string
}

interface AuthContextValue {
  user: AuthUser | null
  isLoading: boolean
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) {
    throw new Error('useAuth must be used within AuthProvider')
  }
  return ctx
}

function getStoredToken(): string | null {
  try {
    return window.localStorage.getItem(AUTH_TOKEN_KEY)
  } catch {
    return null
  }
}

function storeToken(token: string): void {
  try {
    window.localStorage.setItem(AUTH_TOKEN_KEY, token)
  } catch {
    // Storage unavailable; session will not persist across reloads.
  }
}

function clearToken(): void {
  try {
    window.localStorage.removeItem(AUTH_TOKEN_KEY)
  } catch {
    // Ignore.
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [isLoading, setIsLoading] = useState(() => !!getStoredToken())

  const logout = useCallback(() => {
    clearToken()
    setUser(null)
  }, [])

  // On mount, validate existing token
  useEffect(() => {
    const token = getStoredToken()
    if (!token) return

    let cancelled = false
    ;(async () => {
      try {
        const res = await fetch(`${frontendEnv.apiBaseUrl}/api/users/me`, {
          headers: {
            Authorization: `Bearer ${token}`,
            Accept: 'application/json',
          },
        })
        if (cancelled) return
        if (!res.ok) {
          clearToken()
          setUser(null)
        } else {
          const envelope = await res.json()
          setUser({ email: envelope.data?.email ?? '' })
        }
      } catch {
        if (cancelled) return
        clearToken()
        setUser(null)
      } finally {
        if (!cancelled) setIsLoading(false)
      }
    })()

    return () => {
      cancelled = true
    }
  }, [])

  // Intercept 401 responses globally via a patched fetch wrapper
  useEffect(() => {
    const originalFetch = window.fetch

    const patchedFetch: typeof fetch = async (input, init) => {
      const response = await originalFetch(input, init)
      if (response.status === 401) {
        clearToken()
        setUser(null)
      }
      return response
    }

    window.fetch = patchedFetch
    return () => {
      window.fetch = originalFetch
    }
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    const res = await fetch(`${frontendEnv.apiBaseUrl}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify({ email, password }),
    })

    if (!res.ok) {
      const problem = await res.json().catch(() => null)
      throw new AuthError(
        problem?.title ?? problem?.detail ?? 'Invalid email or password',
        res.status,
      )
    }

    const envelope = await res.json()
    const token: string = envelope.data?.token ?? envelope.data?.accessToken ?? ''
    if (!token) throw new AuthError('No token received', 500)

    storeToken(token)
    setUser({ email })
  }, [])

  const register = useCallback(async (email: string, password: string) => {
    const res = await fetch(`${frontendEnv.apiBaseUrl}/api/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify({ email, password }),
    })

    if (!res.ok) {
      const problem = await res.json().catch(() => null)
      const detail = problem?.title ?? problem?.detail ?? 'Registration failed'
      throw new AuthError(detail, res.status, problem?.errors)
    }

    const envelope = await res.json()
    const token: string = envelope.data?.token ?? envelope.data?.accessToken ?? ''
    if (!token) throw new AuthError('No token received', 500)

    storeToken(token)
    setUser({ email })
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({ user, isLoading, login, register, logout }),
    [user, isLoading, login, register, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
