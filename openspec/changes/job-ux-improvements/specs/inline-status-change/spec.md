## ADDED Requirements

### Requirement: Inline status change on dashboard
The system SHALL allow users to change a job's status directly from the dashboard list by clicking the status badge, without navigating away from the page.

#### Scenario: Change status from dashboard
- **WHEN** a signed-in user clicks the status badge on a job row in the dashboard
- **THEN** a dropdown appears showing all valid statuses (Saved, Applied, Interview, Offer, Rejected)

#### Scenario: Select new status from dashboard dropdown
- **WHEN** a signed-in user selects a different status from the dashboard dropdown
- **THEN** the system updates the job status immediately and the badge reflects the new status

#### Scenario: Dashboard dropdown does not navigate
- **WHEN** a signed-in user clicks the status badge on a job row
- **THEN** the click does not navigate to the job detail page

#### Scenario: Status update fails on dashboard
- **WHEN** a status update request fails
- **THEN** the badge reverts to the previous status and an error message is shown briefly

### Requirement: Inline status change on job detail page
The system SHALL allow users to change a job's status from the job detail page sidebar without opening the edit form.

#### Scenario: Change status from job detail
- **WHEN** a signed-in user clicks the status badge/dropdown in the job detail sidebar
- **THEN** a dropdown appears showing all valid statuses

#### Scenario: Select new status from detail dropdown
- **WHEN** a signed-in user selects a different status from the job detail dropdown
- **THEN** the system updates the job status immediately and the display reflects the new status

### Requirement: Dedicated status update endpoint
The system SHALL provide a lightweight endpoint for updating only the job status field.

#### Scenario: PATCH status successfully
- **WHEN** a signed-in user sends a PATCH request with a valid status for a job they own
- **THEN** the system updates only the status and updatedAt fields and returns the updated job

#### Scenario: PATCH status for non-owned job
- **WHEN** a signed-in user sends a PATCH request for a job they do not own
- **THEN** the system returns 404 without exposing the job's existence

#### Scenario: PATCH with invalid status
- **WHEN** a signed-in user sends a PATCH request with an unsupported status value
- **THEN** the system returns a validation error
