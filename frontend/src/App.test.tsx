import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import App from './App'
import { AuthProvider } from './lib/auth'

const AUTH_TOKEN_KEY = 'resumaire:accessToken'

describe('App auth flows', () => {
  afterEach(() => {
    cleanup()
    window.localStorage.clear()
    vi.unstubAllGlobals()
  })

  it('logs in, stores the token, and shows the dashboard', async () => {
    const user = userEvent.setup()
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = requestUrl(input)

      if (url.endsWith('/api/auth/login')) {
        expect(init?.method).toBe('POST')
        expect(JSON.parse(String(init?.body))).toEqual({
          email: 'ada@example.test',
          password: 'correct-password',
        })
        return jsonResponse({ data: { token: 'login-token' }, message: null })
      }

      if (url.endsWith('/api/jobs')) {
        return jsonResponse({ data: [], message: null })
      }

      return jsonResponse({ title: 'Not found' }, 404)
    })
    vi.stubGlobal('fetch', fetchMock)

    renderApp('/login')

    await user.type(screen.getByLabelText(/^email$/i), 'ada@example.test')
    await user.type(screen.getByLabelText(/^password$/i), 'correct-password')
    await user.click(screen.getByRole('button', { name: /^sign in$/i }))

    expect(await screen.findByRole('heading', { name: /^jobs$/i })).toBeInTheDocument()
    expect(window.localStorage.getItem(AUTH_TOKEN_KEY)).toBe('login-token')
  })

  it('registers, signs in, stores the token, and shows the dashboard', async () => {
    const user = userEvent.setup()
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = requestUrl(input)

      if (url.endsWith('/api/auth/register')) {
        expect(init?.method).toBe('POST')
        expect(JSON.parse(String(init?.body))).toEqual({
          email: 'new@example.test',
          password: 'long-password',
        })
        return jsonResponse({ data: null, message: null })
      }

      if (url.endsWith('/api/auth/login')) {
        return jsonResponse({ data: { token: 'register-token' }, message: null })
      }

      if (url.endsWith('/api/jobs')) {
        return jsonResponse({ data: [], message: null })
      }

      return jsonResponse({ title: 'Not found' }, 404)
    })
    vi.stubGlobal('fetch', fetchMock)

    renderApp('/register')

    await user.type(screen.getByLabelText(/^email$/i), 'new@example.test')
    await user.type(screen.getByLabelText(/^password$/i), 'long-password')
    await user.type(screen.getByLabelText(/^confirm password$/i), 'long-password')
    await user.click(screen.getByRole('button', { name: /^create account$/i }))

    expect(await screen.findByRole('heading', { name: /^jobs$/i })).toBeInTheDocument()
    expect(window.localStorage.getItem(AUTH_TOKEN_KEY)).toBe('register-token')
  })

  it('redirects anonymous users away from protected routes', async () => {
    renderApp('/resume')

    expect(
      await screen.findByRole('heading', { name: /sign in to resumaire/i }),
    ).toBeInTheDocument()
  })

  it('clears the token and redirects to login after an API 401', async () => {
    window.localStorage.setItem(AUTH_TOKEN_KEY, 'stale-token')

    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = requestUrl(input)

      if (url.endsWith('/api/users/me')) {
        return jsonResponse({
          data: { email: 'ada@example.test' },
          message: null,
        })
      }

      if (url.endsWith('/api/jobs')) {
        return jsonResponse({ title: 'Unauthorized' }, 401)
      }

      return jsonResponse({ title: 'Not found' }, 404)
    })
    vi.stubGlobal('fetch', fetchMock)

    renderApp('/')

    expect(
      await screen.findByRole('heading', { name: /sign in to resumaire/i }),
    ).toBeInTheDocument()
    expect(window.localStorage.getItem(AUTH_TOKEN_KEY)).toBeNull()
  })

  it('shows inline validation on login and registration forms', async () => {
    const user = userEvent.setup()

    renderApp('/login')

    await user.type(screen.getByLabelText(/^email$/i), 'not-an-email')

    expect(screen.getByRole('alert')).toHaveTextContent('Enter a valid email address.')
    expect(screen.getByRole('button', { name: /^sign in$/i })).toBeDisabled()

    cleanup()

    renderApp('/register')

    await user.type(screen.getByLabelText(/^email$/i), 'bad-email')
    await user.type(screen.getByLabelText(/^password$/i), 'short')
    await user.type(screen.getByLabelText(/^confirm password$/i), 'different')

    expect(screen.getByText('Enter a valid email address.')).toBeInTheDocument()
    expect(screen.getByText('Must be at least 12 characters.')).toBeInTheDocument()
    expect(screen.getByText('Passwords do not match.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /^create account$/i })).toBeDisabled()
  })
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

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function requestUrl(input: RequestInfo | URL): string {
  if (typeof input === 'string') return input
  if (input instanceof URL) return input.toString()
  return input.url
}
