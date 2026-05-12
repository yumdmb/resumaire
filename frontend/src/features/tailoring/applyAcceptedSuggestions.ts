import type { ResumeContent } from '../../lib/types'
import type { SuggestionState } from './types'

const sectionToContentKey = {
  PersonalInfo: 'personalInfo',
  Summary: 'summary',
  Skills: 'skills',
  Experience: 'experience',
  Education: 'education',
  Certifications: 'certifications',
  Links: 'links',
} as const

type TailoringSection = keyof typeof sectionToContentKey

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
          state.suggestion.targetSection,
          state.editedContent.trim(),
          state.suggestion.sourceEvidence.map((evidence) => evidence.path),
        ),
      cloneContent(content),
    )
}

function applySuggestion(
  content: ResumeContent,
  targetSection: string,
  suggestedContent: string,
  sourceEvidencePaths: string[],
): ResumeContent {
  if (!suggestedContent) {
    return content
  }

  if (targetSection === 'Summary') {
    return { ...content, summary: suggestedContent }
  }

  if (targetSection === 'Skills') {
    return applySkillsSuggestion(content, suggestedContent, sourceEvidencePaths)
  }

  const matchingPath = sourceEvidencePaths.find((path) =>
    path.startsWith(`${targetSection}[`) || path.startsWith(`${targetSection}.`),
  )

  if (matchingPath) {
    return applyPathUpdate(content, matchingPath, suggestedContent)
  }

  return applySectionFallback(content, targetSection, suggestedContent)
}

function applySkillsSuggestion(
  content: ResumeContent,
  suggestedContent: string,
  sourceEvidencePaths: string[],
): ResumeContent {
  const matchingPath = sourceEvidencePaths.find((path) => path.startsWith('Skills['))
  if (matchingPath) {
    return applyPathUpdate(content, matchingPath, suggestedContent)
  }

  const suggestedSkills = suggestedContent
    .split(/[\n,;]+/)
    .map((value) => value.trim())
    .filter(Boolean)

  return {
    ...content,
    skills: [...content.skills, ...suggestedSkills].filter(
      (skill, index, all) =>
        all.findIndex((candidate) => candidate.toLowerCase() === skill.toLowerCase()) === index,
    ),
  }
}

function applySectionFallback(
  content: ResumeContent,
  targetSection: string,
  suggestedContent: string,
): ResumeContent {
  switch (targetSection) {
    case 'PersonalInfo':
      return {
        ...content,
        personalInfo: {
          fullName: content.personalInfo?.fullName ?? null,
          email: content.personalInfo?.email ?? null,
          phone: content.personalInfo?.phone ?? null,
          location: content.personalInfo?.location ?? null,
          website: content.personalInfo?.website ?? null,
          headline: suggestedContent,
        },
      }
    case 'Experience':
      if (content.experience.length === 0) return content
      return {
        ...content,
        experience: content.experience.map((entry, index) =>
          index === 0 ? { ...entry, bullets: [...entry.bullets, suggestedContent] } : entry,
        ),
      }
    case 'Education':
      if (content.education.length === 0) return content
      return {
        ...content,
        education: content.education.map((entry, index) =>
          index === 0 ? { ...entry, details: [...entry.details, suggestedContent] } : entry,
        ),
      }
    default:
      return content
  }
}

function applyPathUpdate(
  content: ResumeContent,
  path: string,
  suggestedContent: string,
): ResumeContent {
  if (path === 'Summary') {
    return { ...content, summary: suggestedContent }
  }

  if (path === 'PersonalInfo.Headline') {
    return {
      ...content,
      personalInfo: {
        fullName: content.personalInfo?.fullName ?? null,
        email: content.personalInfo?.email ?? null,
        phone: content.personalInfo?.phone ?? null,
        location: content.personalInfo?.location ?? null,
        website: content.personalInfo?.website ?? null,
        headline: suggestedContent,
      },
    }
  }

  const arrayPath = parseArrayPath(path)
  if (!arrayPath) {
    return content
  }

  if (arrayPath.section === 'Skills' && arrayPath.field === null) {
    return replaceAt(content, 'skills', arrayPath.index, suggestedContent)
  }

  if (arrayPath.section === 'Experience') {
    return applyExperiencePath(content, arrayPath, suggestedContent)
  }

  if (arrayPath.section === 'Education') {
    return applyEducationPath(content, arrayPath, suggestedContent)
  }

  if (arrayPath.section === 'Certifications') {
    return applyObjectFieldPath(content, 'certifications', arrayPath, suggestedContent)
  }

  if (arrayPath.section === 'Links') {
    return applyObjectFieldPath(content, 'links', arrayPath, suggestedContent)
  }

  return content
}

function applyExperiencePath(
  content: ResumeContent,
  path: ParsedArrayPath,
  suggestedContent: string,
): ResumeContent {
  const nestedIndex = path.nestedIndex
  if (path.field === 'Bullets' && nestedIndex !== null) {
    return {
      ...content,
      experience: content.experience.map((entry, index) =>
        index === path.index
          ? { ...entry, bullets: replaceArrayValue(entry.bullets, nestedIndex, suggestedContent) }
          : entry,
      ),
    }
  }

  return applyObjectFieldPath(content, 'experience', path, suggestedContent)
}

function applyEducationPath(
  content: ResumeContent,
  path: ParsedArrayPath,
  suggestedContent: string,
): ResumeContent {
  const nestedIndex = path.nestedIndex
  if (path.field === 'Details' && nestedIndex !== null) {
    return {
      ...content,
      education: content.education.map((entry, index) =>
        index === path.index
          ? { ...entry, details: replaceArrayValue(entry.details, nestedIndex, suggestedContent) }
          : entry,
      ),
    }
  }

  return applyObjectFieldPath(content, 'education', path, suggestedContent)
}

function applyObjectFieldPath<
  K extends 'experience' | 'education' | 'certifications' | 'links',
>(
  content: ResumeContent,
  key: K,
  path: ParsedArrayPath,
  suggestedContent: string,
): ResumeContent {
  const field = toCamelCase(path.field)
  if (!field) return content

  return {
    ...content,
    [key]: content[key].map((entry, index) =>
      index === path.index ? { ...entry, [field]: suggestedContent } : entry,
    ),
  }
}

function replaceAt<K extends 'skills'>(
  content: ResumeContent,
  key: K,
  index: number,
  suggestedContent: string,
): ResumeContent {
  return {
    ...content,
    [key]: replaceArrayValue(content[key], index, suggestedContent),
  }
}

function replaceArrayValue(values: string[], index: number, nextValue: string): string[] {
  if (index < 0 || index >= values.length) {
    return values
  }

  const next = [...values]
  next[index] = nextValue
  return next
}

interface ParsedArrayPath {
  section: TailoringSection
  index: number
  field: string | null
  nestedIndex: number | null
}

function parseArrayPath(path: string): ParsedArrayPath | null {
  const match = /^(?<section>\w+)\[(?<index>\d+)\](?:\.(?<field>\w+)(?:\[(?<nestedIndex>\d+)\])?)?$/.exec(path)
  const groups = match?.groups
  const section = groups?.section
  if (!section || !(section in sectionToContentKey)) {
    return null
  }

  return {
    section: section as TailoringSection,
    index: Number(groups.index),
    field: groups.field ?? null,
    nestedIndex:
      groups.nestedIndex === undefined ? null : Number(groups.nestedIndex),
  }
}

function toCamelCase(value: string | null): string | null {
  if (!value) return null
  return value.charAt(0).toLowerCase() + value.slice(1)
}

function cloneContent(content: ResumeContent): ResumeContent {
  return {
    personalInfo: content.personalInfo ? { ...content.personalInfo } : null,
    summary: content.summary,
    skills: [...content.skills],
    experience: content.experience.map((entry) => ({
      ...entry,
      bullets: [...entry.bullets],
    })),
    education: content.education.map((entry) => ({
      ...entry,
      details: [...entry.details],
    })),
    certifications: content.certifications.map((entry) => ({ ...entry })),
    links: content.links.map((entry) => ({ ...entry })),
  }
}
