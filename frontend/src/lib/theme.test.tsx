import { act, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ThemeProvider } from './theme'
import { THEME_STORAGE_KEY, resolveTheme } from './theme-context'
import { ThemeToggle } from '../components/ui/ThemeToggle'

function mockSystemDark(dark: boolean) {
  window.matchMedia = vi.fn().mockImplementation((query: string) => ({
    matches: dark,
    media: query,
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
  })) as unknown as typeof window.matchMedia
}

describe('theme', () => {
  beforeEach(() => {
    window.localStorage.clear()
    document.documentElement.removeAttribute('data-theme')
  })
  afterEach(() => vi.restoreAllMocks())

  it('resolves system preference', () => {
    expect(resolveTheme('system', true)).toBe('dark')
    expect(resolveTheme('system', false)).toBe('light')
    expect(resolveTheme('light', true)).toBe('light')
  })

  it('follows the system when nothing is saved', () => {
    mockSystemDark(true)
    render(<ThemeProvider><ThemeToggle /></ThemeProvider>)
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark')
  })

  it('persists a manual override', async () => {
    mockSystemDark(true)
    render(<ThemeProvider><ThemeToggle /></ThemeProvider>)
    await userEvent.click(screen.getByRole('button', { name: /switch to light theme/i }))
    expect(document.documentElement.getAttribute('data-theme')).toBe('light')
    expect(window.localStorage.getItem(THEME_STORAGE_KEY)).toBe('light')
  })

  it('uses a saved choice over the system', () => {
    mockSystemDark(true)
    window.localStorage.setItem(THEME_STORAGE_KEY, 'light')
    render(<ThemeProvider><ThemeToggle /></ThemeProvider>)
    expect(document.documentElement.getAttribute('data-theme')).toBe('light')
  })

  it('still switches when storage throws', async () => {
    mockSystemDark(false)
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('blocked')
    })
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('blocked')
    })
    render(<ThemeProvider><ThemeToggle /></ThemeProvider>)
    expect(document.documentElement.getAttribute('data-theme')).toBe('light')
    await act(async () => {
      await userEvent.click(screen.getByRole('button', { name: /switch to dark theme/i }))
    })
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark')
  })
})
