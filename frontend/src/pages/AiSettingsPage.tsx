import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { Page } from '../components/ui/Page'
import { PageHeader } from '../components/ui/PageHeader'
import { Button } from '../components/ui/Button'
import { aiSettingsApi } from '../lib/api'
import type { AiCatalog, AiConnectionInput, AiSettings } from '../lib/ai-types'

export function AiSettingsPage() {
  const [saved, setSaved] = useState<AiSettings | null>(null)
  const [provider, setProvider] = useState('openai')
  const [key, setKey] = useState('')
  const [catalog, setCatalog] = useState<AiCatalog | null>(null)
  const [model, setModel] = useState('')
  const [training, setTraining] = useState(false)
  const [validationToken, setValidationToken] = useState('')
  const [busy, setBusy] = useState<string | null>('Loading AI settings…')
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [confirmRemove, setConfirmRemove] = useState(false)
  const activeRequest = useRef<AbortController | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    activeRequest.current = controller
    async function load() {
      try {
        const settings = await aiSettingsApi.get(controller.signal)
        if (controller.signal.aborted) return
        setSaved(settings)
        setProvider(settings.providerId ?? 'openai')
        setModel(settings.modelId ?? '')
        setTraining(settings.trainingAcknowledged)
        if (settings.hasKey && settings.providerId) {
          const available = await aiSettingsApi.discover({ providerId: settings.providerId }, controller.signal)
          if (!controller.signal.aborted) setCatalog(available)
        }
      } catch (err) {
        if (!controller.signal.aborted) setError(err instanceof Error ? err.message : 'Could not load AI settings')
      } finally {
        if (!controller.signal.aborted) setBusy(null)
      }
    }
    void load()
    return () => { controller.abort(); activeRequest.current?.abort() }
  }, [])

  const selectedModel = catalog?.models.find((item) => item.id === model)
  const selectedProvider = saved?.providers.find((item) => item.id === provider)
  const canReuseKey = saved?.hasKey && saved.providerId === provider
  const hasCredential = key.trim().length > 0 || canReuseKey

  function invalidate() { setValidationToken(''); setMessage(null); setError(null) }
  function input(): AiConnectionInput {
    return { providerId: provider, modelId: model, apiKey: key.trim() || undefined,
      trainingAcknowledged: training, expectedVersion: saved?.credentialVersion ?? null }
  }

  async function perform(label: string, action: (signal: AbortSignal) => Promise<void>) {
    if (busy) return
    const controller = new AbortController()
    activeRequest.current = controller
    setBusy(label); setError(null); setMessage(null)
    try { await action(controller.signal) }
    catch (err) { if (!controller.signal.aborted) setError(err instanceof Error ? err.message : 'Request failed') }
    finally { if (!controller.signal.aborted) setBusy(null) }
  }

  function discover(refresh = false) {
    void perform('Loading available models…', async (signal) => {
      const available = await aiSettingsApi.discover({ ...input(), refresh }, signal)
      if (signal.aborted) return
      setCatalog(available)
      setValidationToken('')
      setModel((current) => available.models.some((item) => item.id === current && item.supported)
        ? current : available.models.find((item) => item.supported && item.recommended)?.id
          ?? available.models.find((item) => item.supported && !item.usesTrainingData)?.id ?? '')
    })
  }

  function test() {
    setValidationToken('')
    void perform('Testing selected model…', async (signal) => {
      const result = await aiSettingsApi.test(input(), signal)
      if (signal.aborted) return
      setValidationToken(result.validationToken); setMessage(result.message)
    })
  }

  function save() {
    void perform('Saving connection…', async (signal) => {
      const settings = await aiSettingsApi.save({ ...input(), validationToken }, signal)
      if (signal.aborted) return
      setSaved(settings); setKey(''); setValidationToken(''); setMessage('AI connection saved. You can now tailor your resume.')
    })
  }

  function remove() {
    void perform('Removing saved key…', async (signal) => {
      await aiSettingsApi.remove(signal)
      if (signal.aborted) return
      setSaved((current) => current ? { ...current, hasKey: false, providerId: null, modelId: null,
        keyLastFour: null, credentialVersion: null, lastValidatedAt: null, trainingAcknowledged: false } : null)
      setKey(''); setModel(''); setCatalog(null); setValidationToken(''); setTraining(false)
      setConfirmRemove(false); setMessage('Saved key removed. Manual tailoring is still available.')
    })
  }

  return (
    <Page width="narrow">
      <PageHeader title="AI settings" subtitle="Connect your own provider and choose a model for resume tailoring." />
      <section className="ai-settings-card" aria-label="AI provider connection" aria-busy={!!busy}>
        <div className="ai-settings-intro">
          <h2>Your AI connection</h2>
          <p>Your provider bills your account directly. Resumaire encrypts your saved key and uses it to make requests on your behalf.</p>
          {saved?.hasKey && <p className="ai-connection-status">Connected to {saved.providers.find((item) => item.id === saved.providerId)?.name}
            {' · '}{saved.modelId}{' · '}Key ending in <strong>{saved.keyLastFour}</strong></p>}
        </div>
        {error && <div className="form-error" role="alert">{error}</div>}
        {message && <div className="ai-settings-success" role="status">{message}</div>}
        {busy && <p role="status">{busy}</p>}
        {!saved && !busy && <Button onClick={() => window.location.reload()}>Reload settings</Button>}
        {saved && <>
          <div className="form-field">
            <label className="form-label" htmlFor="ai-provider">Provider</label>
            <select id="ai-provider" className="form-input" disabled={!!busy} value={provider} onChange={(event) => {
              setProvider(event.target.value); setKey(''); setCatalog(null); setModel(''); setTraining(false); invalidate()
            }}>
              {saved.providers.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label className="form-label" htmlFor="ai-key">{canReuseKey ? 'Replace API key (optional)' : 'API key'}</label>
            <input id="ai-key" type="password" className="form-input" autoComplete="off" spellCheck={false} maxLength={4096}
              disabled={!!busy} value={key} aria-describedby="ai-key-help" placeholder={canReuseKey ? 'Leave blank to use your saved key' : 'Paste your provider API key'}
              onChange={(event) => { setKey(event.target.value); setCatalog(null); setModel(''); invalidate() }} />
            <p id="ai-key-help" className="form-hint">Your saved key is never displayed. Replacing it requires another connection test.</p>
          </div>
          <div className="ai-settings-actions">
            <Button disabled={!!busy || !hasCredential} onClick={() => discover()}>{catalog ? 'Load models' : 'Connect and load models'}</Button>
            {catalog && <Button variant="ghost" disabled={!!busy} onClick={() => discover(true)}>Refresh models</Button>}
          </div>
          {catalog && <>
            {catalog.notice && <p className="ai-settings-notice">{catalog.notice}</p>}
            <div className="form-field">
              <label className="form-label" htmlFor="ai-model">Model</label>
              <select id="ai-model" className="form-input" disabled={!!busy} value={model} onChange={(event) => {
                setModel(event.target.value); setTraining(false); invalidate()
              }}>
                <option value="">Choose a supported model…</option>
                {catalog.models.map((item) => <option key={item.id} value={item.id} disabled={!item.supported}>
                  {item.name}{item.recommended ? ' — Recommended' : ''}{!item.supported ? ' — Not supported' : ''}
                </option>)}
              </select>
              <p className="form-hint">Recommended models favor cost and compatibility. Listed models still need a connection test.</p>
            </div>
            {selectedModel?.note && <p className="ai-settings-notice">{selectedModel.note}</p>}
            {selectedModel?.usesTrainingData && <label className="ai-training-acknowledgement">
              <input type="checkbox" checked={training} disabled={!!busy} onChange={(event) => { setTraining(event.target.checked); invalidate() }} />
              I understand this model may use my resume text and responses for training.
            </label>}
            <p className="ai-settings-disclosure">Testing sends a small synthetic prompt and may incur a provider charge. Tailoring sends your professional resume content and job description. Cancelling a request may still incur a charge.</p>
            <div className="ai-settings-actions">
              <Button disabled={!!busy || !selectedModel?.supported || (selectedModel.usesTrainingData && !training)} onClick={test}>Test selected model</Button>
              <Button variant="primary" disabled={!!busy || !validationToken} onClick={save}>Save connection</Button>
            </div>
          </>}
          {selectedProvider && <p className="ai-settings-privacy"><a href={selectedProvider.privacyUrl} target="_blank" rel="noreferrer">Read {selectedProvider.name}'s data policy</a></p>}
          {saved.hasKey && <div className="ai-settings-remove">
            {confirmRemove ? <>
              <p>Remove the saved key? AI tailoring will require a new connection.</p>
              <div className="ai-settings-actions"><Button variant="danger" disabled={!!busy} onClick={remove}>Confirm removal</Button>
                <Button disabled={!!busy} onClick={() => setConfirmRemove(false)}>Keep key</Button></div>
            </> : <Button variant="danger" disabled={!!busy} onClick={() => setConfirmRemove(true)}>Remove key</Button>}
          </div>}
        </>}
      </section>
      <p className="ai-settings-footer"><Link to="/tailor">Go to tailoring</Link> · Manual editing works without an AI connection.</p>
    </Page>
  )
}
