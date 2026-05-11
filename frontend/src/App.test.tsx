import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import App from './App'

describe('App', () => {
  it('renders the dashboard with the jobs heading', () => {
    render(
      <MemoryRouter initialEntries={['/']}>
        <App />
      </MemoryRouter>,
    )

    expect(screen.getByRole('heading', { name: /^jobs$/i })).toBeInTheDocument()
  })

  it('renders sidebar navigation links for resume and tailoring', () => {
    render(
      <MemoryRouter initialEntries={['/']}>
        <App />
      </MemoryRouter>,
    )

    // Use getAllByRole since StrictMode may render twice; just confirm at least one exists
    const resumeLinks = screen.getAllByRole('link', { name: /resume/i })
    const tailoringLinks = screen.getAllByRole('link', { name: /tailoring/i })

    expect(resumeLinks.length).toBeGreaterThan(0)
    expect(tailoringLinks.length).toBeGreaterThan(0)
  })
})
