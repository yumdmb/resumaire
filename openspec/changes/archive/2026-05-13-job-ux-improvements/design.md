## Context

The job tracking dashboard and detail page currently require navigating to a full edit form to change job status. Tailored resume versions on the job detail page show metadata but lack action buttons for preview/export. The `selectedTailoredResumeId` field on the Job entity already exists but is not exposed in the frontend UI.

The existing backend PUT endpoint at `/api/jobs/{id}` already supports status changes and `selectedTailoredResumeId` updates. The export endpoints at `/api/export/tailored/{id}/preview` and `/api/export/tailored/{id}/pdf` are already implemented.

## Goals / Non-Goals

**Goals:**

- Allow status changes with a single click from both the dashboard list and job detail page
- Add preview and export PDF buttons to each tailored resume version row
- Allow users to manually attach a tailored resume version to a job
- Show the attached resume version in the job detail sidebar

**Non-Goals:**

- Kanban/drag-and-drop board view (explicitly excluded in the MVP design)
- Auto-prompting to attach a resume when status changes to "Applied"
- Batch status changes across multiple jobs
- Status change history/audit log

## Decisions

### Add a dedicated PATCH endpoint for status-only updates

Create `PATCH /api/jobs/{id}/status` that accepts `{ "status": "Applied" }` and returns the updated job summary. This avoids requiring the frontend to send the full job payload just to change status.

Rationale: The existing PUT requires all fields (company, title, description, etc.). Sending a full payload for a one-field change is wasteful and error-prone if the frontend doesn't have the full job loaded (e.g., on the dashboard where only summaries are available).

Alternatives considered:
- Reuse existing PUT: requires loading the full job detail first on the dashboard, adding an extra round-trip.
- Frontend-only optimistic update without a dedicated endpoint: risky if the PUT payload drifts.

### Use a click-outside-to-close dropdown component for inline status

Build a small `StatusDropdown` component that renders the current status badge as a button. On click, it shows a positioned dropdown with all valid statuses. Selecting one fires the PATCH immediately and updates local state optimistically.

Rationale: Keeps the interaction lightweight. No modal, no form, no save button. The dropdown closes on selection or click-outside.

Alternatives considered:
- Popover API: browser support is good but styling is harder to control cross-browser.
- Radix/Headless UI: adds a dependency for one component. Not worth it for the MVP.

### Reuse existing PUT for attaching a tailored resume version

The "Use for application" action on a version row will call the existing `PUT /api/jobs/{id}` with the current job data plus `selectedTailoredResumeId` set to the chosen version. The job detail page already has the full job loaded.

Rationale: No new endpoint needed. The job detail page has all the data required for the PUT. This keeps the backend surface minimal.

### Open preview in a new route, export as direct download

"Preview" navigates to `/resume/preview/{tailoredResumeId}?jobId={jobId}`. "Export PDF" triggers a direct blob download without navigation. Both routes already exist.

Rationale: Preview benefits from a full-page view (the iframe needs space). Export is a fire-and-forget download action.

## Risks / Trade-offs

- Optimistic status update on dashboard could show stale state if the PATCH fails → Show a brief error toast/flash and revert the badge on failure.
- Click-stopping the badge on dashboard rows prevents the row link from firing → Use `event.stopPropagation()` on the dropdown trigger. Users can still click anywhere else on the row.
- Multiple rapid status changes could race → Debounce or ignore clicks while a PATCH is in-flight (disable the dropdown briefly).
