# version-row-actions Specification

## Purpose
TBD - created by archiving change job-ux-improvements. Update Purpose after archive.
## Requirements
### Requirement: Preview button on tailored version rows
The system SHALL display a "Preview" button on each tailored resume version row in the job detail page.

#### Scenario: Click preview on a version
- **WHEN** a signed-in user clicks "Preview" on a tailored resume version row
- **THEN** the system navigates to the resume preview page for that version

### Requirement: Export PDF button on tailored version rows
The system SHALL display an "Export PDF" button on each tailored resume version row in the job detail page.

#### Scenario: Click export on a version
- **WHEN** a signed-in user clicks "Export PDF" on a tailored resume version row
- **THEN** the system downloads the PDF for that version without navigating away from the page

#### Scenario: Export in progress
- **WHEN** a PDF export is in progress for a version
- **THEN** the export button shows a loading state and is disabled until the download completes or fails

