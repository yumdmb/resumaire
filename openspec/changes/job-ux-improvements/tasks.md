## 1. Backend: Status PATCH Endpoint

- [x] 1.1 Add `PATCH /api/jobs/{id}/status` endpoint that accepts `{ "status": "..." }`, validates ownership and status value, updates only status and updatedAt, and returns the updated job summary
- [x] 1.2 Add backend test coverage for PATCH status (valid change, invalid status, non-owned job)

## 2. Frontend: StatusDropdown Component

- [x] 2.1 Create a reusable `StatusDropdown` component that renders the current status badge as a clickable trigger, shows a positioned dropdown with all statuses, and fires an `onChange` callback on selection
- [x] 2.2 Add click-outside-to-close and keyboard escape handling to the dropdown
- [x] 2.3 Add CSS for the status dropdown (positioned below the badge, matches existing design tokens)

## 3. Frontend: Inline Status on Dashboard

- [x] 3.1 Add `patchJobStatus(jobId, status)` function to the frontend API client
- [x] 3.2 Replace the static badge in `DashboardPage` job rows with the `StatusDropdown` component
- [x] 3.3 Wire the dropdown to call `patchJobStatus`, update local state optimistically, and revert on error
- [x] 3.4 Ensure clicking the status badge does not trigger row navigation (`stopPropagation`)

## 4. Frontend: Inline Status on Job Detail

- [x] 4.1 Replace the static status badge in the job detail sidebar with the `StatusDropdown` component
- [x] 4.2 Wire the dropdown to call `patchJobStatus` and update the local job state

## 5. Frontend: Version Row Actions

- [ ] 5.1 Add "Preview" link button to each tailored resume version row that navigates to `/resume/preview/{versionId}?jobId={jobId}`
- [ ] 5.2 Add "Export PDF" button to each tailored resume version row that triggers a direct PDF download
- [ ] 5.3 Add loading/disabled state to the export button while download is in progress

## 6. Frontend: Job Resume Attachment

- [ ] 6.1 Add "Use for application" button to each version row (hidden if already selected)
- [ ] 6.2 Wire the button to call the existing job PUT endpoint with `selectedTailoredResumeId` set to the chosen version
- [ ] 6.3 Show a "Selected" indicator on the currently attached version row
- [ ] 6.4 Add an "Attached resume" section to the job detail sidebar showing the selected version name with Preview and Export PDF links
- [ ] 6.5 Show a placeholder in the sidebar section when no version is attached
