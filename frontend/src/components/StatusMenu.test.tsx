import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Link, MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { StatusMenu } from './StatusMenu'

function setup(onChange = vi.fn()) {
  render(
    <MemoryRouter initialEntries={['/']}>
      <Routes>
        <Route
          path="/"
          element={
            <Link to="/detail">
              <StatusMenu value="Saved" onChange={onChange} label="Engineer" />
            </Link>
          }
        />
        <Route path="/detail" element={<p>detail page</p>} />
      </Routes>
    </MemoryRouter>,
  )
  return onChange
}

describe('StatusMenu', () => {
  it('opens a menu with every status', async () => {
    setup()
    await userEvent.click(screen.getByRole('button', { name: /engineer: status saved/i }))
    const items = screen.getAllByRole('menuitemradio')
    expect(items.map((i) => i.textContent)).toEqual(['Saved', 'Applied', 'Interview', 'Offer', 'Rejected'])
    expect(items[0]).toHaveAttribute('aria-checked', 'true')
  })

  it('selects with the mouse without navigating', async () => {
    const onChange = setup()
    await userEvent.click(screen.getByRole('button', { name: /status saved/i }))
    await userEvent.click(screen.getByRole('menuitemradio', { name: 'Interview' }))
    expect(onChange).toHaveBeenCalledWith('Interview')
    expect(screen.queryByText('detail page')).not.toBeInTheDocument()
  })

  it('does not navigate when the trigger is clicked', async () => {
    setup()
    await userEvent.click(screen.getByRole('button', { name: /status saved/i }))
    expect(screen.queryByText('detail page')).not.toBeInTheDocument()
  })

  it('works with the keyboard and returns focus', async () => {
    const onChange = setup()
    const trigger = screen.getByRole('button', { name: /status saved/i })
    trigger.focus()
    await userEvent.keyboard('{Enter}')
    expect(screen.getByRole('menuitemradio', { name: 'Saved' })).toHaveFocus()
    await userEvent.keyboard('{ArrowDown}{ArrowDown}{Enter}')
    expect(onChange).toHaveBeenCalledWith('Interview')
    expect(trigger).toHaveFocus()
    expect(screen.queryByRole('menu')).not.toBeInTheDocument()
  })

  it('closes on Escape and returns focus', async () => {
    const onChange = setup()
    const trigger = screen.getByRole('button', { name: /status saved/i })
    await userEvent.click(trigger)
    await userEvent.keyboard('{Escape}')
    expect(screen.queryByRole('menu')).not.toBeInTheDocument()
    expect(trigger).toHaveFocus()
    expect(onChange).not.toHaveBeenCalled()
  })

  it('does not fire onChange when the same status is chosen', async () => {
    const onChange = setup()
    await userEvent.click(screen.getByRole('button', { name: /status saved/i }))
    await userEvent.click(screen.getByRole('menuitemradio', { name: 'Saved' }))
    expect(onChange).not.toHaveBeenCalled()
  })
})
