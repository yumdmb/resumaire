import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { AuthLayout } from '../components/AuthLayout'
import { Button } from '../components/ui/Button'
import { TextField } from '../components/ui/Field'
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
    <AuthLayout
      title="Create your account"
      footer={
        <>
          Already have an account? <Link to="/login" className="auth-link">Sign in</Link>
        </>
      }
    >
      {error && (
        <div className="form-alert" role="alert">
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit} noValidate>
        <div className="auth-fields">
          <TextField
            id="register-email"
            label="Email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            autoComplete="email"
            autoFocus
            error={emailTouched && !emailValid ? ['Enter a valid email address.'] : undefined}
          />
          <TextField
            id="register-password"
            label="Password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="new-password"
            error={passwordTouched && !passwordValid ? ['Must be at least 12 characters.'] : undefined}
          />
          <TextField
            id="register-confirm"
            label="Confirm password"
            type="password"
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            autoComplete="new-password"
            error={confirmTouched && !confirmValid ? ['Passwords do not match.'] : undefined}
          />
        </div>

        <Button type="submit" variant="primary" className="auth-submit" disabled={!canSubmit}>
          {isSubmitting ? 'Creating account…' : 'Create account'}
        </Button>
      </form>
    </AuthLayout>
  )
}
