import { useEffect, useRef, useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { exportApi } from '../lib/api'

type PreviewType = 'base' | 'tailored'

export function ResumePreviewPage() {
  const { tailoredResumeId } = useParams<{ tailoredResumeId?: string }>()
  const [searchParams] = useSearchParams()
  const jobId = searchParams.get('jobId')

  const previewType: PreviewType = tailoredResumeId ? 'tailored' : 'base'

  const [html, setHtml] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [exporting, setExporting] = useState(false)
  const iframeRef = useRef<HTMLIFrameElement>(null)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError(null)

    const fetchPreview = async () => {
      try {
        const result =
          previewType === 'tailored' && tailoredResumeId
            ? await exportApi.previewTailored(tailoredResumeId)
            : await exportApi.previewBase()

        if (!cancelled) {
          setHtml(result)
          setLoading(false)
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Failed to load preview')
          setLoading(false)
        }
      }
    }

    fetchPreview()
    return () => { cancelled = true }
  }, [previewType, tailoredResumeId])

  const handleExportPdf = async () => {
    setExporting(true)
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
      setError(err instanceof Error ? err.message : 'Export failed')
    } finally {
      setExporting(false)
    }
  }

  const backPath = previewType === 'tailored' && jobId
    ? `/jobs/${jobId}`
    : '/resume'

  return (
    <div className="page preview-page">
      <div className="page-header">
        <div>
          <Link to={backPath} className="back-link">
            <svg width="12" height="12" viewBox="0 0 16 16" fill="none" aria-hidden="true">
              <path d="M10 3L5 8l5 5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
            Back
          </Link>
          <h1 className="page-title">
            {previewType === 'tailored' ? 'Tailored Resume Preview' : 'Resume Preview'}
          </h1>
        </div>
        <div className="page-actions">
          <button
            type="button"
            className="btn btn-primary"
            onClick={handleExportPdf}
            disabled={exporting || loading || !!error}
          >
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" aria-hidden="true">
              <path d="M3 10v2.5A1.5 1.5 0 0 0 4.5 14h7a1.5 1.5 0 0 0 1.5-1.5V10M8 2v8M5 7l3 3 3-3" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
            {exporting ? 'Exporting…' : 'Export PDF'}
          </button>
        </div>
      </div>

      {loading && (
        <div className="preview-loading">
          <div className="skeleton skeleton-card" style={{ height: 600 }} />
        </div>
      )}

      {error && (
        <div className="form-alert">{error}</div>
      )}

      {html && !loading && (
        <div className="preview-frame-wrapper">
          <iframe
            ref={iframeRef}
            className="preview-frame"
            srcDoc={html}
            title="Resume preview"
            sandbox="allow-same-origin"
          />
        </div>
      )}
    </div>
  )
}
