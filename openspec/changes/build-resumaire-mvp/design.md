## Context

The repository currently contains requirements and an empty OpenSpec setup, with no application code yet. The MVP is a SaaS-style resume tailoring and job tracking product, but the requested stack is a split application: a React frontend in `frontend/` and an ASP.NET Core backend in `backend/`.

The application handles private user data, job descriptions, resume history, AI-generated suggestions, and PDF exports. The backend must own secrets, authorization, persistence, AI calls, and PDF generation. The frontend should provide fast editing, review, accept/reject interactions, dashboard filtering, and an HTML resume preview.

## Goals / Non-Goals

**Goals:**

- Establish separate frontend and backend projects with clear API boundaries.
- Build a minimal authenticated workflow: sign in, create base resume, add jobs, request tailored suggestions, edit tailored resume, export PDF, and update job status.
- Store resumes as structured JSON so sections can be edited, compared, suggested, rendered, and exported deterministically.
- Ensure AI suggestions rephrase, reorder, and emphasize existing experience without fabricating companies, roles, projects, credentials, or metrics.
- Generate PDF exports from HTML derived from saved structured resume content.

**Non-Goals:**

- Calendar sync, reminders, analytics, kanban boards, and payments.
- Multi-tenant organization management.
- Fully automated job scraping from external postings.
- AI-generated resumes that bypass user review.
- Multiple resume templates beyond one clean MVP template.

## Decisions

### Use React as a separate SPA frontend

The `frontend/` project will be a React app that consumes the backend API. It will organize code by features such as `auth`, `jobs`, `resume-profile`, `tailoring`, and `export`, with shared UI and API client modules kept separate.

Rationale: the user requested React and separate folders. A SPA keeps the editor and accept/reject suggestion workflow responsive without coupling UI rendering to ASP.NET Core views.

Alternatives considered:

- Next.js: originally recommended in `requirements.md`, but it conflicts with the requested split React/ASP.NET Core architecture.
- ASP.NET Core server-rendered UI: simpler deployment, but it would not satisfy the requested React frontend.

### Use ASP.NET Core Minimal APIs for the backend

The `backend/` project will expose JSON APIs using ASP.NET Core Minimal APIs grouped by feature. Route handlers should stay thin and delegate to application services. The backend will use OpenAPI in development, ProblemDetails for errors, health checks for operations, and CORS configured for the frontend origin.

Rationale: the MVP API surface is focused and benefits from low ceremony. Minimal APIs are a good fit as long as feature groups, DTOs, validation, and services keep the code organized.

Alternatives considered:

- Controller-based Web API: viable if the API grows substantially or requires controller filters/conventions, but unnecessary for the initial MVP.
- One monolithic frontend/backend project: simpler folder count, but conflicts with the requested structure.

### Use PostgreSQL with EF Core

The backend will persist data in PostgreSQL through EF Core. Core tables/entities are users, jobs, base resumes, tailored resumes, and tailoring suggestion records if suggestions need audit/history separate from accepted resume content.

Rationale: the requirements already identify PostgreSQL, the data is relational, and JSON resume content can be stored while still preserving ownership and queryable job metadata.

Alternatives considered:

- Supabase as a complete backend: conflicts with the requested ASP.NET Core backend ownership.
- Document database: structured resume JSON fits documents, but job status, user ownership, and version relationships are straightforward relational data.

### Use backend-owned authentication and authorization

Authentication will be implemented in the backend, with all user-owned endpoints protected and every query scoped to the authenticated user. The frontend stores only the client-side session state needed to call the API.

Rationale: private resume and job data must be isolated by user. Backend-owned auth keeps authorization checks close to persistence and avoids exposing secrets or privileged operations to the frontend.

Alternatives considered:

- Clerk or Supabase Auth: good SaaS options, but they add external account dependencies that are not required to define the MVP.

### Use structured resume JSON as the source of truth

Base and tailored resumes will be stored as versioned JSON documents with typed sections: personal info, summary, skills, experience, education, certifications, and links. Tailored resumes will reference the source job and base resume snapshot used at creation time.

Rationale: section-level JSON enables manual editing, suggestion diffs, keyword matching, HTML preview, and PDF export without asking AI to manipulate opaque documents.

Alternatives considered:

- Raw text or raw HTML as the source of truth: easy initially, but fragile for editing, suggestions, diffing, and safe export.

### Treat AI output as suggestions, not automatic truth

The backend will send job description and resume JSON to the AI provider and request structured suggestions. Each suggestion will identify the target section, original text when applicable, suggested text/order, rationale, and source evidence from existing resume content. The frontend will show accept/reject/edit controls before changes become part of a tailored resume.

Rationale: this directly supports the product trust requirement. The system must not fabricate experience, and user review is the enforcement point.

Alternatives considered:

- One-click resume generation: faster, but less trustworthy and more likely to create unverifiable content.

### Generate PDFs server-side from saved resume content

The backend will generate HTML from saved structured resume content and render PDF using a server-side browser renderer such as Playwright for .NET. The frontend preview should visually match the server template closely, but saved backend data remains the export source of truth.

Rationale: server-side export avoids exposing rendering internals, keeps downloads reproducible, and allows consistent PDF generation across user browsers.

Alternatives considered:

- Browser print/export from the frontend: simpler, but output varies by browser and is harder to test.
- AI-generated PDF content: rejected because export should be deterministic from user-approved resume data.

### Use a centralized auth context with route guards for frontend authentication

The frontend will use a React context provider (`AuthContext`) that holds the current user state, access token, and loading flag. A `ProtectedRoute` wrapper component will redirect unauthenticated users to `/login`. The auth context will:

- On mount: check localStorage for an existing token and validate it via `GET /api/users/me`
- Expose `login(email, password)`, `register(email, password)`, and `logout()` functions
- Intercept 401 responses from any API call to trigger automatic logout and redirect

This replaces the per-page `hasAccessToken()` checks and "unauthorized" state handling currently scattered across DashboardPage, JobDetailPage, JobFormPage, and ResumeBuilderPage.

Rationale: centralizing auth state eliminates duplicated logic, provides a single point of control for session management, and makes route protection declarative rather than imperative in each page component.

Alternatives considered:

- Keep per-page auth checks: works but duplicates logic across every protected page and makes the unauthorized UX inconsistent.
- Silent token refresh: adds complexity (refresh token storage, race conditions) without clear MVP benefit. Redirect to login on expiry is simpler and acceptable for an MVP.

### Use register-and-go without email confirmation

New users can register with email and password (12+ characters, unique email) and immediately access the app. No email verification step.

Rationale: reduces friction for MVP onboarding. Email confirmation can be added later as a separate change without breaking existing accounts.

### Show a minimal landing page for unauthenticated users

The root route `/` shows a simple landing page with the product name and login/register CTAs when no user is authenticated. Authenticated users see the dashboard at the same route.

Rationale: gives the app a public entry point without building a full marketing site. The landing page is a placeholder that can be expanded later.

### Use inline form validation for auth forms

Login and register forms validate input as the user types (email format, password length) rather than only on submit. Server errors (duplicate email, wrong credentials) display after submission.

Rationale: immediate feedback reduces failed submissions and communicates requirements clearly, especially the 12-character password minimum.

## Risks / Trade-offs

- AI may suggest unsupported claims -> Require structured suggestions with source evidence and keep accept/reject review mandatory.
- Resume JSON schema may evolve -> Version resume documents and add migration code when schema changes.
- Server-side PDF rendering can be operationally heavy -> Keep the MVP to one template, queue or rate-limit export if usage grows, and health-check the renderer dependency.
- Cross-origin auth can be misconfigured -> Centralize CORS and auth middleware in `Program.cs`, keep allowed origins environment-specific, and test unauthorized/forbidden responses.
- Minimal API files can grow messy -> Group endpoints by feature and keep business logic in services.
- Frontend and backend contracts can drift -> Generate or publish OpenAPI in development and keep typed frontend API adapters close to feature code.

## Migration Plan

1. Create `frontend/` and `backend/` projects.
2. Add backend persistence, auth, OpenAPI, CORS, health checks, and baseline feature endpoints.
3. Add frontend routing, API client, auth screens, and feature pages.
4. Implement job tracker and resume profile first because tailoring depends on them.
5. Add AI suggestion generation and review UI.
6. Add HTML preview and PDF export.
7. Add focused backend and frontend tests for ownership, core workflows, and export behavior.

Rollback is simple during MVP development: revert the change branch or remove the newly created `frontend/`, `backend/`, and related OpenSpec artifacts before production data exists.

## Open Questions

- ~~Which production auth model should be used for launch: backend identity only, or a hosted identity provider?~~ Decided: backend ASP.NET Identity with bearer tokens, register-and-go (no email confirmation), 401 triggers redirect to login (no silent refresh).
- Which PostgreSQL hosting target should be assumed for deployment?
- Which AI model and budget limits should be configured for MVP usage?
- Should tailored resume history retain every suggestion batch, only accepted versions, or both?
