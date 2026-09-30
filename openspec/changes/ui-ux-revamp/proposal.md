# Proposal

## Why

The current UI is a generic SaaS template: Inter, a blue-violet accent, topbar plus sidebar, boxed cards, and pill badges. Styling lives in a single 2,000-line `index.css` with hard-coded values, there is no dark theme, no shared component layer, and failures (e.g. a failed status change) are silent. A recent bug (status dropdown nested inside a row `<Link>` navigated to job detail instead of changing status) showed the interaction layer is fragile. Resumaire is a document-centric product; the interface should feel like a well-set page, not a dashboard template.

## What Changes

- New visual identity, "warm editorial paper": warm neutrals, single vermilion/terracotta accent, serif display type + clean sans UI type + mono metadata, small radii, hairline rules, no shadows.
- Design tokens (colour, type, space, radius, motion) split out of `index.css`; light and dark themes ship together, following `prefers-color-scheme` with a manual toggle persisted in `localStorage`.
- Shared UI components: Button, StatusMark, Field, Card, EmptyState, Skeleton, PageHeader, Toast, ThemeToggle. Pages stop repeating raw class strings.
- App shell: slim left rail (wordmark, nav, theme toggle, sign out) replaces the topbar+sidebar; "MVP" tag removed; rail collapses to a bottom bar on small screens.
- Jobs page: default view becomes a status **board** (one column per status) with drag-and-drop between columns and a keyboard/menu alternative; list view kept as a toggle; search and sort added. Failed status changes show a toast with retry instead of silently reverting.
- Job detail: two-column layout with a status timeline; job form: sticky action bar.
- Resume builder: sticky section index and calmer entry cards. Tailoring: stepper flow (choose job, review, save) with before/after diffs. Preview: paper-on-desk presentation.
- Landing: real hero and how-it-works section. Login/Register: split layout with brand panel.
- Accessibility baseline: visible `:focus-visible`, WCAG AA contrast in both themes, `prefers-reduced-motion` respected.

No backend or API changes. No **BREAKING** changes to data or routes.

## Capabilities

### New Capabilities
- `ui-design-system`: theme tokens, light/dark theming with persisted toggle, shared components, toast feedback, app shell and navigation, responsive and accessibility requirements.

### Modified Capabilities
- `inline-status-change`: status changes also happen via board drag-and-drop and a keyboard-accessible menu; failure feedback becomes a toast with retry; dropdown/menu must never trigger navigation.
- `job-tracking`: dashboard gains a board view (default) alongside the list, plus search and sort, in addition to status filtering.

## Impact

- Frontend only: `frontend/src/index.css` (split into token/base/component/page styles), `App.tsx`, all pages under `frontend/src/pages`, `components/`, `features/tailoring`, `ResumeSectionEditor`.
- New dependencies: Google Fonts (serif, sans, mono), a small drag-and-drop library (e.g. `@dnd-kit/core`), and a small toast implementation (own context, or a tiny library).
- Existing Vitest tests updated where markup changes; new tests for theme persistence, board status change, and toast undo.
- Typst PDF export and backend are untouched; resume preview styling stays visually consistent with the export.
