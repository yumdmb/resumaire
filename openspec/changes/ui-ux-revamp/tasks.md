# Tasks

Each numbered group is one phase and one commit. Every group must leave `npm run build`, `npm run lint` and `npm test` (in `frontend/`) green.

## 1. Foundation: tokens, theme, shared components

- [x] 1.1 Add fonts (Google Fonts links with preconnect, `display=swap`) and split `index.css` into `styles/tokens.css`, `base.css`, `components.css` and per-area files imported from `main.tsx`; verify the app renders identically apart from fonts and `npm run build` passes
- [x] 1.2 Define warm light and dark token sets (surfaces, ink, rules, vermilion accent + accent-text, five status hues, type scale, space, radius 4-6px, motion durations) with `color-scheme`; verify by rendering a temporary token sheet in both themes
- [x] 1.3 Add the pre-paint theme script in `index.html` and `ThemeProvider`/`useTheme` (`system|light|dark`, storage in try/catch) plus `ThemeToggle`; verify with Vitest tests for default-follows-system, override-persists, and storage-throws
- [x] 1.4 Build shared components in `components/ui/` (Button, StatusMark, Field, Card, EmptyState, Skeleton, PageHeader) with role/label-friendly markup; verify with a small render test per component and that StatusMark always includes a text label
- [x] 1.5 Build `ToastProvider`/`useToast` (polite live region, auto-dismiss with pause on hover/focus, manual dismiss, optional action); verify with tests for announce, auto-dismiss (fake timers), dismiss, and action callback
- [x] 1.6 Add global `:focus-visible` ring and `prefers-reduced-motion` rules in `base.css`; verify by keyboard-tabbing the current pages and emulating reduced motion in the browser

## 2. Shell, Jobs board, Job detail and form

- [x] 2.1 Replace topbar+sidebar with the left rail (wordmark, Jobs/Resume/Tailoring, ThemeToggle, Sign out), bottom nav under 800px, remove the MVP tag; verify at desktop and 375px widths in the browser and that active-route marking works
- [x] 2.2 Install `@dnd-kit/core` and write pure `groupJobs`/filter/search/sort helpers with unit tests (status grouping, title+company search case-insensitive, each sort option, empty input)
- [x] 2.3 Build `StatusMenu` (menu-button, arrow keys, Escape returns focus, portal render) replacing `StatusDropdown`; verify with tests for open, keyboard select, Escape, and that selecting never triggers navigation
- [x] 2.4 Add `useJobStatusChange` (optimistic update, revert on failure, failure toast with Retry, stale-response guard); verify with tests for success, failure+revert+toast, and retry re-calling `patchStatus`
- [x] 2.5 Build the Jobs board: five status columns with counts and empty hints, draggable cards (pointer with activation distance, keyboard and touch sensors), stretched-link card navigation, StatusMenu on each card; verify with a test that a drop calls `patchStatus` once with the target status, a plain click navigates, and by dragging in the browser
- [x] 2.6 Add view toggle (board/list), search, sort, status filter and persisted preferences to `DashboardPage`; restyle the list view with StatusMark; verify with page tests for view persistence, search and sort, and screenshot both views in light and dark
- [x] 2.7 Update the existing dashboard-related and shell tests for the new markup and remove `StatusDropdown` and its CSS; verify no references remain (grep) and the suite passes
- [x] 2.8 Redesign Job detail as two columns with the status timeline (current stage marked) and StatusMenu in the sidebar; verify with a test that Interview marks the Interview stage current, and screenshot
- [x] 2.9 Restyle Job form with sections and a sticky action bar using shared Field/Button; verify create and edit still submit (existing behaviour) and screenshot at 375px

## 3. Resume builder and Tailoring

- [x] 3.1 Restyle Resume builder: sticky section index with scroll-spy, calmer entry cards, collapse/expand entries, shared Field/Button; verify existing `ResumeBuilderPage` tests pass (updated as needed) and screenshot both themes
- [x] 3.2 Restyle remaining resume section editors (skills groups, bullets, projects, activities, links, certifications) to the new components; verify by editing and saving each section type in the browser
- [x] 3.3 Add a `Stepper` component and apply it to `TailoringPage` (select job, review, saved) driven by existing state; verify with updated `TailoringPage` tests that the current step is exposed (`aria-current="step"`)
- [x] 3.4 Redesign suggestion cards with before/after blocks, clear accepted/rejected states and non-colour cues, and restyle gap notes, keywords tab, save bar and saved confirmation; verify updated `AiReviewPanel` tests and a screenshot of a review with mixed states
- [x] 3.5 Restyle Resume preview as paper-on-desk matching the Typst output proportions; verify preview and PDF export still work (manual export from the browser)

## 4. Landing and auth

- [x] 4.1 Build the landing page: hero with a resume/tailoring illustration made in CSS/SVG, three-step how-it-works, honesty statement, primary/secondary CTAs; verify at desktop and 375px and that CTAs route to register/login
- [x] 4.2 Build split-layout Login and Register with brand panel (hidden on mobile), inline validation messages and shared Field/Button; verify existing auth behaviour (submit, error display, redirect) manually and via any existing tests

## 5. Dark mode, responsive and accessibility audit

- [x] 5.1 Verify every screen in light and dark (Landing, Login, Register, Jobs board/list, Job detail, Job form, Resume, Tailoring steps, Preview); fix any hard-coded colours found by grepping for `#` and `oklch(` outside token files
- [x] 5.2 Measure contrast for text, secondary text, StatusMark labels, accent and accent-text on their surfaces in both themes (script or devtools) and fix values under AA; record results in the commit message
- [x] 5.3 Keyboard-only pass over every screen (tab order, visible focus, menu and drag alternatives, dialogs, Escape) and a reduced-motion pass; fix findings
- [x] 5.4 Responsive pass at 375, 768 and 1280px including board horizontal scroll and bottom nav; fix overflow issues
- [ ] 5.5 Final integration check: `npm run build`, `npm run lint`, `npm test`, delete leftover dead CSS, refresh the README screenshots (`resumaire-*.png`) and mention the theme toggle in the README
