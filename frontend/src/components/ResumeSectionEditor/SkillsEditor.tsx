import { useState } from 'react'

interface Props {
  value: string[]
  onChange: (v: string[]) => void
}

export function SkillsEditor({ value, onChange }: Props) {
  const [draft, setDraft] = useState('')

  function addSkill() {
    const trimmed = draft.trim()
    if (!trimmed) return
    const newSkills = trimmed
      .split(',')
      .map((s) => s.trim())
      .filter((s) => s.length > 0 && !value.includes(s))
    if (newSkills.length > 0) onChange([...value, ...newSkills])
    setDraft('')
  }

  function removeSkill(index: number) {
    onChange(value.filter((_, i) => i !== index))
  }

  function handleKeyDown(e: React.KeyboardEvent) {
    if (e.key === 'Enter') {
      e.preventDefault()
      addSkill()
    }
  }

  return (
    <div className="editor-fields">
      <div className="skills-input-row">
        <input
          className="form-input"
          placeholder="Add skills (comma-separated)"
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          onKeyDown={handleKeyDown}
          aria-label="New skill"
        />
        <button type="button" className="btn btn-secondary" onClick={addSkill}>
          Add
        </button>
      </div>
      {value.length > 0 && (
        <div className="skills-list">
          {value.map((skill, i) => (
            <span key={`${skill}-${i}`} className="skill-tag">
              {skill}
              <button
                type="button"
                className="skill-tag-remove"
                onClick={() => removeSkill(i)}
                aria-label={`Remove ${skill}`}
              >
                ×
              </button>
            </span>
          ))}
        </div>
      )}
    </div>
  )
}
