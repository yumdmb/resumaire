## Why

Resumaire needs an MVP plan that turns the product requirements into a buildable application: a lightweight job tracker with structured resume editing, trustworthy tailoring suggestions, and PDF export. The requested architecture also changes the initial stack direction from a single Next.js app to separate React and ASP.NET Core projects.

## What Changes

- Add a split application structure with a React frontend in `frontend/` and an ASP.NET Core backend in `backend/`.
- Add authenticated user access for managing private jobs, resume profiles, tailored resumes, and exports.
- Add a minimal job tracker with company, title, posting link, job description, status, date applied, notes, and linked resume version.
- Add a structured base resume profile that stores personal info, summary, skills, experience, education, certifications, and links as JSON-backed sections.
- Add a tailoring workflow that extracts job keywords, compares them with the base resume, and proposes honest section-level edits that users can accept, reject, or manually edit.
- Add HTML resume preview and PDF export from structured resume content.
- Defer reminders, calendar sync, analytics, kanban complexity, and payments until after the MVP.

## Capabilities

### New Capabilities

- `user-auth`: Authenticated access and user-owned data boundaries.
- `job-tracking`: CRUD and status management for saved job applications.
- `resume-profile`: Structured base resume creation and editing.
- `resume-tailoring`: Manual and AI-assisted tailored resume suggestions tied to a job and base resume.
- `pdf-export`: HTML resume preview and PDF download generation.

### Modified Capabilities

- None.

## Impact

- Creates new `frontend/` and `backend/` folders.
- Introduces React frontend dependencies and ASP.NET Core backend dependencies.
- Introduces persistent storage for users, jobs, base resumes, and tailored resumes.
- Introduces backend API contracts for authentication-aware resume, job, tailoring, and export workflows.
- Introduces AI integration for suggestion generation with explicit guardrails against fabricated experience.
- Introduces server-side PDF rendering/export infrastructure.
