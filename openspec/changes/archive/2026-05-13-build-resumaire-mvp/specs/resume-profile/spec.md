## ADDED Requirements

### Requirement: Structured base resume
The system SHALL allow authenticated users to create and maintain one base resume stored as structured sections for personal info, summary, skills, work or project experience, education, certifications, and links.

#### Scenario: Create base resume
- **WHEN** a signed-in user saves base resume sections
- **THEN** the system stores the resume as structured data owned by that user

#### Scenario: Edit base resume
- **WHEN** a signed-in user updates one or more base resume sections
- **THEN** the system persists the updated structured resume and records an updated timestamp

### Requirement: Section-level editing
The system SHALL support editing resume sections independently so users can update summary, skills, experience bullets, education, certifications, and links without rewriting the entire resume.

#### Scenario: Update skills
- **WHEN** a signed-in user reorders, adds, edits, or removes skills
- **THEN** the system saves the updated skills section without discarding unrelated resume sections

#### Scenario: Update experience bullet
- **WHEN** a signed-in user edits an experience bullet
- **THEN** the system saves the changed bullet without discarding other experience entries

### Requirement: Resume data validation
The system SHALL validate base resume data before saving and SHALL reject malformed section data.

#### Scenario: Save malformed resume content
- **WHEN** a signed-in user submits resume content that does not match the expected section structure
- **THEN** the system rejects the save and reports validation errors

### Requirement: Base resume source for tailoring
The system SHALL use the saved base resume as the source material for tailoring suggestions.

#### Scenario: Tailoring starts from base resume
- **WHEN** a signed-in user requests tailoring for a job
- **THEN** the system uses that user's saved base resume as the source content
