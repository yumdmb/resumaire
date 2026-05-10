## ADDED Requirements

### Requirement: HTML resume preview
The system SHALL render an HTML preview from saved structured resume content for base and tailored resumes.

#### Scenario: Preview tailored resume
- **WHEN** a signed-in user opens a tailored resume preview
- **THEN** the system displays the selected resume version as an HTML resume preview

#### Scenario: Preview reflects saved edits
- **WHEN** a signed-in user saves changes to a resume section
- **THEN** the preview reflects the saved structured resume content

### Requirement: PDF download
The system SHALL export a selected saved resume version as a downloadable PDF generated from HTML.

#### Scenario: Export tailored resume PDF
- **WHEN** a signed-in user requests PDF export for one of their tailored resume versions
- **THEN** the system generates and downloads a PDF for that resume version

### Requirement: Export source integrity
The system SHALL generate PDFs only from saved resume content and SHALL exclude rejected or unaccepted AI suggestions.

#### Scenario: Export after rejecting suggestion
- **WHEN** a signed-in user rejects an AI suggestion and exports the tailored resume
- **THEN** the PDF excludes the rejected suggestion

#### Scenario: Export after unsaved edit
- **WHEN** a signed-in user has unsaved editor changes and requests export
- **THEN** the system either requires saving first or exports the most recently saved resume content with clear UI state

### Requirement: Export ownership enforcement
The system SHALL allow users to export only resume versions they own.

#### Scenario: Export another user's resume
- **WHEN** a signed-in user requests PDF export for another user's resume version
- **THEN** the system denies the export without exposing the resume content
