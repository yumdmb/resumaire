import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { StatusTimeline } from './StatusTimeline'

describe('StatusTimeline', () => {
  it('marks the current stage', () => {
    render(<StatusTimeline status="Interview" />)
    const current = screen.getAllByRole('listitem').filter((li) => li.getAttribute('aria-current') === 'step')
    expect(current).toHaveLength(1)
    expect(current[0]).toHaveTextContent('Interview')
  })

  it('treats Rejected as its own end state', () => {
    render(<StatusTimeline status="Rejected" />)
    const current = screen.getAllByRole('listitem').filter((li) => li.getAttribute('aria-current') === 'step')
    expect(current).toHaveLength(1)
    expect(current[0]).toHaveTextContent('Rejected')
  })
})
