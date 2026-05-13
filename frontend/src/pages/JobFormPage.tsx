import { Link, useNavigate, useParams } from 'react-router-dom'
import { useEffect, useMemo, useState } from 'react'
import { ApiError, jobsApi } from '../lib/api'
import {
  JOB_STATUSES,
  type FieldErrors,
  type JobFormValues,
  type JobStatus,
} from '../lib/types'

type Mode = 'create' | 'edit'

const EMPTY_VALUES: JobFormValues = {
  company: '',
  title: '',
  link: '',
  description: '',
  status: 'Saved',
  dateApplied: '',
  notes: '',
}

type LoadState =
  | { status: 'loading' }
  | { status: 'ready' }
  | { status: 'not_found' }
  | { status: 'error'; message: string }

export function JobFormPage({ mode }: { mode: Mode }) {
  const { jobId = '' } = useParams()
  const navigate = useNavigate()
  const isEdit = mode === 'edit'

  const [values, setValues] = useState<JobFormValues>(EMPTY_VALUES)
  const [errors, setErrors] = useState<FieldErrors>({})
  const [isSaving, setIsSaving] = useState(false)
  const [reloadToken, setReloadToken] = useState(0)
  const [loadState, setLoadState] = useState<LoadState>(() =>
    isEdit ? { status: 'loading' } : { status: 'ready' },
  )

  // Load existing job data when editing
  useEffect(() => {
    if (!isEdit) return
    let cancelled = false
    ;(async () => {
      try {
        const job = await jobsApi.get(jobId)
        if (cancelled) return
        if (!job) {
          setLoadState({ status: 'not_found' })
          return
        }
        setValues({
          company: job.company,
          title: job.title,
          link: job.link ?? '',
          description: job.description,
          status: job.status,
          dateApplied: job.dateApplied ?? '',
          notes: job.notes ?? '',
        })
        setLoadState({ status: 'ready' })
      } catch (error) {
        if (cancelled) return
        if (error instanceof ApiError) {
          if (error.isNotFound) {
            setLoadState({ status: 'not_found' })
            return
          }
          setLoadState({ status: 'error', message: error.message })
          return
        }
        setLoadState({
          status: 'error',
          message:
            error instanceof Error ? error.message : 'Could not load this job',
        })
      }
    })()
    return () => {
      cancelled = true
    }
  }, [isEdit, jobId, reloadToken])

  function handleRetry() {
    setLoadState({ status: 'loading' })
    setReloadToken((t) => t + 1)
  }

  const heading = isEdit ? 'Edit job' : 'Add job'
  const backHref = isEdit ? `/jobs/${jobId}` : '/'
  const backLabel = isEdit ? 'Back to job' : 'Jobs'

  const bindField = useMemo(
    () =>
      (field: keyof JobFormValues) => ({
        value: values[field],
        onChange: (
          event:
            | React.ChangeEvent<HTMLInputElement>
            | React.ChangeEvent<HTMLTextAreaElement>
            | React.ChangeEvent<HTMLSelectElement>,
        ) => {
          const nextValue =
            field === 'status'
              ? (event.target.value as JobStatus)
              : event.target.value
          setValues((prev) => ({ ...prev, [field]: nextValue }))
          if (errors[field]) {
            setErrors((prev) => {
              const next = { ...prev }
              delete next[field]
              return next
            })
          }
        },
      }),
    [errors, values],
  )

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    setIsSaving(true)
    setErrors({})
    try {
      const saved = isEdit
        ? await jobsApi.update(jobId, values)
        : await jobsApi.create(values)
      navigate(`/jobs/${saved.id}`, { replace: true })
    } catch (error) {
      if (error instanceof ApiError) {
        setErrors({
          ...error.fieldErrors,
          _general: error.fieldErrors._general ?? [error.message],
        })
      } else {
        const message =
          error instanceof Error ? error.message : 'Could not save this job'
        setErrors({ _general: [message] })
      }
    } finally {
      setIsSaving(false)
    }
  }

  if (loadState.status === 'loading') {
    return (
      <div className="page" aria-busy="true">
        <FormHeader heading={heading} backHref={backHref} backLabel={backLabel} />
        <div className="form-card">
          <div className="skeleton skeleton-line" style={{ width: '30%' }} />
          <div
            className="skeleton skeleton-line"
            style={{ width: '60%', marginTop: 12 }}
          />
          <div
            className="skeleton skeleton-line"
            style={{ width: '80%', marginTop: 12 }}
          />
        </div>
      </div>
    )
  }

  if (loadState.status === 'not_found') {
    return (
      <div className="page">
        <FormHeader heading={heading} backHref="/" backLabel="Jobs" />
        <div className="empty-state">
          <p className="empty-state-title">Job not found</p>
          <p className="empty-state-body">
            It may have been deleted, or the link is incorrect.
          </p>
          <Link to="/" className="btn btn-secondary">
            Back to jobs
          </Link>
        </div>
      </div>
    )
  }

  if (loadState.status === 'error') {
    return (
      <div className="page">
        <FormHeader heading={heading} backHref={backHref} backLabel={backLabel} />
        <div className="empty-state">
          <p className="empty-state-title">Could not load this job</p>
          <p className="empty-state-body">{loadState.message}</p>
          <button type="button" className="btn btn-secondary" onClick={handleRetry}>
            Retry
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="page">
      <FormHeader heading={heading} backHref={backHref} backLabel={backLabel} />

      {errors._general?.length ? (
        <div className="form-alert" role="alert">
          {errors._general.join(' ')}
        </div>
      ) : null}

      <form className="form-card" onSubmit={handleSubmit} noValidate>
        <div className="form-row-two">
          <Field
            label="Company"
            field="company"
            required
            bind={bindField('company')}
            error={errors.company}
          />
          <Field
            label="Title"
            field="title"
            required
            bind={bindField('title')}
            error={errors.title}
          />
        </div>

        <div className="form-row-two">
          <div className="form-field">
            <label className="form-label" htmlFor="status">
              Status
            </label>
            <select
              id="status"
              className="form-input"
              {...bindField('status')}
            >
              {JOB_STATUSES.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </select>
            <FieldError messages={errors.status} />
          </div>

          <div className="form-field">
            <label className="form-label" htmlFor="dateApplied">
              Date applied
            </label>
            <input
              id="dateApplied"
              type="date"
              className="form-input"
              {...bindField('dateApplied')}
            />
            <FieldError messages={errors.dateApplied} />
          </div>
        </div>

        <Field
          label="Job link"
          field="link"
          type="url"
          placeholder="https://…"
          bind={bindField('link')}
          error={errors.link}
        />

        <div className="form-field">
          <label className="form-label" htmlFor="description">
            Job description
            <span className="form-required"> *</span>
          </label>
          <textarea
            id="description"
            className="form-input form-textarea"
            rows={8}
            required
            {...bindField('description')}
          />
          <FieldError messages={errors.description} />
        </div>

        <div className="form-field">
          <label className="form-label" htmlFor="notes">
            Notes
          </label>
          <textarea
            id="notes"
            className="form-input form-textarea"
            rows={3}
            placeholder="Recruiter name, interview loop, follow-ups"
            {...bindField('notes')}
          />
          <FieldError messages={errors.notes} />
        </div>

        <div className="form-actions">
          <Link to={backHref} className="btn btn-secondary">
            Cancel
          </Link>
          <button type="submit" className="btn btn-primary" disabled={isSaving}>
            {isSaving ? 'Saving' : isEdit ? 'Save changes' : 'Add job'}
          </button>
        </div>
      </form>
    </div>
  )
}

function FormHeader({
  heading,
  backHref,
  backLabel,
}: {
  heading: string
  backHref: string
  backLabel: string
}) {
  return (
    <div className="page-header">
      <div>
        <Link to={backHref} className="back-link">
          ← {backLabel}
        </Link>
        <h1 className="page-title" style={{ marginTop: 4 }}>
          {heading}
        </h1>
      </div>
    </div>
  )
}

type FieldBinding = {
  value: string
  onChange: (event: React.ChangeEvent<HTMLInputElement>) => void
}

function Field({
  label,
  field,
  required,
  type = 'text',
  placeholder,
  bind,
  error,
}: {
  label: string
  field: keyof JobFormValues
  required?: boolean
  type?: string
  placeholder?: string
  bind: FieldBinding
  error?: string[]
}) {
  return (
    <div className="form-field">
      <label className="form-label" htmlFor={field}>
        {label}
        {required ? <span className="form-required"> *</span> : null}
      </label>
      <input
        id={field}
        type={type}
        className="form-input"
        placeholder={placeholder}
        required={required}
        {...bind}
      />
      <FieldError messages={error} />
    </div>
  )
}

function FieldError({ messages }: { messages?: string[] }) {
  if (!messages || messages.length === 0) return null
  return (
    <p className="form-error" role="alert">
      {messages.join(' ')}
    </p>
  )
}
