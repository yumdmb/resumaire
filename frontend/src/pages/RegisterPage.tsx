import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { AuthError, useAuth } from '../lib/auth'

export function RegisterPage() {
  const { register } = useAuth()
  const navigate = useNavigate()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const emailTouched = email.length > 0
  const emailValid = !emailTouched || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)

  const passwordTouched = password.length > 0
  const passwordValid = !passwordTouched || password.length >= 12

  const confirmTouched = confirmPassword.length > 0
  const confirmValid = !confirmTouched || confirmPassword === password

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)

    if (!email || !password || !confirmPassword) {
      setError('All fields are required.')
      return
    }

    if (!emailValid || !passwordValid || !confirmValid) {
      return
    }

    setIsSubmitting(true)
    try {
      await register(email, password)
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

  const canSubmit = emailValid && passwordValid && confirmValid && !isSubmitting

  return (
    <div className="auth-page">
      <div className="auth-card">
        <div className="auth-header">
          <svg width="20" height="20" viewBox="0 0 16 16" fill="none" aria-hidden="true" className="auth-logo-icon">
            <rect x="1.5" y="1.5" width="13" height="13" rx="3" stroke="currentColor" strokeWidth="1.5" />
            <path d="M4.5 8h7M4.5 5.5h4M4.5 10.5h5.5" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" />
          </svg>
          <h1 className="auth-title">Create your account</h1>
        </div>

        {error && (
          <div className="form-alert" role="alert">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} noValidate>
          <div className="auth-fields">
            <div className="form-field">
              <label className="form-label" htmlFor="register-email">
                Email
              </label>
              <input
                id="register-email"
                type="email"
                className={`form-input${emailTouched && !emailValid ? ' form-input--invalid' : ''}`}
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                autoComplete="email"
                autoFocus
              />
              {emailTouched && !emailValid && (
                <p className="form-error" role="alert">
                  Enter a valid email address.
                </p>
              )}
            </div>

            <div className="form-field">
              <label className="form-label" htmlFor="register-password">
                Password
              </label>
              <input
                id="register-password"
                type="password"
                className={`form-input${passwordTouched && !passwordValid ? ' form-input--invalid' : ''}`}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                autoComplete="new-password"
              />
              {passwordTouched && !passwordValid && (
                <p className="form-error" role="alert">
                  Must be at least 12 characters.
                </p>
              )}
            </div>

            <div className="form-field">
              <label className="form-label" htmlFor="register-confirm">
                Confirm password
              </label>
              <input
                id="register-confirm"
                type="password"
                className={`form-input${confirmTouched && !confirmValid ? ' form-input--invalid' : ''}`}
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                autoComplete="new-password"
              />
              {confirmTouched && !confirmValid && (
                <p className="form-error" role="alert">
                  Passwords do not match.
                </p>
              )}
            </div>
          </div>

          <button
            type="submit"
            className="btn btn-primary auth-submit"
            disabled={!canSubmit}
          >
            {isSubmitting ? 'Creating account…' : 'Create account'}
          </button>
        </form>

        <p className="auth-footer">
          Already have an account? <Link to="/login" className="auth-link">Sign in</Link>
        </p>
      </div>
    </div>
  )
}
