## Why

Managing job application status requires navigating to the edit form, which is too many steps for the most frequent action in the app. Additionally, tailored resume versions on the job detail page lack preview/export actions and there's no way to mark which version was actually used for an application.

## What Changes

- Add inline status dropdown on the dashboard job list (click badge to change status without leaving the list)
- Add inline status dropdown on the job detail page sidebar (replace static badge with clickable dropdown)
- Add "Preview" and "Export PDF" action buttons to each tailored resume version row on the job detail page
- Add ability to select/attach a tailored resume version to a job as the "applied with" version
- Show the currently attached resume version in the job detail sidebar with quick preview/export links

## Capabilities

### New Capabilities
- `inline-status-change`: Click-to-change status dropdown on both the dashboard list and job detail page, with immediate save (no form submission required)
- `job-resume-attachment`: Select which tailored resume version was used for a job application, displayed in the job detail sidebar with preview/export actions
- `version-row-actions`: Preview and Export PDF buttons on each tailored resume version row in the job detail page

### Modified Capabilities

## Impact

- Frontend: `DashboardPage.tsx`, `JobDetailPage.tsx` — new interactive components
- Frontend: `api.ts` — new `patchJobStatus` function for lightweight status updates
- Backend: Optional `PATCH /api/jobs/{id}/status` endpoint (or reuse existing PUT)
- No database changes needed — `selectedTailoredResumeId` already exists on the Job entity
- No new dependencies required
