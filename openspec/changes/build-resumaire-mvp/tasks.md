## 1. Project Setup

- [x] 1.1 Create `frontend/` React project with TypeScript, routing, linting, and test tooling
- [x] 1.2 Create `backend/` ASP.NET Core API project targeting the selected stable .NET version
- [x] 1.3 Add root-level documentation for running frontend, backend, database, and tests locally
- [x] 1.4 Configure environment variable templates for frontend API URL and backend secrets

## 2. Backend Foundation

- [x] 2.1 Configure ASP.NET Core services, middleware, OpenAPI, ProblemDetails, CORS, HTTPS redirection, and health checks
- [x] 2.2 Add PostgreSQL and EF Core configuration with initial migration support
- [x] 2.3 Add user authentication and authorization for protected API endpoints
- [x] 2.4 Add common API response, validation, and authenticated user helpers
- [x] 2.5 Add backend integration test infrastructure with test database support

## 3. Data Model

- [x] 3.1 Create user-owned job entity with status enum and resume version reference fields
- [x] 3.2 Create base resume entity with versioned structured JSON content
- [x] 3.3 Create tailored resume entity linked to user, job, and source base resume snapshot
- [x] 3.4 Create optional tailoring suggestion persistence for AI notes, review state, and accepted changes
- [x] 3.5 Add EF Core migrations and ownership indexes for user-scoped queries

## 4. Job Tracking

- [x] 4.1 Implement backend job CRUD endpoints scoped to the authenticated user
- [x] 4.2 Implement backend status validation for `Saved`, `Applied`, `Interview`, `Rejected`, and `Offer`
- [x] 4.3 Implement backend job detail endpoint with saved tailored resume versions
- [x] 4.4 Build frontend dashboard with status filters
- [x] 4.5 Build frontend add/edit job form and job detail page
- [x] 4.6 Add tests for job ownership, CRUD behavior, status filtering, and invalid statuses

## 5. Resume Profile

- [x] 5.1 Define resume JSON schema and DTOs for personal info, summary, skills, experience, education, certifications, and links
- [x] 5.2 Implement backend base resume read and save endpoints with validation
- [ ] 5.3 Build frontend resume builder with section-level editing
- [ ] 5.4 Preserve unrelated resume sections when editing a single section
- [ ] 5.5 Add tests for resume validation, ownership, and section updates

## 6. Tailoring Workflow

- [ ] 6.1 Implement backend keyword extraction from job descriptions
- [ ] 6.2 Implement backend comparison of extracted keywords against base resume content
- [ ] 6.3 Integrate AI suggestion generation behind a backend service with no secrets exposed to the frontend
- [ ] 6.4 Enforce AI guardrails that prevent fabricated companies, roles, projects, credentials, metrics, or experience
- [ ] 6.5 Implement backend endpoints for generating, saving, and listing tailored resume versions
- [ ] 6.6 Build frontend tailoring review UI with original content, suggested content, rationale, and accept/reject/edit controls
- [ ] 6.7 Build manual tailored resume editing without requiring AI suggestions
- [ ] 6.8 Add tests for unsupported keywords, accepted suggestions, rejected suggestions, manual edits, and saved versions

## 7. Resume Preview And PDF Export

- [ ] 7.1 Implement backend HTML rendering from saved structured resume content
- [ ] 7.2 Build frontend resume preview for base and tailored resume versions
- [ ] 7.3 Integrate server-side PDF rendering from saved resume HTML
- [ ] 7.4 Implement protected PDF export endpoint for owned resume versions
- [ ] 7.5 Ensure export excludes rejected or unaccepted suggestions and handles unsaved editor changes clearly
- [ ] 7.6 Add tests for preview rendering, export ownership, and export source integrity

## 8. End-To-End Verification

- [ ] 8.1 Add frontend tests for dashboard, job detail, resume builder, tailoring review, and export UI states
- [ ] 8.2 Add end-to-end workflow test for sign in, create base resume, add job, tailor resume, save version, and export PDF
- [ ] 8.3 Run backend test suite and frontend test suite
- [ ] 8.4 Verify local startup instructions for both `frontend/` and `backend/`
