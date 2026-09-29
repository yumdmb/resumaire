import { Button } from '../ui/Button'
import { useState } from 'react'
import type { ResumeSkillGroup } from '../../lib/types'

interface Props {
  skills: string[]
  skillGroups: ResumeSkillGroup[]
  /** Both values change together: the flat list mirrors the groups so keyword matching keeps working. */
  onChange: (skills: string[], skillGroups: ResumeSkillGroup[]) => void
}

function splitSkills(text: string, existing: string[]): string[] {
  const seen = new Set(existing.map((s) => s.toLowerCase()))
  return text
    .split(',')
    .map((s) => s.trim())
    .filter((s) => {
      const key = s.toLowerCase()
      if (!s || seen.has(key)) return false
      seen.add(key)
      return true
    })
}

function flatten(groups: ResumeSkillGroup[]): string[] {
  return splitSkills(groups.flatMap((g) => g.items).join(','), [])
}

interface TagListProps {
  items: string[]
  onChange: (items: string[]) => void
  label: string
}

function TagList({ items, onChange, label }: TagListProps) {
  const [draft, setDraft] = useState('')

  function add() {
    const added = splitSkills(draft, items)
    if (added.length > 0) onChange([...items, ...added])
    setDraft('')
  }

  return (
    <>
      <div className="skills-input-row">
        <input
          className="form-input"
          placeholder="Add skills (comma-separated)"
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') {
              e.preventDefault()
              add()
            }
          }}
          aria-label={label}
        />
        <Button onClick={add}>
          Add
        </Button>
      </div>
      {items.length > 0 && (
        <div className="skills-list">
          {items.map((skill, i) => (
            <span key={`${skill}-${i}`} className="skill-tag">
              {skill}
              <button
                type="button"
                className="skill-tag-remove"
                onClick={() => onChange(items.filter((_, j) => j !== i))}
                aria-label={`Remove ${skill}`}
              >
                ×
              </button>
            </span>
          ))}
        </div>
      )}
    </>
  )
}

/**
 * Skills are either one flat list, or named categories such as "Frontend" and "Backend" that
 * export as "Frontend: React, Next.js". Once categories exist they are the source of truth.
 */
export function SkillsEditor({ skills, skillGroups, onChange }: Props) {
  if (skillGroups.length === 0) {
    return (
      <div className="editor-fields">
        <TagList
          items={skills}
          onChange={(next) => onChange(next, [])}
          label="New skill"
        />
        <div>
          <Button
            onClick={() =>
              onChange(skills, [{ category: skills.length > 0 ? 'Skills' : '', items: skills }])
            }
          >
            Group skills into categories
          </Button>
        </div>
      </div>
    )
  }

  function update(next: ResumeSkillGroup[]) {
    onChange(flatten(next), next)
  }

  return (
    <div className="editor-fields">
      {skillGroups.map((group, i) => (
        <div key={i} className="skill-group">
          <div className="skills-input-row">
            <input
              className="form-input"
              placeholder="Category, e.g. Frontend"
              value={group.category ?? ''}
              onChange={(e) =>
                update(skillGroups.map((g, j) => (j === i ? { ...g, category: e.target.value } : g)))
              }
              aria-label={`Category ${i + 1} name`}
            />
            <Button
              onClick={() => update(skillGroups.filter((_, j) => j !== i))}
              aria-label={`Remove category ${group.category || i + 1}`}
            >
              Remove
            </Button>
          </div>
          <TagList
            items={group.items}
            onChange={(items) => update(skillGroups.map((g, j) => (j === i ? { ...g, items } : g)))}
            label={`New skill for ${group.category || `category ${i + 1}`}`}
          />
        </div>
      ))}
      <div>
        <Button
          onClick={() => update([...skillGroups, { category: '', items: [] }])}
        >
          Add category
        </Button>
      </div>
    </div>
  )
}
