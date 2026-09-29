# resume-tailoring Specification

## Purpose
TBD - created by archiving change build-resumaire-mvp. Update Purpose after archive.
## Requirements
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

### Requirement: Targeted, non-destructive suggestions
Each AI suggestion SHALL change exactly one editable resume location (summary, headline, a single bullet or detail line, a bullet list append, or a reorder of existing skills) and SHALL NOT change roles, employers, institutions, degrees, project names, dates, or credentials.

#### Scenario: Suggestion targets an identity field
- **WHEN** the AI proposes changing a role, employer, institution, degree, or project name
- **THEN** the system rejects that suggestion before it is shown to the user

#### Scenario: Skills reorder
- **WHEN** the AI proposes a new skills list
- **THEN** the system accepts it only if every skill is already on the resume

### Requirement: Server-applied acceptance
The system SHALL build a tailored resume from accepted suggestions on the server, using the current base resume and the user's edited text, so the saved version differs from the base resume only by what the user accepted.

#### Scenario: Accept an edited suggestion
- **WHEN** a user edits a suggestion's text and accepts it
- **THEN** the saved version contains the edited text at the suggestion's target and is otherwise identical to the base resume

#### Scenario: Base resume changed after generation
- **WHEN** the base resume changed after suggestions were generated and the user tries to save them
- **THEN** the system refuses with a conflict and asks the user to generate new suggestions

#### Scenario: Suggestion already used or replaced
- **WHEN** a user saves a suggestion that was already saved or was superseded by newer suggestions
- **THEN** the system refuses with a conflict

### Requirement: AI request limits and failure reporting
The system SHALL limit AI suggestion requests per user and SHALL report provider failures with a specific, safe reason.

#### Scenario: Limit reached
- **WHEN** a user exceeds the configured number of suggestion requests
- **THEN** the system responds with 429 and a retry delay

#### Scenario: Provider failure
- **WHEN** the provider is unreachable, times out, rejects the key, or returns a truncated or unreadable reply
- **THEN** the system responds with a 502 whose detail names the cause without echoing resume content

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

