import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ToastProvider } from '../components/ui/Toast'
import { resolveDrop } from '../features/jobs/boardModel'
import type { JobSummary } from '../lib/types'
import { DashboardPage } from './DashboardPage'

const list = vi.fn()
const patchStatus = vi.fn()
vi.mock('../lib/api', () => ({
  ApiError: class extends Error {},
  jobsApi: {
    list: (...args: unknown[]) => list(...args),
    patchStatus: (...args: unknown[]) => patchStatus(...args),
  },
}))

function job(id: string, company: string, title: string, status: JobSummary['status']): JobSummary {
  return {
    id, company, title, status, link: null, dateApplied: null, notes: null,
    selectedBaseResumeId: null, selectedTailoredResumeId: null,
    createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-01-01T00:00:00Z',
  }
}

function renderPage() {
  return render(
    <ToastProvider>
      <MemoryRouter initialEntries={['/']}>
        <Routes>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/jobs/:id" element={<p>detail page</p>} />
        </Routes>
      </MemoryRouter>
    </ToastProvider>,
  )
}

const column = (name: string) => screen.getByRole('region', { name: new RegExp(`^${name},`) })

describe('DashboardPage', () => {
  beforeEach(() => {
    window.localStorage.clear()
    list.mockReset()
    patchStatus.mockReset()
    list.mockResolvedValue([
      job('1', 'Linear', 'Product Engineer', 'Saved'),
      job('2', 'Stripe', 'Backend Engineer', 'Applied'),
      job('3', 'Notion', 'Designer', 'Applied'),
    ])
  })

  it('shows a board with a column and count per status by default', async () => {
    renderPage()
    expect(await within(await screen.findByRole('region', { name: /^Saved,/ })).findByText('Product Engineer')).toBeInTheDocument()
    expect(column('Saved')).toHaveAccessibleName('Saved, 1 job')
    expect(column('Applied')).toHaveAccessibleName('Applied, 2 jobs')
    expect(column('Offer')).toHaveAccessibleName('Offer, 0 jobs')
    expect(within(column('Offer')).getByText('Nothing here yet')).toBeInTheDocument()
  })

  it('switches to list view and remembers it', async () => {
    const { unmount } = renderPage()
    await screen.findByText('Product Engineer')
    await userEvent.click(screen.getByRole('button', { name: 'List' }))
    expect(screen.getAllByRole('listitem')).toHaveLength(3)
    expect(window.localStorage.getItem('resumaire:jobs:view')).toBe('list')
    unmount()

    renderPage()
    await screen.findByText('Product Engineer')
    expect(screen.getByRole('button', { name: 'List' })).toHaveAttribute('aria-pressed', 'true')
  })

  it('searches by title or company', async () => {
    renderPage()
    await screen.findByText('Product Engineer')
    await userEvent.type(screen.getByRole('searchbox', { name: /search jobs/i }), 'stripe')
    expect(screen.getByText('Backend Engineer')).toBeInTheDocument()
    expect(screen.queryByText('Product Engineer')).not.toBeInTheDocument()
  })

  it('filters to one status column', async () => {
    renderPage()
    await screen.findByText('Product Engineer')
    await userEvent.click(screen.getByRole('button', { name: 'Applied' }))
    expect(screen.queryByText('Product Engineer')).not.toBeInTheDocument()
    expect(screen.getByText('Backend Engineer')).toBeInTheDocument()
  })

  it('moves a job via the status menu without navigating', async () => {
    patchStatus.mockResolvedValue({})
    renderPage()
    await screen.findByText('Product Engineer')
    await userEvent.click(screen.getByRole('button', { name: /product engineer at linear: status saved/i }))
    await userEvent.click(screen.getByRole('menuitemradio', { name: 'Interview' }))

    expect(patchStatus).toHaveBeenCalledTimes(1)
    expect(patchStatus).toHaveBeenCalledWith('1', 'Interview')
    await waitFor(() => expect(within(column('Interview')).getByText('Product Engineer')).toBeInTheDocument())
    expect(screen.queryByText('detail page')).not.toBeInTheDocument()
  })

  it('puts the job back and offers a retry when the change fails', async () => {
    patchStatus.mockRejectedValueOnce(new Error('nope'))
    renderPage()
    await screen.findByText('Product Engineer')
    await userEvent.click(screen.getByRole('button', { name: /product engineer at linear: status saved/i }))
    await userEvent.click(screen.getByRole('menuitemradio', { name: 'Offer' }))

    await waitFor(() => expect(within(column('Saved')).getByText('Product Engineer')).toBeInTheDocument())
    expect(screen.getByRole('button', { name: 'Retry' })).toBeInTheDocument()
  })

  it('opens the job when a card title is clicked', async () => {
    renderPage()
    await userEvent.click(await screen.findByRole('link', { name: /product engineer/i }))
    expect(screen.getByText('detail page')).toBeInTheDocument()
  })
})

describe('resolveDrop', () => {
  it('maps a drop on a status column to a move', () => {
    expect(resolveDrop('job-1', 'Interview')).toEqual({ jobId: 'job-1', status: 'Interview' })
  })
  it('ignores drops outside any column', () => {
    expect(resolveDrop('job-1', null)).toBeNull()
    expect(resolveDrop('job-1', 'job-2')).toBeNull()
  })
})
