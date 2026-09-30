import type { InputHTMLAttributes, ReactNode, TextareaHTMLAttributes } from 'react'

interface FieldShellProps {
  id: string
  label: string
  required?: boolean
  hint?: string
  error?: string[]
  children: ReactNode
}

export function FieldShell({ id, label, required, hint, error, children }: FieldShellProps) {
  return (
    <div className="form-field">
      <label className="form-label" htmlFor={id}>
        {label}
        {required ? (
          <span className="form-required" aria-hidden="true">
            {' '}
            *
          </span>
        ) : null}
      </label>
      {children}
      {hint && !error?.length ? <p className="form-hint">{hint}</p> : null}
      {error?.length ? (
        <p className="form-error" id={`${id}-error`} role="alert">
          {error.join(' ')}
        </p>
      ) : null}
    </div>
  )
}

interface TextFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  id: string
  label: string
  hint?: string
  error?: string[]
}

export function TextField({ id, label, hint, error, required, className, ...rest }: TextFieldProps) {
  return (
    <FieldShell id={id} label={label} hint={hint} error={error} required={required}>
      <input
        id={id}
        className={`form-input${error?.length ? ' form-input--invalid' : ''}${className ? ` ${className}` : ''}`}
        aria-invalid={error?.length ? true : undefined}
        aria-describedby={error?.length ? `${id}-error` : undefined}
        required={required}
        {...rest}
      />
    </FieldShell>
  )
}

interface TextAreaFieldProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  id: string
  label: string
  hint?: string
  error?: string[]
}

export function TextAreaField({ id, label, hint, error, required, className, ...rest }: TextAreaFieldProps) {
  return (
    <FieldShell id={id} label={label} hint={hint} error={error} required={required}>
      <textarea
        id={id}
        className={`form-input form-textarea${error?.length ? ' form-input--invalid' : ''}${className ? ` ${className}` : ''}`}
        aria-invalid={error?.length ? true : undefined}
        aria-describedby={error?.length ? `${id}-error` : undefined}
        required={required}
        {...rest}
      />
    </FieldShell>
  )
}
