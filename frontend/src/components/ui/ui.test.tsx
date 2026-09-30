import { act, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { JOB_STATUSES } from '../../lib/types'
import { Button, ButtonLink } from './Button'
import { Card } from './Card'
import { EmptyState } from './EmptyState'
import { TextField } from './Field'
import { PageHeader } from './PageHeader'
import { StatusMark } from './StatusMark'
import { ToastProvider } from './Toast'
import { useToast } from './toast-context'

describe('StatusMark', () => {
  it.each(JOB_STATUSES)('always renders the %s text label', (status) => {
    render(<StatusMark status={status} />)
    expect(screen.getByText(status)).toBeInTheDocument()
  })
})

describe('shared components', () => {
  it('Button defaults to type=button', () => {
    render(<Button>Go</Button>)
    expect(screen.getByRole('button', { name: 'Go' })).toHaveAttribute('type', 'button')
  })

  it('ButtonLink renders a link', () => {
    render(
      <MemoryRouter>
        <ButtonLink to="/x" variant="primary">
          Open
        </ButtonLink>
      </MemoryRouter>,
    )
    expect(screen.getByRole('link', { name: 'Open' })).toHaveAttribute('href', '/x')
  })

  it('TextField associates label and error', () => {
    render(<TextField id="t" label="Title" error={['Required']} />)
    const input = screen.getByLabelText('Title')
    expect(input).toHaveAttribute('aria-invalid', 'true')
    expect(screen.getByRole('alert')).toHaveTextContent('Required')
  })

  it('Card, EmptyState and PageHeader render their content', () => {
    render(
      <>
        <PageHeader title="Jobs" subtitle="3 applications" />
        <Card title="Notes">body</Card>
        <EmptyState title="Nothing" body="Add one" />
      </>,
    )
    expect(screen.getByRole('heading', { name: 'Jobs' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Notes' })).toBeInTheDocument()
    expect(screen.getByText('Add one')).toBeInTheDocument()
  })
})

function Trigger({ onAction }: { onAction: () => void }) {
  const toast = useToast()
  return (
    <button
      type="button"
      onClick={() =>
        toast.show({ message: 'Could not save', tone: 'error', action: { label: 'Retry', onAction } })
      }
    >
      fire
    </button>
  )
}

describe('Toast', () => {
  afterEach(() => vi.useRealTimers())

  it('announces in a polite live region and runs the action', async () => {
    const onAction = vi.fn()
    render(
      <ToastProvider>
        <Trigger onAction={onAction} />
      </ToastProvider>,
    )
    await userEvent.click(screen.getByRole('button', { name: 'fire' }))
    expect(screen.getByRole('status')).toHaveAttribute('aria-live', 'polite')
    expect(screen.getByText('Could not save')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Retry' }))
    expect(onAction).toHaveBeenCalledTimes(1)
    expect(screen.queryByText('Could not save')).not.toBeInTheDocument()
  })

  it('dismisses manually', async () => {
    render(
      <ToastProvider>
        <Trigger onAction={() => {}} />
      </ToastProvider>,
    )
    await userEvent.click(screen.getByRole('button', { name: 'fire' }))
    await userEvent.click(screen.getByRole('button', { name: /dismiss notification/i }))
    expect(screen.queryByText('Could not save')).not.toBeInTheDocument()
  })

  it('auto-dismisses after the duration', () => {
    vi.useFakeTimers()
    render(
      <ToastProvider>
        <Trigger onAction={() => {}} />
      </ToastProvider>,
    )
    act(() => {
      screen.getByRole('button', { name: 'fire' }).click()
    })
    expect(screen.getByText('Could not save')).toBeInTheDocument()
    act(() => {
      vi.advanceTimersByTime(6100)
    })
    expect(screen.queryByText('Could not save')).not.toBeInTheDocument()
  })
})
