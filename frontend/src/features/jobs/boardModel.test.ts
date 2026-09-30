import { describe, expect, it } from 'vitest'
import type { JobSummary } from '../../lib/types'
import { groupJobs, prepareJobs, searchJobs, sortJobs } from './boardModel'

function job(id: string, company: string, title: string, status: JobSummary['status'], extra: Partial<JobSummary> = {}): JobSummary {
  return {
    id, company, title, status, link: null, dateApplied: null, notes: null,
    selectedBaseResumeId: null, selectedTailoredResumeId: null,
    createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-01-01T00:00:00Z', ...extra,
  }
}

const jobs = [
  job('1', 'Stripe', 'Backend Engineer', 'Applied', { updatedAt: '2026-03-01T00:00:00Z', dateApplied: '2026-02-01' }),
  job('2', 'Acme', 'Designer', 'Saved', { updatedAt: '2026-05-01T00:00:00Z' }),
  job('3', 'Zed', 'Engineer', 'Applied', { updatedAt: '2026-04-01T00:00:00Z', dateApplied: '2026-03-15' }),
]

describe('groupJobs', () => {
  it('returns every status, empty ones as empty arrays', () => {
    const cols = groupJobs(jobs)
    expect(Object.keys(cols)).toEqual(['Saved', 'Applied', 'Interview', 'Offer', 'Rejected'])
    expect(cols.Applied.map((j) => j.id)).toEqual(['1', '3'])
    expect(cols.Interview).toEqual([])
  })

  it('handles empty input', () => {
    expect(groupJobs([]).Saved).toEqual([])
  })
})

describe('searchJobs', () => {
  it('matches title or company case-insensitively', () => {
    expect(searchJobs(jobs, 'STRIPE').map((j) => j.id)).toEqual(['1'])
    expect(searchJobs(jobs, 'engineer').map((j) => j.id)).toEqual(['1', '3'])
  })

  it('returns all for blank query', () => {
    expect(searchJobs(jobs, '   ')).toHaveLength(3)
  })
})

describe('sortJobs', () => {
  it('sorts by recently updated', () => {
    expect(sortJobs(jobs, 'updated').map((j) => j.id)).toEqual(['2', '3', '1'])
  })
  it('sorts by company', () => {
    expect(sortJobs(jobs, 'company').map((j) => j.id)).toEqual(['2', '1', '3'])
  })
  it('sorts by title', () => {
    expect(sortJobs(jobs, 'title').map((j) => j.id)).toEqual(['1', '2', '3'])
  })
  it('sorts by date applied, unapplied last', () => {
    expect(sortJobs(jobs, 'applied').map((j) => j.id)).toEqual(['3', '1', '2'])
  })
  it('does not mutate the input', () => {
    const before = jobs.map((j) => j.id)
    sortJobs(jobs, 'company')
    expect(jobs.map((j) => j.id)).toEqual(before)
  })
})

describe('prepareJobs', () => {
  it('combines status, search and sort', () => {
    const out = prepareJobs(jobs, { query: 'engineer', sort: 'company', status: 'Applied' })
    expect(out.map((j) => j.id)).toEqual(['1', '3'])
    expect(prepareJobs(jobs, { query: '', sort: 'updated', status: 'Saved' }).map((j) => j.id)).toEqual(['2'])
  })
})
