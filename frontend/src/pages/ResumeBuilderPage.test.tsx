import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ResumeBuilderPage } from './ResumeBuilderPage'
import type { BaseResumeResponse, ResumeContent } from '../lib/types'

describe('ResumeBuilderPage', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('preserves unrelated resume sections when saving a skills edit', async () => {
    const user = userEvent.setup()
    const content = createResumeContent()
    const savedRequests: Array<{ content: ResumeContent }> = []

    vi.stubGlobal(
      'fetch',
      vi.fn(async (_input: RequestInfo | URL, init?: RequestInit) => {
        if (init?.method === 'PUT') {
          const body = JSON.parse(String(init.body)) as { content: ResumeContent }
          savedRequests.push(body)

          return jsonResponse({
            data: createResumeResponse(body.content, 2),
            message: null,
          })
        }

        return jsonResponse({
          data: createResumeResponse(content, 1),
          message: null,
        })
      }),
    )

    render(
      <MemoryRouter>
        <ResumeBuilderPage />
      </MemoryRouter>,
    )

    await screen.findByRole('heading', { name: /^resume$/i })

    await user.click(screen.getByRole('button', { name: /skills/i }))
    await user.type(screen.getByLabelText(/new skill/i), 'React')
    await user.click(screen.getByRole('button', { name: /^add$/i }))
    await user.click(screen.getByRole('button', { name: /^save$/i }))

    await waitFor(() => expect(savedRequests).toHaveLength(1))

    const savedContent = savedRequests[0].content
    expect(savedContent.skills).toEqual(['ASP.NET Core', 'PostgreSQL', 'React'])
    expect(savedContent.summary).toBe('Builds reliable APIs.')
    expect(savedContent.experience[0].bullets).toEqual(['Built API workflows.'])
    expect(savedContent.education[0].institution).toBe('Example University')
    expect(savedContent.links[0].url).toBe('https://example.test/portfolio')
  })
})

function jsonResponse(body: unknown): Response {
  return new Response(JSON.stringify(body), {
    status: 200,
    headers: { 'Content-Type': 'application/json' },
  })
}

function createResumeResponse(
  content: ResumeContent,
  revision: number,
): BaseResumeResponse {
  return {
    id: 'base-resume-1',
    schemaVersion: 1,
    revision,
    content,
    createdAt: '2026-05-12T00:00:00Z',
    updatedAt: '2026-05-12T00:00:00Z',
  }
}

function createResumeContent(): ResumeContent {
  return {
    personalInfo: {
      fullName: 'Ada Lovelace',
      email: 'ada@example.test',
      phone: '',
      location: 'London',
      headline: 'Backend Engineer',
      website: 'https://example.test',
    },
    summary: 'Builds reliable APIs.',
    skills: ['ASP.NET Core', 'PostgreSQL'],
    experience: [
      {
        id: 'exp-1',
        role: 'Backend Engineer',
        organization: 'Example Co',
        location: 'Remote',
        startDate: '2024-01',
        endDate: '',
        isCurrent: true,
        bullets: ['Built API workflows.'],
      },
    ],
    education: [
      {
        id: 'edu-1',
        institution: 'Example University',
        degree: 'BS',
        field: 'Computer Science',
        location: '',
        startDate: '',
        endDate: '2022',
        details: [],
      },
    ],
    certifications: [],
    links: [
      {
        id: 'link-1',
        label: 'Portfolio',
        url: 'https://example.test/portfolio',
      },
    ],
  }
}
