interface Props {
  value: string | null
  onChange: (v: string | null) => void
  idPrefix: string
}

export function SummaryEditor({ value, onChange, idPrefix }: Props) {
  const textId = `${idPrefix}summary-text`

  return (
    <div className="editor-fields">
      <div className="form-field">
        <label className="form-label" htmlFor={textId}>
          Professional summary
        </label>
        <textarea
          id={textId}
          className="form-input form-textarea"
          rows={4}
          placeholder="A brief overview of your professional background and goals"
          value={value ?? ''}
          onChange={(e) => onChange(e.target.value || null)}
        />
        <p className="editor-hint">Keep it concise: 2–4 sentences.</p>
      </div>
    </div>
  )
}
