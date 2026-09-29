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

function dedupe(values: string[]): string[] {
  const seen = new Set<string>()
  return values.filter((value) => {
    const key = value.toLowerCase()
    if (seen.has(key)) return false
    seen.add(key)
    return true
  })
}

/** Reorder or trim only: skills that are not already in `existing` are dropped. */
function reorderSubset(existing: string[], text: string): string[] {
  const allowed = new Set(existing.map((skill) => skill.toLowerCase()))
  return dedupe(
    text
      .split(/[\n,;]+/)
      .map((value) => value.trim())
      .filter((value) => value && allowed.has(value.toLowerCase())),
  )
}

export function applySuggestion(
  content: ResumeContent,
  targetPath: string,
  operation: string,
  text: string,
): ResumeContent {
  if (!text) return content

  if (operation === 'SetSkills') {
    const groups = content.skillGroups ?? []
    if (targetPath === 'Skills' && groups.length === 0) {
      const skills = reorderSubset(content.skills, text)
      return skills.length === 0 ? content : { ...content, skills }
    }

    const groupMatch = /^SkillGroups\[(\d+)\]$/.exec(targetPath)
    const index = groupMatch ? Number(groupMatch[1]) : -1
    if (!groups[index]) return content

    const items = reorderSubset(groups[index].items, text)
    if (items.length === 0) return content

    const skillGroups = groups.map((group, i) => (i === index ? { ...group, items } : group))
    // The flat list mirrors the categories, as the server does when it applies the same change.
    const skills = dedupe(skillGroups.flatMap((group) => group.items))
    return { ...content, skillGroups, skills }
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
