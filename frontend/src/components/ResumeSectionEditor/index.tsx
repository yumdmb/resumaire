import type { ResumeContent } from '../../lib/types'
import { CertificationsEditor } from './CertificationsEditor'
import { EducationEditor } from './EducationEditor'
import { ExperienceEditor } from './ExperienceEditor'
import { LinksEditor } from './LinksEditor'
import { PersonalInfoEditor } from './PersonalInfoEditor'
import { SkillsEditor } from './SkillsEditor'
import { SummaryEditor } from './SummaryEditor'
import type { ResumeSectionId, SectionUpdater } from './sections'

export { RESUME_SECTIONS } from './sections'
export type { ResumeSectionDef, ResumeSectionId, SectionUpdater } from './sections'

interface Props {
  sectionId: ResumeSectionId
  content: ResumeContent
  updateSection: SectionUpdater
  /**
   * Optional prefix for input `id` attributes, to avoid collisions when
   * multiple editor instances render simultaneously (e.g., base resume builder
   * and manual tailoring on the same page tree).
   */
  idPrefix?: string
}

/**
 * Routes to the appropriate editor for the given section. Each editor only
 * mutates its own slice of ResumeContent, preserving unrelated sections.
 */
export function ResumeSectionEditor({
  sectionId,
  content,
  updateSection,
  idPrefix = '',
}: Props) {
  switch (sectionId) {
    case 'personal':
      return (
        <PersonalInfoEditor
          value={content.personalInfo}
          onChange={(v) => updateSection('personalInfo', v)}
          idPrefix={idPrefix}
        />
      )
    case 'summary':
      return (
        <SummaryEditor
          value={content.summary}
          onChange={(v) => updateSection('summary', v)}
          idPrefix={idPrefix}
        />
      )
    case 'skills':
      return (
        <SkillsEditor
          value={content.skills}
          onChange={(v) => updateSection('skills', v)}
        />
      )
    case 'experience':
      return (
        <ExperienceEditor
          value={content.experience}
          onChange={(v) => updateSection('experience', v)}
          idPrefix={idPrefix}
        />
      )
    case 'education':
      return (
        <EducationEditor
          value={content.education}
          onChange={(v) => updateSection('education', v)}
          idPrefix={idPrefix}
        />
      )
    case 'certifications':
      return (
        <CertificationsEditor
          value={content.certifications}
          onChange={(v) => updateSection('certifications', v)}
          idPrefix={idPrefix}
        />
      )
    case 'links':
      return (
        <LinksEditor
          value={content.links}
          onChange={(v) => updateSection('links', v)}
          idPrefix={idPrefix}
        />
      )
  }
}
