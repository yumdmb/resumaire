# Design

## Context

The frontend is React 19 + react-router 7 + Vite, with no UI libraries. All styling is one global `frontend/src/index.css` (~2,000 lines) using an oklch token block at the top but many hard-coded values below. The shell is `App.tsx` (grid: 48px topbar, 220px sidebar). Status is rendered as `.badge-*` classes and a `StatusDropdown` component. The Jobs page (`DashboardPage.tsx`) fetches per-filter from `jobsApi.list(status)` and patches status optimistically, reverting silently on failure. Tests are Vitest + Testing Library (page tests for Resume builder, Tailoring, AiReviewPanel). See proposal.md for motivation.

## Goals / Non-Goals

**Goals:**
- One coherent, distinctive visual system driven entirely by CSS custom properties, with light and dark themes.
- A small in-repo component layer that removes repeated markup and makes status/feedback consistent.
- A Jobs board that is genuinely different from the old list while staying keyboard and touch usable.
- Keep the existing routes, API calls and business logic intact; restyle and restructure markup only.

**Non-Goals:**
- No backend, API or schema changes.
- No CSS framework migration (Tailwind, CSS-in-JS) and no component library such as MUI.
- No change to the Typst PDF template; preview only matches it visually.
- No new features beyond those in the specs (no notifications, calendar, analytics).

## Decisions

**1. Plain CSS with layered files and tokens, not a framework.**
Split `index.css` into `styles/tokens.css` (colour, type, space, radius, motion for `:root` and `[data-theme="dark"]`), `base.css` (reset, typography, focus), `components.css` (shared components) and per-area files (`shell`, `jobs`, `resume`, `tailoring`, `auth`, `landing`), imported in order from `main.tsx`. Alternative: Tailwind. Rejected: it would rewrite every component's markup for no user-visible gain, and the existing code is class-based.

**2. Theme via `data-theme` on `<html>`, applied before paint.**
Tokens are defined for light on `:root`, for dark under `@media (prefers-color-scheme: dark) :root:not([data-theme="light"])` and under `:root[data-theme="dark"]`. A tiny inline script in `index.html` reads `localStorage` (try/catch) and sets `data-theme` before first paint; a `ThemeProvider` hook exposes `theme` (`system|light|dark`) and `setTheme`. `color-scheme` is set so native controls follow. Alternative: React-only state. Rejected: causes a flash of the wrong theme.

**3. Palette and type.**
Warm paper neutrals (light: off-white ~oklch 97% 0.008 80, ink ~oklch 20% 0.02 60; dark: warm charcoal, not pure black), accent vermilion ~oklch 58% 0.17 40 with a darker text variant that passes AA on both surfaces. Status hues are muted and mutually distinct from the accent (saved neutral, applied ochre, interview teal, offer green, rejected brick). Fonts via Google Fonts with `font-display: swap`: Fraunces (display serif), Geist or Instrument Sans (UI), Geist Mono/JetBrains Mono (metadata). System-font fallbacks defined in tokens. Final contrast values are verified in the audit task, not assumed.

**4. StatusMark instead of pill badges.**
A small filled/outlined marker glyph plus text label, coloured by status token. One component used on cards, list rows, detail and selector, replacing `.badge-*`. Text label guarantees the not-colour-only requirement.

**5. Board built on `@dnd-kit/core` (+ `@dnd-kit/sortable` not needed).**
Columns are droppables, cards are draggables using PointerSensor (with an activation distance so a plain click still opens detail) and KeyboardSensor (native keyboard drag support), plus TouchSensor with a press delay. Alternatives: native HTML5 drag-and-drop (no touch support, poor a11y) and `react-beautiful-dnd` (unmaintained). Independently of drag, every card has a status menu button (see 5b), which is the primary path on phones and for assistive tech.

**5b. New accessible `StatusMenu`, replacing `StatusDropdown`.**
A menu-button pattern (button with `aria-haspopup="menu"`, `role="menu"`/`menuitemradio`, arrow-key navigation, Escape closes and returns focus), rendered in a portal so it is never a DOM descendant of the card link. This is the structural fix for the earlier bug: interactive controls are siblings of the navigation link, never children. Card navigation uses the stretched-link pattern already applied to the list row.

**6. Board data flow.**
The board needs all statuses at once, so it loads `jobsApi.list('All')` once and groups client-side; the status filter becomes a column visibility filter and search/sort are client-side over that list (job counts are small per user). Status change is optimistic through one shared `useJobStatusChange` hook that also serves list view and detail: on failure it reverts, shows a Toast with Retry, and ignores stale responses. View mode (`board|list`), sort and last filter persist in `localStorage` with try/catch.

**7. Toast as a small in-repo provider.**
`ToastProvider` + `useToast()` with a polite `aria-live` region, auto-dismiss (~6s, paused on hover/focus), manual dismiss, and optional action button. About 100 lines; no dependency. Alternative: `sonner`/`react-hot-toast`. Rejected: trivial to own, and it must match the design system exactly.

**8. Shell.**
`AppShell` becomes a CSS grid with a 64-72px rail (icons + labels appear below the wordmark; labels visible, not icon-only, for clarity) at ≥ 800px, and a fixed bottom nav below that. Rail hosts ThemeToggle and Sign out. Content max-width is constrained per page type (reading pages narrow, board full-width, resume editor two-pane).

**9. Page structure changes** (markup only, logic preserved):
Job detail: two columns (description and notes left; status timeline, attached resume, versions right). Job form: sticky bottom action bar. Resume builder: sticky section index with scroll-spy. Tailoring: a Stepper header component driven by the existing page state (select → review → saved); suggestion cards get before/after blocks. Preview: neutral desk background with a paper sheet. Landing: hero, how-it-works, honesty statement. Auth: two-column split, brand panel hidden on mobile.

**10. Motion.**
Tokens for duration/easing (120-200ms). Motion used only for: card lift while dragging, drop settle, menu/toast enter, step change. A global `prefers-reduced-motion: reduce` rule collapses durations to ~0.

## Risks / Trade-offs

- **Large surface area; regressions in untested screens** → land in the five phases from the proposal, each a commit that builds, lints and passes tests; screenshot each page in both themes after each phase using the browser.
- **Drag-and-drop mis-fires on click or scroll (touch)** → activation distance/delay, plus the menu path; test that a click without movement still navigates and that drop calls `patchStatus` once.
- **Client-side grouping diverges from server filtering** → filter/sort/search all live in one pure function with unit tests; the API is only asked for the full list.
- **AA contrast on a warm accent in dark mode** → tokens have separate `accent` (fills) and `accent-text` (text) values; verify with a script/manual contrast check and adjust.
- **Google Fonts dependency (privacy, offline)** → fallbacks defined, `display=swap`; can be self-hosted later without code changes.
- **Existing tests couple to markup/classes** → update in the same phase as the markup they cover, prefer role/label queries.
- **Two typefaces plus mono add page weight** → subset weights (2-3 per family), preconnect.
