import { describe, expect, it } from 'vitest'
import type { ResumeContent, TailoringSuggestion } from '../../lib/types'
import { applyAcceptedSuggestions } from './applyAcceptedSuggestions'
import type { SuggestionDecision, SuggestionState } from './types'

function content(): ResumeContent {
  return {
    personalInfo: {
      fullName: 'Ada Lovelace',
      email: null,
      phone: null,
      location: null,
      headline: 'Old headline',
      website: null,
    },
    summary: 'Old summary.',
    skills: ['React', 'TypeScript', 'Go'],
    experience: [
      {
        id: 'e1',
        role: 'Engineer',
        organization: 'Example Co',
        location: null,
        startDate: null,
        endDate: null,
        isCurrent: false,
        bullets: ['First.', 'Second.'],
      },
    ],
    education: [
      {
        id: 'ed1',
        institution: 'Uni',
        degree: 'BSc',
        field: null,
        location: null,
        startDate: null,
        endDate: null,
        details: ['Honours.'],
      },
    ],
    certifications: [],
    links: [],
    projects: [{ id: 'p1', name: 'Engine', url: null, technologies: null, bullets: ['Built it.'] }],
    activities: [
      { id: 'a1', title: 'Hack', location: null, role: null, date: null, bullets: ['Won.'] },
    ],
  }
}

function state(
  targetPath: string,
  operation: TailoringSuggestion['operation'],
  editedContent: string,
  decision: SuggestionDecision = 'accepted',
): SuggestionState {
  return {
    decision,
    editedContent,
    suggestion: {
      id: `${targetPath}-${operation}`,
      reviewState: 'Pending',
      targetSection: targetPath.split(/[.[]/)[0],
      targetPath,
      operation,
      originalContent: null,
      suggestedContent: editedContent,
      rationale: null,
      aiNotes: null,
      sourceEvidence: [],
      createdAt: '2026-01-01T00:00:00Z',
      reviewedAt: null,
    },
  }
}

describe('applyAcceptedSuggestions', () => {
  it('replaces only the targeted bullet and leaves role and organization alone', () => {
    const result = applyAcceptedSuggestions(content(), [
      state('Experience[0].Bullets[1]', 'Replace', 'Second, rewritten.'),
    ])

    expect(result.experience[0].bullets).toEqual(['First.', 'Second, rewritten.'])
    expect(result.experience[0].role).toBe('Engineer')
    expect(result.experience[0].organization).toBe('Example Co')
  })

  it('does not change the resume for rejected or pending suggestions', () => {
    const base = content()

    const result = applyAcceptedSuggestions(base, [
      state('Summary', 'Replace', 'Nope', 'rejected'),
      state('Summary', 'Replace', 'Nope', 'pending'),
    ])

    expect(result).toEqual(base)
  })

  it('replaces the summary and headline', () => {
    const result = applyAcceptedSuggestions(content(), [
      state('Summary', 'Replace', 'New summary.'),
      state('PersonalInfo.Headline', 'Replace', 'New headline'),
    ])

    expect(result.summary).toBe('New summary.')
    expect(result.personalInfo?.headline).toBe('New headline')
    expect(result.personalInfo?.fullName).toBe('Ada Lovelace')
  })

  it('reorders skills without adding new ones or duplicating', () => {
    const result = applyAcceptedSuggestions(content(), [
      state('Skills', 'SetSkills', 'TypeScript, Terraform, React, typescript'),
    ])

    expect(result.skills).toEqual(['TypeScript', 'React'])
  })

  it('reorders one skill category and mirrors the flat list', () => {
    const base = {
      ...content(),
      skillGroups: [
        { category: 'Frontend', items: ['React', 'CSS'] },
        { category: 'Backend', items: ['Go', 'SQL'] },
      ],
    }

    const result = applyAcceptedSuggestions(base, [
      state('SkillGroups[0]', 'SetSkills', 'CSS, SQL, React'),
    ])

    expect(result.skillGroups?.[0].items).toEqual(['CSS', 'React'])
    expect(result.skillGroups?.[1].items).toEqual(['Go', 'SQL'])
    expect(result.skills).toEqual(['CSS', 'React', 'Go', 'SQL'])
    expect(applyAcceptedSuggestions(base, [state('Skills', 'SetSkills', 'React')])).toEqual(base)
  })

  it('adds a bullet to the targeted list for experience, education, projects and activities', () => {
    const result = applyAcceptedSuggestions(content(), [
      state('Experience[0].Bullets', 'AddBullet', 'Added experience.'),
      state('Education[0].Details', 'AddBullet', 'Added detail.'),
      state('Projects[0].Bullets', 'AddBullet', 'Added project.'),
      state('Activities[0].Bullets', 'AddBullet', 'Added activity.'),
    ])

    expect(result.experience[0].bullets).toEqual(['First.', 'Second.', 'Added experience.'])
    expect(result.education[0].details).toEqual(['Honours.', 'Added detail.'])
    expect(result.projects?.[0].bullets).toEqual(['Built it.', 'Added project.'])
    expect(result.activities?.[0].bullets).toEqual(['Won.', 'Added activity.'])
  })

  it.each([
    ['Experience[0].Role', 'Replace'],
    ['Experience[5].Bullets[0]', 'Replace'],
    ['Experience[0].Bullets[9]', 'Replace'],
    ['Experience[0].Bullets[0]', 'AddBullet'],
    ['Experience[0].Details[0]', 'Replace'],
    ['Skills[0]', 'SetSkills'],
    ['Certifications[0].Name', 'Replace'],
  ] as const)('ignores non-editable or missing target %s (%s)', (path, operation) => {
    const base = content()

    expect(applyAcceptedSuggestions(base, [state(path, operation, 'Injected')])).toEqual(base)
  })

  it('ignores blank text and legacy suggestions that have no target', () => {
    const base = content()

    expect(
      applyAcceptedSuggestions(base, [
        state('Summary', 'Replace', '   '),
        state('', 'Replace', 'No target'),
      ]),
    ).toEqual(base)
  })

  it('does not mutate the input resume', () => {
    const base = content()
    const snapshot = JSON.stringify(base)

    applyAcceptedSuggestions(base, [state('Experience[0].Bullets[0]', 'Replace', 'Changed.')])

    expect(JSON.stringify(base)).toBe(snapshot)
  })
})
