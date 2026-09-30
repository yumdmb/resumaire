# Spec Delta

## Purpose

Defines the shared visual language, theming, reusable components, navigation shell, and accessibility baseline that every Resumaire screen follows, so the product feels consistent, calm, and usable in light and dark themes.

## ADDED Requirements

### Requirement: Warm editorial visual identity
The system SHALL present all screens with a single visual identity: warm neutral surfaces, one warm accent colour, a serif typeface for display headings, a sans-serif typeface for interface text, and a monospace typeface for dates and metadata. Surfaces SHALL be separated by hairline rules and tone rather than drop shadows.

#### Scenario: Consistent identity across screens
- **WHEN** a user navigates between Jobs, Job detail, Resume, Tailoring, Preview, Landing, Login and Register
- **THEN** every screen uses the same colour, type and spacing tokens and no screen uses the retired blue-violet accent

#### Scenario: Fonts fail to load
- **WHEN** web fonts cannot be loaded
- **THEN** text renders in fallback system fonts without layout breaking or text becoming invisible

### Requirement: Light and dark themes
The system SHALL provide a light theme and a dark theme. By default it SHALL follow the operating system preference, and users SHALL be able to override it with a manual toggle whose choice persists across sessions on that device.

#### Scenario: Follow system preference
- **WHEN** a user with no saved theme choice opens the app while their system prefers dark
- **THEN** the app renders in the dark theme

#### Scenario: Manual override persists
- **WHEN** a user selects the light theme with the toggle and reloads the page
- **THEN** the app renders in the light theme regardless of the system preference

#### Scenario: Storage unavailable
- **WHEN** browser storage cannot be read or written
- **THEN** the app still renders correctly using the system preference and the toggle still switches the theme for the current page view

#### Scenario: No flash of wrong theme
- **WHEN** the page first loads with a saved theme choice
- **THEN** the correct theme is applied before first paint

### Requirement: Shared component library
The system SHALL provide shared Button, StatusMark, Field, Card, EmptyState, Skeleton, PageHeader and Toast components, and screens SHALL use them for these patterns instead of bespoke markup.

#### Scenario: Status shown consistently
- **WHEN** a job status is displayed on the board, list, job detail or tailoring job selector
- **THEN** it is rendered by the same StatusMark with the same label and colour for that status

#### Scenario: Status not conveyed by colour alone
- **WHEN** a status is displayed
- **THEN** it includes a text label so it is understandable without seeing colour

### Requirement: Toast feedback
The system SHALL show transient toast notifications for the outcome of user actions that fail in the background, and a failure toast SHALL offer a retry action where retrying is meaningful.

#### Scenario: Failure toast
- **WHEN** a background save such as a status change fails
- **THEN** a toast describes the failure and offers a retry action

#### Scenario: Toast is announced
- **WHEN** a toast appears
- **THEN** it is exposed to assistive technology through a live region and does not steal focus

#### Scenario: Toast dismissal
- **WHEN** a toast is not interacted with
- **THEN** it dismisses itself after a short time, and the user can also dismiss it manually

### Requirement: Navigation shell
The system SHALL provide a slim navigation rail for signed-in screens containing the wordmark, primary navigation (Jobs, Resume, Tailoring), the theme toggle and sign out. On narrow screens the rail SHALL become a bottom navigation bar. The shell SHALL NOT display a development-stage label such as "MVP".

#### Scenario: Desktop navigation
- **WHEN** a signed-in user views any app screen at desktop width
- **THEN** the rail is visible, the current section is marked as current, and the content area uses the remaining width

#### Scenario: Mobile navigation
- **WHEN** a signed-in user views any app screen at phone width
- **THEN** primary navigation is available as a bottom bar and content does not scroll horizontally

### Requirement: Accessibility baseline
The system SHALL keep text and essential controls at WCAG AA contrast in both themes, show a visible focus indicator on all keyboard-focusable elements, and be fully operable by keyboard.

#### Scenario: Keyboard focus visible
- **WHEN** a user tabs through any screen
- **THEN** each focused control shows a clearly visible focus indicator

#### Scenario: Contrast in both themes
- **WHEN** body text, secondary text, status labels and the accent on its surface are measured in either theme
- **THEN** each meets at least 4.5:1 contrast for text (3:1 for large text and non-text indicators)

### Requirement: Reduced motion
The system SHALL limit motion to short, purposeful transitions and SHALL disable or minimise non-essential animation when the user prefers reduced motion.

#### Scenario: Reduced motion preference
- **WHEN** a user's system prefers reduced motion
- **THEN** transitions and animations such as card movement and skeleton shimmer are removed or reduced to instant changes
