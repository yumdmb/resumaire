import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { BrandMark } from './ui/BrandMark'

interface AuthLayoutProps {
  title: string
  children: ReactNode
  footer: ReactNode
}

/** Split layout: a brand panel (hidden on small screens) beside the form. */
export function AuthLayout({ title, children, footer }: AuthLayoutProps) {
  return (
    <div className="auth-page">
      <aside className="auth-brand" aria-hidden="true">
        <Link to="/" className="auth-brand-mark" tabIndex={-1}>
          <BrandMark size={22} />
          <span>Resumaire</span>
        </Link>
        <blockquote className="auth-brand-quote">
          <p>Every line on your resume should be one you can defend in an interview.</p>
          <footer>Tailoring that only rephrases what is already true.</footer>
        </blockquote>
      </aside>
      <main className="auth-main">
        <div className="auth-card">
          <div className="auth-header">
            <Link to="/" className="auth-logo-link">
              <BrandMark size={20} />
              <span className="visually-hidden">Resumaire home</span>
            </Link>
            <h1 className="auth-title">{title}</h1>
          </div>
          {children}
          <p className="auth-footer">{footer}</p>
        </div>
      </main>
    </div>
  )
}
