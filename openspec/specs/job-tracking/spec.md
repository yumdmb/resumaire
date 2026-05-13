# job-tracking Specification

## Purpose
TBD - created by archiving change build-resumaire-mvp. Update Purpose after archive.
## Requirements
### Requirement: Job record management
The system SHALL allow authenticated users to create, view, update, and delete job records with company, job title, job link, job description, status, optional date applied, notes, and resume version fields.

#### Scenario: Create job
- **WHEN** a signed-in user submits a company, job title, and job description
- **THEN** the system creates a job record owned by that user

#### Scenario: Update job details
- **WHEN** a signed-in user edits a job's status, notes, date applied, link, description, title, company, or resume version
- **THEN** the system saves the updated job record

### Requirement: Job status workflow
The system SHALL support the statuses `Saved`, `Applied`, `Interview`, `Rejected`, and `Offer` for each job.

#### Scenario: Change job status
- **WHEN** a signed-in user changes a job status to one of the supported values
- **THEN** the system stores and displays the new status

#### Scenario: Reject unsupported status
- **WHEN** a request attempts to set a job status outside the supported values
- **THEN** the system rejects the status change

### Requirement: Dashboard status filtering
The system SHALL show a dashboard of the user's jobs with filters by status.

#### Scenario: Filter dashboard by status
- **WHEN** a signed-in user selects the `Interview` status filter
- **THEN** the dashboard shows only that user's jobs with `Interview` status

### Requirement: Job detail view
The system SHALL provide a job detail view that shows the job description, current status, notes, tailoring action, and saved resume versions for that job.

#### Scenario: View job detail
- **WHEN** a signed-in user opens one of their jobs
- **THEN** the system shows the job description, status, notes, tailoring action, and associated tailored resume versions

