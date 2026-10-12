import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { aiSettingsApi } from '../lib/api'
import type { AiCatalog, AiSettings } from '../lib/ai-types'
import { AiSettingsPage } from './AiSettingsPage'

const emptySettings: AiSettings = { hasKey: false, providerId: null, modelId: null, keyLastFour: null,
  credentialVersion: null, lastValidatedAt: null, trainingAcknowledged: false,
  providers: [{ id: 'openai', name: 'OpenAI', privacyUrl: 'https://example.test/privacy' },
    { id: 'opencode', name: 'OpenCode Zen', privacyUrl: 'https://example.test/privacy' }] }
const connected: AiSettings = { ...emptySettings, hasKey: true, providerId: 'openai', modelId: 'gpt-4o-mini',
  keyLastFour: '1234', credentialVersion: 'version-1', lastValidatedAt: '2026-10-12T00:00:00Z' }
const catalog: AiCatalog = { isFallback: false, notice: null, models: [
  { id: 'gpt-4o-mini', name: 'GPT-4o mini', supported: true, recommended: true, usesTrainingData: false, note: null },
  { id: 'gpt-4.1-mini', name: 'GPT-4.1 mini', supported: true, recommended: false, usesTrainingData: false, note: null },
  { id: 'whisper-1', name: 'Whisper', supported: false, recommended: false, usesTrainingData: false, note: null },
] }

describe('AI settings', () => {
  beforeEach(() => {
    vi.spyOn(aiSettingsApi, 'get').mockResolvedValue(emptySettings)
    vi.spyOn(aiSettingsApi, 'discover').mockResolvedValue(catalog)
    vi.spyOn(aiSettingsApi, 'test').mockResolvedValue({ validationToken: 'exact-model-receipt', testedAt: '2026-10-12T00:00:00Z', message: 'Connection verified.' })
    vi.spyOn(aiSettingsApi, 'save').mockResolvedValue(connected)
    vi.spyOn(aiSettingsApi, 'remove').mockResolvedValue(undefined)
  })
  afterEach(() => { vi.restoreAllMocks(); window.localStorage.clear() })

  it('discovers models, requires an explicit test, saves and clears the key', async () => {
    const user = userEvent.setup()
    renderPage()
    const key = await screen.findByLabelText('API key')
    await user.type(key, 'personal-key-1234')
    await user.click(screen.getByRole('button', { name: 'Connect and load models' }))
    const picker = await screen.findByLabelText('Model')
    expect(picker).toHaveValue('gpt-4o-mini')
    expect(screen.getByRole('option', { name: /whisper/i })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Save connection' })).toBeDisabled()
    await user.click(screen.getByRole('button', { name: 'Test selected model' }))
    await screen.findByText('Connection verified.')
    await user.click(screen.getByRole('button', { name: 'Save connection' }))
    await screen.findByText(/AI connection saved/)
    expect(aiSettingsApi.save).toHaveBeenCalledWith(expect.objectContaining({ apiKey: 'personal-key-1234',
      providerId: 'openai', modelId: 'gpt-4o-mini', validationToken: 'exact-model-receipt' }), expect.any(AbortSignal))
    expect(screen.getByLabelText('Replace API key (optional)')).toHaveValue('')
    expect(window.localStorage.length).toBe(0)
    expect(screen.getByText('1234')).toBeInTheDocument()
  })

  it('invalidates a successful test when the selected model changes', async () => {
    const user = userEvent.setup()
    vi.mocked(aiSettingsApi.get).mockResolvedValue(connected)
    renderPage()
    await screen.findByLabelText('Model')
    await waitFor(() => expect(screen.getByRole('button', { name: 'Test selected model' })).toBeEnabled())
    await user.click(screen.getByRole('button', { name: 'Test selected model' }))
    await screen.findByText('Connection verified.')
    expect(screen.getByRole('button', { name: 'Save connection' })).toBeEnabled()
    await user.selectOptions(screen.getByLabelText('Model'), 'gpt-4.1-mini')
    expect(screen.getByRole('button', { name: 'Save connection' })).toBeDisabled()
    await user.click(screen.getByRole('button', { name: 'Refresh models' }))
    await waitFor(() => expect(aiSettingsApi.discover).toHaveBeenLastCalledWith(expect.objectContaining({ refresh: true, apiKey: undefined }), expect.any(AbortSignal)))
  })

  it('requires training acknowledgement for contributor models', async () => {
    const user = userEvent.setup()
    vi.mocked(aiSettingsApi.discover).mockResolvedValue({ isFallback: true, notice: 'Catalog unavailable.', models: [
      { id: 'contributor-free', name: 'Contributor Free', supported: true, recommended: false, usesTrainingData: true, note: 'Uses submitted text for training.' },
    ] })
    renderPage()
    await user.type(await screen.findByLabelText('API key'), 'personal-key-1234')
    await user.click(screen.getByRole('button', { name: 'Connect and load models' }))
    await user.selectOptions(await screen.findByLabelText('Model'), 'contributor-free')
    expect(screen.getByRole('button', { name: 'Test selected model' })).toBeDisabled()
    expect(screen.getByText('Catalog unavailable.')).toBeInTheDocument()
    await user.click(screen.getByRole('checkbox'))
    expect(screen.getByRole('button', { name: 'Test selected model' })).toBeEnabled()
  })

  it('keeps draft edits when saving fails and removes a saved key with confirmation', async () => {
    const user = userEvent.setup()
    vi.mocked(aiSettingsApi.get).mockResolvedValue(connected)
    vi.mocked(aiSettingsApi.save).mockRejectedValue(new Error('Settings changed in another window.'))
    renderPage()
    await screen.findByLabelText('Model')
    await waitFor(() => expect(screen.getByRole('button', { name: 'Test selected model' })).toBeEnabled())
    await user.click(screen.getByRole('button', { name: 'Test selected model' }))
    await screen.findByText('Connection verified.')
    await user.click(screen.getByRole('button', { name: 'Save connection' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('Settings changed')
    expect(screen.getByLabelText('Model')).toHaveValue('gpt-4o-mini')
    await user.click(screen.getByRole('button', { name: 'Remove key' }))
    expect(aiSettingsApi.remove).not.toHaveBeenCalled()
    await user.click(screen.getByRole('button', { name: 'Confirm removal' }))
    await screen.findByText(/Saved key removed/)
    expect(screen.getByLabelText('API key')).toHaveValue('')
    expect(screen.queryByText('1234')).not.toBeInTheDocument()
  })
})

function renderPage() { render(<MemoryRouter><AiSettingsPage /></MemoryRouter>) }
