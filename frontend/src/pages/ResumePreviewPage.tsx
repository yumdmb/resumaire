import { Page } from '../components/ui/Page'
import { useEffect, useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { Button } from '../components/ui/Button'
import { PageHeader } from '../components/ui/PageHeader'
import { exportApi } from '../lib/api'

type PreviewType = 'base' | 'tailored'

export function ResumePreviewPage() {
  const { tailoredResumeId } = useParams<{ tailoredResumeId?: string }>()
  const [searchParams] = useSearchParams()
  const jobId = searchParams.get('jobId')

  const previewType: PreviewType = tailoredResumeId ? 'tailored' : 'base'

  // The result is tagged with the request it belongs to, so a new request reads as loading
  // without resetting state inside the effect.
  const requestKey = `${previewType}:${tailoredResumeId ?? ''}`
  const [result, setResult] = useState<{ key: string; url: string | null; error: string | null } | null>(null)
  const [exportError, setExportError] = useState<string | null>(null)
  const [exporting, setExporting] = useState(false)

  const current = result?.key === requestKey ? result : null
  const loading = current === null
  const pdfUrl = current?.url ?? null
  const error = exportError ?? current?.error ?? null

  useEffect(() => {
    let cancelled = false
    let objectUrl: string | null = null

    const fetchPreview = async () => {
      try {
        const blob =
          previewType === 'tailored' && tailoredResumeId
            ? await exportApi.previewTailored(tailoredResumeId)
            : await exportApi.previewBase()

        if (!cancelled) {
          objectUrl = URL.createObjectURL(blob)
          setResult({ key: requestKey, url: objectUrl, error: null })
        }
      } catch (err) {
        if (!cancelled) {
          setResult({
            key: requestKey,
            url: null,
            error: err instanceof Error ? err.message : 'Failed to load preview',
          })
        }
      }
    }

    fetchPreview()
    return () => {
      cancelled = true
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
  }, [previewType, tailoredResumeId, requestKey])

  const handleExportPdf = async () => {
    setExporting(true)
    setExportError(null)
    try {
      const blob =
        previewType === 'tailored' && tailoredResumeId
          ? await exportApi.exportTailoredPdf(tailoredResumeId)
          : await exportApi.exportBasePdf()

      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = previewType === 'tailored' ? 'resume-tailored.pdf' : 'resume.pdf'
      document.body.appendChild(a)
      a.click()
      document.body.removeChild(a)
      URL.revokeObjectURL(url)
    } catch (err) {
      setExportError(err instanceof Error ? err.message : 'Export failed')
    } finally {
      setExporting(false)
    }
  }

  const backPath = previewType === 'tailored' && jobId
    ? `/jobs/${jobId}`
    : '/resume'

  return (
    <Page width="standard">
      <PageHeader
        eyebrow={
          <Link to={backPath} className="back-link">
            <svg width="12" height="12" viewBox="0 0 16 16" fill="none" aria-hidden="true">
              <path d="M10 3L5 8l5 5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
            Back
          </Link>
        }
        title={previewType === 'tailored' ? 'Tailored Resume Preview' : 'Resume Preview'}
        actions={
          <Button variant="primary" onClick={handleExportPdf} disabled={exporting || loading || !!error}>
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" aria-hidden="true">
              <path d="M3 10v2.5A1.5 1.5 0 0 0 4.5 14h7a1.5 1.5 0 0 0 1.5-1.5V10M8 2v8M5 7l3 3 3-3" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
            {exporting ? 'Exporting…' : 'Export PDF'}
          </Button>
        }
      />

      {loading && (
        <div className="preview-desk preview-loading" aria-busy="true">
          <div className="preview-sheet skeleton" />
        </div>
      )}

      {error && (
        <div className="form-alert" role="alert">{error}</div>
      )}

      {pdfUrl && !loading && (
        <div className="preview-desk">
          <div className="preview-sheet">
            <iframe className="preview-frame" src={pdfUrl} title="Resume preview" />
          </div>
        </div>
      )}
    </Page>
  )
}
