## ADDED Requirements

### Requirement: User account access
The system SHALL allow a user to create an account, sign in, sign out, and access authenticated application features only while authenticated.

#### Scenario: Authenticated user accesses app data
- **WHEN** a signed-in user opens the dashboard
- **THEN** the system shows that user's jobs and resume data

#### Scenario: Anonymous user is blocked
- **WHEN** an unauthenticated user requests a protected page or API resource
- **THEN** the system denies access and requires authentication

### Requirement: User-owned data isolation
The system SHALL scope jobs, base resumes, tailored resumes, tailoring suggestions, and exports to the authenticated user that owns them.

#### Scenario: User requests another user's job
- **WHEN** a signed-in user requests a job owned by a different user
- **THEN** the system denies access without exposing the other user's job data

#### Scenario: User creates private data
- **WHEN** a signed-in user creates a job, base resume, or tailored resume
- **THEN** the system associates the record with that user

### Requirement: Protected API operations
The system SHALL require authenticated API access for creating, reading, updating, deleting, tailoring, and exporting user-owned resources.

#### Scenario: Unauthenticated API mutation
- **WHEN** an unauthenticated request attempts to create or update a job, resume, tailoring result, or export
- **THEN** the system rejects the request
