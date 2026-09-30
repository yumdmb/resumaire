# Spec Delta

## MODIFIED Requirements

### Requirement: Dashboard status filtering
The system SHALL show a dashboard of the user's jobs as a board with one column per status by default, offer a list view as an alternative, and let users filter by status, search by title or company, and sort. The chosen view SHALL persist across visits on the same device.

#### Scenario: Filter dashboard by status
- **WHEN** a signed-in user selects the `Interview` status filter
- **THEN** the dashboard shows only that user's jobs with `Interview` status

#### Scenario: Default board view
- **WHEN** a signed-in user opens the dashboard for the first time
- **THEN** jobs are shown grouped in columns for Saved, Applied, Interview, Offer and Rejected, each with a count

#### Scenario: Switch to list view
- **WHEN** a signed-in user switches to list view and later returns to the dashboard
- **THEN** the list view is shown

#### Scenario: Search jobs
- **WHEN** a signed-in user types part of a job title or company name into the search field
- **THEN** only matching jobs are shown in the current view

#### Scenario: Sort jobs
- **WHEN** a signed-in user chooses a sort option such as most recently updated or company name
- **THEN** jobs within each column or list are ordered accordingly

#### Scenario: Empty column
- **WHEN** a board column has no jobs
- **THEN** the column shows a short empty hint and remains a valid drop target

#### Scenario: Board on small screens
- **WHEN** the dashboard is viewed at phone width
- **THEN** the board remains usable, with columns scrollable or stacked and the status menu available as the way to change status
