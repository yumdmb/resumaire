import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { AuthError, useAuth } from '../lib/auth'

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const emailValid = email.length === 0 || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)

    if (!email || !password) {
      setError('Email and password are required.')
      return
    }

    setIsSubmitting(true)
    try {
      await login(email, password)
      navigate('/', { replace: true })
    } catch (err) {
      if (err instanceof AuthError) {
        setError(err.message)
      } else {
        setError('Something went wrong. Try again.')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-card">
        <div className="auth-header">
          <svg width="20" height="20" viewBox="0 0 16 16" fill="none" aria-hidden="true" className="auth-logo-icon">
            <rect x="1.5" y="1.5" width="13" height="13" rx="3" stroke="currentColor" strokeWidth="1.5" />
            <path d="M4.5 8h7M4.5 5.5h4M4.5 10.5h5.5" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" />
          </svg>
          <h1 className="auth-title">Sign in to Resumaire</h1>
        </div>

        {error && (
          <div className="form-alert" role="alert">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} noValidate>
          <div className="auth-fields">
            <div className="form-field">
              <label className="form-label" htmlFor="login-email">
                Email
              </label>
              <input
                id="login-email"
                type="email"
                className={`form-input${!emailValid ? ' form-input--invalid' : ''}`}
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                autoComplete="email"
                autoFocus
              />
              {!emailValid && (
                <p className="form-error" role="alert">
                  Enter a valid email address.
                </p>
              )}
            </div>

            <div className="form-field">
              <label className="form-label" htmlFor="login-password">
                Password
              </label>
              <input
                id="login-password"
                type="password"
                className="form-input"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                autoComplete="current-password"
              />
            </div>
          </div>

          <button
            type="submit"
            className="btn btn-primary auth-submit"
            disabled={isSubmitting || !emailValid}
          >
            {isSubmitting ? 'Signing in…' : 'Sign in'}
          </button>
        </form>

        <p className="auth-footer">
          No account? <Link to="/register" className="auth-link">Create one</Link>
        </p>
      </div>
    </div>
  )
}
