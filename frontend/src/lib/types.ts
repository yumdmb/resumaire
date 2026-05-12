// Shared types for the frontend. Mirrors backend DTOs in
// backend/Endpoints/JobsEndpoints.cs.

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
