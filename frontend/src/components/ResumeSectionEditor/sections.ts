import type { ResumeContent } from '../../lib/types'

export type ResumeSectionId =
  | 'personal'
  | 'summary'
  | 'skills'
  | 'experience'
  | 'education'
  | 'certifications'
  | 'links'

export interface ResumeSectionDef {
  id: ResumeSectionId
  label: string
  meta: (content: ResumeContent) => string
}

/** Declarative list of resume sections for rendering the section list UI. */
export const RESUME_SECTIONS: ResumeSectionDef[] = [
  {
    id: 'personal',
    label: 'Personal info',
    meta: (c) => c.personalInfo?.fullName || 'Not set',
  },
  {
    id: 'summary',
    label: 'Summary',
    meta: (c) =>
      c.summary
        ? `${c.summary.slice(0, 48)}${c.summary.length > 48 ? '…' : ''}`
        : 'Not set',
  },
  {
    id: 'skills',
    label: 'Skills',
    meta: (c) => `${c.skills.length} skill${c.skills.length === 1 ? '' : 's'}`,
  },
  {
    id: 'experience',
    label: 'Experience',
    meta: (c) =>
      `${c.experience.length} entr${c.experience.length === 1 ? 'y' : 'ies'}`,
  },
  {
    id: 'education',
    label: 'Education',
    meta: (c) =>
      `${c.education.length} entr${c.education.length === 1 ? 'y' : 'ies'}`,
  },
  {
    id: 'certifications',
    label: 'Certifications',
    meta: (c) =>
      `${c.certifications.length} entr${c.certifications.length === 1 ? 'y' : 'ies'}`,
  },
  {
    id: 'links',
    label: 'Links',
    meta: (c) => `${c.links.length} link${c.links.length === 1 ? '' : 's'}`,
  },
]

/** Updates a single top-level section of ResumeContent, preserving the rest. */
export type SectionUpdater = <K extends keyof ResumeContent>(
  key: K,
  value: ResumeContent[K],
) => void
