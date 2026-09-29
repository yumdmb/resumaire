import type { ResumeContent } from '../../lib/types'
import type { SuggestionState } from './types'

/**
 * Client-side preview of what accepted suggestions do to the base resume. The server applies the same
 * rules when a version is saved (backend/Tailoring/ResumeContentEditor.cs) and is authoritative, so keep
 * the two in step. A suggestion only ever changes the one location named by its target path.
 */
export function applyAcceptedSuggestions(
  content: ResumeContent,
  suggestions: SuggestionState[],
): ResumeContent {
  return suggestions
    .filter((state) => state.decision === 'accepted')
    .reduce(
      (current, state) =>
        applySuggestion(
          current,
          state.suggestion.targetPath,
          state.suggestion.operation,
          state.editedContent.trim(),
        ),
      content,
    )
}

const LIST_PATH = /^(Experience|Education|Projects|Activities)\[(\d+)\]\.(Bullets|Details)(?:\[(\d+)\])?$/

type ListSection = 'experience' | 'education' | 'projects' | 'activities'

const listSections: Record<string, { key: ListSection; field: 'bullets' | 'details' }> = {
  Experience: { key: 'experience', field: 'bullets' },
  Education: { key: 'education', field: 'details' },
  Projects: { key: 'projects', field: 'bullets' },
  Activities: { key: 'activities', field: 'bullets' },
}

export function applySuggestion(
  content: ResumeContent,
  targetPath: string,
  operation: string,
  text: string,
): ResumeContent {
  if (!text) return content

  if (operation === 'SetSkills' && targetPath === 'Skills') {
    // Reorder or trim only: skills that are not already on the resume are dropped.
    const existing = new Set(content.skills.map((skill) => skill.toLowerCase()))
    const seen = new Set<string>()
    const skills = text
      .split(/[\n,;]+/)
      .map((value) => value.trim())
      .filter((value) => {
        const key = value.toLowerCase()
        if (!value || !existing.has(key) || seen.has(key)) return false
        seen.add(key)
        return true
      })
    return skills.length === 0 ? content : { ...content, skills }
  }

  if (operation === 'Replace' && targetPath === 'Summary') {
    return { ...content, summary: text }
  }

  if (operation === 'Replace' && targetPath === 'PersonalInfo.Headline') {
    return content.personalInfo
      ? { ...content, personalInfo: { ...content.personalInfo, headline: text } }
      : content
  }

  const match = LIST_PATH.exec(targetPath)
  if (!match) return content

  const [, section, rawIndex, field, rawItem] = match
  const target = listSections[section]
  const expectedField = target.field === 'bullets' ? 'Bullets' : 'Details'
  if (field !== expectedField) return content

  const index = Number(rawIndex)
  const entries = content[target.key] ?? []
  const entry = entries[index]
  if (!entry) return content

  const current = (entry as unknown as Record<string, string[]>)[target.field] ?? []
  let next: string[]

  if (operation === 'AddBullet' && rawItem === undefined) {
    next = [...current, text]
  } else if (operation === 'Replace' && rawItem !== undefined) {
    const itemIndex = Number(rawItem)
    if (itemIndex < 0 || itemIndex >= current.length) return content
    next = current.map((value, i) => (i === itemIndex ? text : value))
  } else {
    return content
  }

  return {
    ...content,
    [target.key]: entries.map((value, i) => (i === index ? { ...value, [target.field]: next } : value)),
  }
}
