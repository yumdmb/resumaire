## ADDED Requirements

### Requirement: Job keyword extraction
The system SHALL extract important keywords from a job description, including required skills, tools, frameworks, responsibilities, and seniority level signals.

#### Scenario: Extract keywords from job description
- **WHEN** a signed-in user requests tailoring for a job with a job description
- **THEN** the system identifies relevant skills, tools, responsibilities, and seniority signals from that description

### Requirement: Resume comparison
The system SHALL compare extracted job keywords with the user's base resume content to identify matches, gaps, and reorder opportunities.

#### Scenario: Compare job and resume
- **WHEN** keyword extraction completes for a job
- **THEN** the system identifies which keywords are already supported by the base resume and which are missing

### Requirement: Honest AI-assisted suggestions
The system SHALL generate tailored suggestions that only rephrase, reorder, and emphasize existing resume content and SHALL NOT fabricate companies, roles, projects, credentials, metrics, or experience.

#### Scenario: Supported suggestion
- **WHEN** the base resume contains React project experience and the job description emphasizes React
- **THEN** the system may suggest rephrasing or moving that React experience to make it more prominent

#### Scenario: Unsupported keyword
- **WHEN** the job description contains a skill or responsibility not supported by the base resume
- **THEN** the system marks it as a missing keyword or gap instead of adding it as user experience

### Requirement: Suggest tailored changes review
The system SHALL present tailoring output as reviewable suggestions with original content, suggested content or ordering, rationale, and accept/reject/edit controls.

#### Scenario: Review suggestions
- **WHEN** tailoring suggestions are generated
- **THEN** the user can accept, reject, or edit each suggestion before it changes a tailored resume

### Requirement: Manual tailoring
The system SHALL allow users to manually edit tailored resume sections without using AI suggestions.

#### Scenario: Edit tailored resume manually
- **WHEN** a signed-in user edits a tailored resume section directly
- **THEN** the system saves the user's manual edit

### Requirement: Tailored resume versions
The system SHALL save tailored resume versions associated with the authenticated user, the source job, and the source base resume content used to create the version.

#### Scenario: Save tailored version
- **WHEN** a signed-in user saves a tailored resume after accepting suggestions or manual edits
- **THEN** the system stores a tailored resume version associated with the job and user

#### Scenario: View saved versions for job
- **WHEN** a signed-in user opens a job detail page
- **THEN** the system lists tailored resume versions saved for that job
