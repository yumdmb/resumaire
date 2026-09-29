import { NavLink, Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { ProtectedRoute } from './components/ProtectedRoute'
import { ThemeToggle } from './components/ui/ThemeToggle'
import { useAuth } from './lib/auth'
import { DashboardPage } from './pages/DashboardPage'
import { JobDetailPage } from './pages/JobDetailPage'
import { JobFormPage } from './pages/JobFormPage'
import { LandingPage } from './pages/LandingPage'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { ResumeBuilderPage } from './pages/ResumeBuilderPage'
import { ResumePreviewPage } from './pages/ResumePreviewPage'
import { TailoringPage } from './pages/TailoringPage'

const nav = [
  {
    to: '/',
    end: true,
    label: 'Jobs',
    icon: (
      <svg width="20" height="20" viewBox="0 0 16 16" fill="none" aria-hidden="true">
        <rect x="2" y="4" width="12" height="9" rx="1.5" stroke="currentColor" strokeWidth="1.4" />
        <path d="M5 4V3a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v1" stroke="currentColor" strokeWidth="1.4" />
        <path d="M2 8h12" stroke="currentColor" strokeWidth="1.4" />
      </svg>
    ),
  },
  {
    to: '/resume',
    label: 'Resume',
    icon: (
      <svg width="20" height="20" viewBox="0 0 16 16" fill="none" aria-hidden="true">
        <rect x="3" y="1.5" width="10" height="13" rx="1.5" stroke="currentColor" strokeWidth="1.4" />
        <path d="M5.5 5.5h5M5.5 8h5M5.5 10.5h3" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" />
      </svg>
    ),
  },
  {
    to: '/tailor',
    label: 'Tailoring',
    icon: (
      <svg width="20" height="20" viewBox="0 0 16 16" fill="none" aria-hidden="true">
        <path d="M2 8h3l2-5 2 10 2-5h3" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round" />
      </svg>
    ),
  },
]

function AppShell() {
  const { logout } = useAuth()

  return (
    <div className="app-shell">
      <aside className="rail">
        <div className="rail-brand" aria-label="Resumaire">
          <span className="rail-mark" aria-hidden="true">
            R
          </span>
          <span className="rail-wordmark">Resumaire</span>
        </div>

        <nav className="rail-nav" aria-label="Primary">
          {nav.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) => `nav-item${isActive ? ' active' : ''}`}
            >
              {item.icon}
              {item.label}
            </NavLink>
          ))}
        </nav>

        <div className="rail-spacer" />

        <div className="rail-foot">
          <ThemeToggle />
          <button type="button" className="rail-button" onClick={logout}>
            <svg width="20" height="20" viewBox="0 0 16 16" fill="none" aria-hidden="true">
              <path d="M6 2.5H3.5a1 1 0 0 0-1 1v9a1 1 0 0 0 1 1H6M10 5l3 3-3 3M13 8H6" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
            Sign out
          </button>
        </div>
      </aside>

      <main className="main">
        <Routes>
          <Route index element={<DashboardPage />} />
          <Route path="jobs/new" element={<JobFormPage mode="create" />} />
          <Route path="jobs/:jobId" element={<JobDetailPage />} />
          <Route path="jobs/:jobId/edit" element={<JobFormPage mode="edit" />} />
          <Route path="resume" element={<ResumeBuilderPage />} />
          <Route path="resume/preview" element={<ResumePreviewPage />} />
          <Route path="resume/preview/:tailoredResumeId" element={<ResumePreviewPage />} />
          <Route path="tailor" element={<TailoringPage />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>
    </div>
  )
}

/** Shows landing for anonymous users, dashboard shell for authenticated. */
function RootRoute() {
  const { user, isLoading } = useAuth()
  const location = useLocation()

  if (isLoading) {
    return (
      <div className="auth-loading" aria-busy="true">
        <div className="auth-loading-spinner" />
      </div>
    )
  }

  if (!user) {
    if (location.pathname === '/') {
      return <LandingPage />
    }

    return (
      <ProtectedRoute>
        <AppShell />
      </ProtectedRoute>
    )
  }

  return (
    <ProtectedRoute>
      <AppShell />
    </ProtectedRoute>
  )
}

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/*" element={<RootRoute />} />
    </Routes>
  )
}

export default App
