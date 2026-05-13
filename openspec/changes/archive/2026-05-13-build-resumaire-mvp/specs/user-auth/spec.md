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

### Requirement: Frontend registration
The system SHALL provide a registration page that creates a new account without email confirmation and redirects to the dashboard on success.

#### Scenario: New user registers
- **WHEN** a visitor submits a valid email and password (12+ characters) on the register page
- **THEN** the system creates the account, stores the access token, and navigates to the dashboard

#### Scenario: Registration with invalid input
- **WHEN** a visitor types an invalid email or a password shorter than 12 characters
- **THEN** the system shows inline validation errors as the user types, before form submission

#### Scenario: Registration with duplicate email
- **WHEN** a visitor submits an email that already exists
- **THEN** the system shows a server error message without exposing whether the email is registered

### Requirement: Frontend login
The system SHALL provide a login page that authenticates an existing user and redirects to the dashboard on success.

#### Scenario: Existing user logs in
- **WHEN** a user submits valid credentials on the login page
- **THEN** the system stores the access token and navigates to the dashboard

#### Scenario: Login with wrong credentials
- **WHEN** a user submits incorrect email or password
- **THEN** the system shows a generic error without revealing which field is wrong

### Requirement: Frontend session management
The system SHALL manage authentication state centrally and protect all app routes except the landing page, login, and register.

#### Scenario: Token expires during use
- **WHEN** any API request returns 401 Unauthorized
- **THEN** the system clears the stored token and redirects to the login page

#### Scenario: User logs out
- **WHEN** a signed-in user triggers logout
- **THEN** the system clears the stored token and redirects to the login page

#### Scenario: Unauthenticated user visits protected route
- **WHEN** an unauthenticated user navigates to a protected route
- **THEN** the system redirects to the login page

### Requirement: Landing page for unauthenticated users
The system SHALL show a minimal landing page with login and register links when the user is not authenticated.

#### Scenario: Anonymous user visits root
- **WHEN** an unauthenticated user visits /
- **THEN** the system shows the landing page with calls to action for login and registration

#### Scenario: Authenticated user visits root
- **WHEN** an authenticated user visits /
- **THEN** the system shows the dashboard
