# Spec Delta

## MODIFIED Requirements

### Requirement: Inline status change on dashboard
The system SHALL allow users to change a job's status directly from the dashboard, without navigating away from the page, either by dragging a job between status columns on the board, or by using a keyboard-accessible status menu on the job card or list row.

#### Scenario: Change status from dashboard menu
- **WHEN** a signed-in user activates the status control on a job card or list row
- **THEN** a menu appears showing all valid statuses (Saved, Applied, Interview, Offer, Rejected)

#### Scenario: Select new status from dashboard menu
- **WHEN** a signed-in user selects a different status from the menu
- **THEN** the system updates the job status immediately and the job appears under the new status

#### Scenario: Drag job to another status column
- **WHEN** a signed-in user drags a job card from one status column to another on the board and drops it
- **THEN** the system updates the job to that column's status immediately and the card appears in the new column

#### Scenario: Keyboard status change
- **WHEN** a signed-in user operates the status menu using only the keyboard (open, arrow keys, confirm, Escape to close)
- **THEN** the status can be changed and focus returns to the control

#### Scenario: Status control does not navigate
- **WHEN** a signed-in user activates the status control, selects a status from the menu, or drops a card in a column
- **THEN** the app does not navigate to the job detail page

#### Scenario: Status update fails on dashboard
- **WHEN** a status update request fails
- **THEN** the job returns to its previous status and a failure toast is shown with a retry action

#### Scenario: Retry after failure
- **WHEN** a signed-in user activates retry on the failure toast
- **THEN** the system attempts the same status change again

## ADDED Requirements

### Requirement: Status timeline on job detail
The system SHALL show on the job detail page the job's current status prominently, together with a timeline of its status stages that indicates the current stage.

#### Scenario: View timeline
- **WHEN** a signed-in user opens a job whose status is Interview
- **THEN** the detail page shows the stages with Interview marked as the current stage
