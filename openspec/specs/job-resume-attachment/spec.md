# job-resume-attachment Specification

## Purpose
TBD - created by archiving change job-ux-improvements. Update Purpose after archive.
## Requirements
### Requirement: Attach tailored resume version to job
The system SHALL allow users to manually select which tailored resume version is attached to a job application.

#### Scenario: Attach a version from the versions list
- **WHEN** a signed-in user clicks "Use for application" on a tailored resume version row
- **THEN** the system sets that version as the job's selected tailored resume and updates the UI to show it as selected

#### Scenario: Change attached version
- **WHEN** a signed-in user attaches a different version to a job that already has one attached
- **THEN** the system replaces the previous selection with the new one

#### Scenario: Detach version
- **WHEN** a signed-in user clicks to remove the attached version
- **THEN** the system clears the selected tailored resume from the job

### Requirement: Display attached resume in job detail sidebar
The system SHALL show the currently attached tailored resume version in the job detail sidebar with quick actions.

#### Scenario: Show attached version in sidebar
- **WHEN** a signed-in user views a job detail page that has an attached tailored resume
- **THEN** the sidebar displays the version name/number with "Preview" and "Export PDF" links

#### Scenario: No version attached
- **WHEN** a signed-in user views a job detail page with no attached tailored resume
- **THEN** the sidebar shows a placeholder indicating no version is selected

