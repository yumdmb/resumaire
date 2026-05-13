// Shared types for the frontend. Mirrors backend DTOs in
// backend/Endpoints/JobsEndpoints.cs and backend/Contracts/ResumeContentDto.cs.

// ─── Resume types ─────────────────────────────────────────────

export interface ResumePersonalInfo {
  fullName: string | null
  email: string | null
  phone: string | null
  location: string | null
  headline: string | null
  website: string | null
}

export interface ResumeExperience {
  id: string | null
  role: string | null
  organization: string | null
  location: string | null
  startDate: string | null
  endDate: string | null
  isCurrent: boolean
  bullets: string[]
}

export interface ResumeEducation {
  id: string | null
  institution: string | null
  degree: string | null
  field: string | null
  location: string | null
  startDate: string | null
  endDate: string | null
  details: string[]
}

export interface ResumeCertification {
  id: string | null
  name: string | null
  issuer: string | null
  issuedDate: string | null
  expirationDate: string | null
  credentialId: string | null
  url: string | null
}

export interface ResumeLink {
  id: string | null
  label: string | null
  url: string | null
}

export interface ResumeContent {
  personalInfo: ResumePersonalInfo | null
  summary: string | null
  skills: string[]
  experience: ResumeExperience[]
  education: ResumeEducation[]
  certifications: ResumeCertification[]
  links: ResumeLink[]
}

export interface BaseResumeResponse {
  id: string
  schemaVersion: number
  revision: number
  content: ResumeContent
  createdAt: string
  updatedAt: string
}

export function emptyResumeContent(): ResumeContent {
  return {
    personalInfo: {
      fullName: null,
      email: null,
      phone: null,
      location: null,
      headline: null,
      website: null,
    },
    summary: null,
    skills: [],
    experience: [],
    education: [],
    certifications: [],
    links: [],
  }
}

export function emptyExperience(): ResumeExperience {
  return {
    id: crypto.randomUUID(),
    role: null,
    organization: null,
    location: null,
    startDate: null,
    endDate: null,
    isCurrent: false,
    bullets: [],
  }
}

export function emptyEducation(): ResumeEducation {
  return {
    id: crypto.randomUUID(),
    institution: null,
    degree: null,
    field: null,
    location: null,
    startDate: null,
    endDate: null,
    details: [],
  }
}

export function emptyCertification(): ResumeCertification {
  return {
    id: crypto.randomUUID(),
    name: null,
    issuer: null,
    issuedDate: null,
    expirationDate: null,
    credentialId: null,
    url: null,
  }
}

export function emptyLink(): ResumeLink {
  return {
    id: crypto.randomUUID(),
    label: null,
    url: null,
  }
}

// ─── Job types ────────────────────────────────────────────────

export const JOB_STATUSES = [
  'Saved',
  'Applied',
  'Interview',
  'Offer',
  'Rejected',
] as const

export type JobStatus = (typeof JOB_STATUSES)[number]

export interface JobSummary {
  id: string
  company: string
  title: string
  link: string | null
  status: JobStatus
  dateApplied: string | null
  notes: string | null
  selectedBaseResumeId: string | null
  selectedTailoredResumeId: string | null
  createdAt: string
  updatedAt: string
}

export interface JobDetail extends JobSummary {
  description: string
  tailoredResumeVersions: TailoredResumeVersion[]
}

export interface TailoredResumeVersion {
  id: string
  versionNumber: number
  name: string | null
  sourceBaseResumeId: string
  sourceBaseResumeRevision: number
  schemaVersion: number
  createdAt: string
  updatedAt: string
}

export interface JobFormValues {
  company: string
  title: string
  link: string
  description: string
  status: JobStatus
  dateApplied: string
  notes: string
}

export type FieldErrors = Partial<Record<keyof JobFormValues, string[]>> & {
  _general?: string[]
}

// ─── Tailoring types ───────────────────────────────────────────

export interface JobKeyword {
  text: string
  category: string
  aliases: string[]
  mentionCount: number
}

export interface ResumeKeywordEvidence {
  section: string
  path: string
  text: string
}

export interface SupportedKeyword {
  keyword: JobKeyword
  evidence: ResumeKeywordEvidence[]
}

export interface MissingKeyword {
  keyword: JobKeyword
}

export interface ReorderOpportunity {
  keyword: JobKeyword
  currentEvidence: ResumeKeywordEvidence[]
  suggestedSections: string[]
}

export interface KeywordComparison {
  supportedKeywords: SupportedKeyword[]
  missingKeywords: MissingKeyword[]
  reorderOpportunities: ReorderOpportunity[]
}

export interface TailoringAnalysis {
  jobId: string
  baseResumeId: string
  baseResumeRevision: number
  extractedKeywords: JobKeyword[]
  comparison: KeywordComparison
}

export interface TailoringSourceEvidence {
  section: string
  path: string
  text: string
}

export interface TailoringSuggestion {
  id: string
  reviewState: 'Pending' | 'Accepted' | 'Rejected'
  targetSection: string
  originalContent: string | null
  suggestedContent: string
  rationale: string | null
  aiNotes: string | null
  sourceEvidence: TailoringSourceEvidence[]
  createdAt: string
  reviewedAt: string | null
}

export interface TailoringGapNote {
  keyword: string
  reason: string
}

export interface TailoringSuggestionBatch {
  jobId: string
  baseResumeId: string
  baseResumeRevision: number
  suggestions: TailoringSuggestion[]
  gapNotes: TailoringGapNote[]
  guardrailRejections: string[]
}

export interface TailoredResumeDetail {
  id: string
  versionNumber: number
  name: string | null
  jobId: string
  sourceBaseResumeId: string
  sourceBaseResumeRevision: number
  schemaVersion: number
  content: ResumeContent
  createdAt: string
  updatedAt: string
}
