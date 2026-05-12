---
name: Resumaire
description: A structured workspace for managing job applications and tailoring resumes.
colors:
  focused-indigo: "#4059b5"
  indigo-subtle: "#e8ecf7"
  indigo-text: "#3550a3"
  surface-white: "#fdfcfb"
  surface-raised: "#fefefe"
  surface-sunken: "#f5f4f3"
  bg-wash: "#faf9f8"
  border-light: "#e3e2e0"
  border-strong: "#cccac8"
  text-primary: "#1e1f21"
  text-secondary: "#6b6d70"
  text-tertiary: "#9a9c9f"
  status-saved: "#9a9c9f"
  status-applied: "#4059b5"
  status-interview: "#2e8a8a"
  status-offer: "#3d8a3d"
  status-rejected: "#a84832"
typography:
  body:
    fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif"
    fontSize: "14px"
    fontWeight: 400
    lineHeight: 1.5
  label:
    fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif"
    fontSize: "12.5px"
    fontWeight: 500
    lineHeight: 1.3
    letterSpacing: "-0.005em"
  title:
    fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif"
    fontSize: "18px"
    fontWeight: 600
    lineHeight: 1.3
    letterSpacing: "-0.02em"
  mono:
    fontFamily: "'Cascadia Code', 'Consolas', ui-monospace, monospace"
    fontSize: "12px"
    fontWeight: 400
    lineHeight: 1.5
rounded:
  sm: "6px"
  md: "8px"
  lg: "12px"
  pill: "999px"
spacing:
  xs: "4px"
  sm: "8px"
  md: "16px"
  lg: "24px"
  xl: "32px"
components:
  button-primary:
    backgroundColor: "{colors.focused-indigo}"
    textColor: "{colors.surface-white}"
    rounded: "{rounded.sm}"
    padding: "6px 12px"
  button-primary-hover:
    backgroundColor: "#3a4fa6"
    textColor: "{colors.surface-white}"
  button-secondary:
    backgroundColor: "{colors.surface-white}"
    textColor: "{colors.text-primary}"
    rounded: "{rounded.sm}"
    padding: "6px 12px"
  button-secondary-hover:
    backgroundColor: "{colors.surface-sunken}"
    textColor: "{colors.text-primary}"
  input-default:
    backgroundColor: "{colors.surface-raised}"
    textColor: "{colors.text-primary}"
    rounded: "{rounded.sm}"
    padding: "8px 10px"
  input-focus:
    backgroundColor: "{colors.surface-white}"
    textColor: "{colors.text-primary}"
  nav-item-active:
    backgroundColor: "{colors.indigo-subtle}"
    textColor: "{colors.indigo-text}"
    rounded: "{rounded.sm}"
    padding: "8px 12px"
  badge-applied:
    backgroundColor: "{colors.indigo-subtle}"
    textColor: "{colors.status-applied}"
    rounded: "{rounded.pill}"
    padding: "2px 8px"
---

# Design System: Resumaire

## 1. Overview

**Creative North Star: "The Ruled Notebook"**

Resumaire is a structured, calm workspace that disappears into the task. Like a well-made ruled notebook, it provides just enough structure to keep work organized without drawing attention to itself. The interface is precise, quiet, and functional. It respects the user's focus and never interrupts flow with decoration or unnecessary motion.

The system draws from Linear and Raycast: dense information display, restrained color, and interactions that feel immediate rather than theatrical. Every element earns its place through utility. There is no ornamentation, no personality for its own sake. The personality IS the restraint.

This system explicitly rejects: filler copy, AI-sounding explanations, decorative flourishes, and anything that breaks the user's task flow.

**Key Characteristics:**
- Flat and tonal: depth through surface layering, never shadows
- Single accent used with discipline: the indigo appears sparingly and means "actionable"
- Dense but not cramped: 14px base with tight spacing that still breathes
- Immediate transitions: 100–120ms state changes, no choreography
- Structured over freeform: typed sections, not text blobs

## 2. Colors

A restrained palette built entirely in OKLCH. Tinted neutrals carry a faint cool undertone (hue 250) that keeps whites from feeling sterile. One accent color, used on less than 10% of any screen.

### Primary
- **Focused Indigo** (oklch(48% 0.18 264)): The single accent. Used for primary actions, active navigation states, and the "applied" job status. Its rarity is the point.
- **Indigo Subtle** (oklch(95% 0.04 264)): Background tint for active states and skill tags. Never used as a standalone surface.
- **Indigo Text** (oklch(42% 0.18 264)): Text rendered over indigo-subtle backgrounds. Darker than the fill accent for legibility.

### Neutral
- **Background Wash** (oklch(98% 0.004 250)): The page canvas. Not white; faintly warm-cool.
- **Surface** (oklch(100% 0.002 250)): Cards, sidebar, topbar. The "paper" layer.
- **Surface Raised** (oklch(99% 0.003 250)): Hover states on list rows. One step above surface.
- **Surface Sunken** (oklch(96% 0.005 250)): Recessed areas, code backgrounds, hover on nav items.
- **Border Light** (oklch(90% 0.006 250)): Default dividers and card outlines.
- **Border Strong** (oklch(82% 0.008 250)): Input hover borders, emphasis dividers.
- **Text Primary** (oklch(18% 0.01 250)): Headings, row titles, input values. Not black.
- **Text Secondary** (oklch(46% 0.01 250)): Descriptions, labels, supporting copy.
- **Text Tertiary** (oklch(62% 0.008 250)): Timestamps, hints, metadata. The quietest text.

### Status
- **Saved** (oklch(62% 0.01 250)): Neutral gray. No urgency.
- **Applied** (oklch(52% 0.14 264)): Indigo. Active, in-progress.
- **Interview** (oklch(58% 0.16 200)): Teal. Forward momentum.
- **Offer** (oklch(52% 0.16 145)): Green. Positive outcome.
- **Rejected** (oklch(54% 0.12 25)): Muted red. Closed, not alarming.

### Named Rules
**The One Voice Rule.** The accent indigo is used on no more than 10% of any given screen. Primary buttons, active nav, and the "Applied" badge. Nothing else. Its scarcity makes it meaningful.

**The Tinted Neutral Rule.** No pure white (#fff) or pure black (#000) anywhere. Every neutral carries chroma 0.004–0.01 at hue 250. This keeps the palette cohesive without visible color.

## 3. Typography

**Body Font:** Inter (with system-ui fallback stack)
**Mono Font:** Cascadia Code (with Consolas, ui-monospace fallback)

**Character:** Inter at 14px with OpenType features cv02, cv03, cv04, cv11 enabled. The result is a slightly more distinctive version of the default: disambiguated letterforms, no decorative flair. The font disappears; the content stays.

### Hierarchy
- **Title** (600, 18px, line-height 1.3, letter-spacing -0.02em): Page headings only. One per view.
- **Body** (400, 14px, line-height 1.5): Default reading text. Inputs, descriptions, prose blocks. Max line length 70ch.
- **Body Medium** (500, 13.5px, line-height 1.5): Row titles, nav items, form values. The workhorse weight.
- **Label** (500, 12.5px, line-height 1.3, letter-spacing -0.005em): Form labels, sidebar section headers, metadata.
- **Caption** (400–500, 11–12px): Timestamps, badge text, hints. The smallest text in the system.
- **Sidebar Label** (600, 11px, letter-spacing 0.06em, uppercase): Section dividers in navigation. Rare.
- **Mono** (400, 12px): Code snippets, credential IDs. Always in Cascadia Code.

### Named Rules
**The Tight Tracking Rule.** Headings use negative letter-spacing (-0.01em to -0.03em). Body text uses normal tracking. This creates hierarchy through density contrast, not just size.

## 4. Elevation

Resumaire is entirely flat. No box-shadows exist in the system. Depth is conveyed exclusively through tonal surface layering and 1px borders.

Three tonal layers create the spatial hierarchy:
- **Sunken** (oklch 96%): recessed, inactive, background
- **Surface** (oklch 100%): the default card/panel plane
- **Raised** (oklch 99%): hover feedback, one step forward

Borders at oklch 90% define edges. Stronger borders at oklch 82% signal interactivity (input hover, emphasis dividers). This is the entire depth vocabulary.

### Named Rules
**The No Shadow Rule.** Shadows are prohibited. If an element needs to feel elevated, use a lighter surface tone and a border. If that's not enough, the element doesn't need elevation.

## 5. Components

### Buttons
- **Shape:** Gently rounded (6px radius), compact
- **Primary:** Focused Indigo fill, near-white text, 6px 12px padding. 13px/500 weight.
- **Hover:** Darkens to oklch(44% 0.18 264). 120ms ease transition on background and border-color.
- **Secondary:** Surface fill, text-primary color, 1px border-light outline. Hover shifts to surface-sunken with border-strong.
- **Icon:** 28×28px hit target, no background at rest. Hover reveals surface-sunken fill. Used for inline actions (remove, close).
- **Text:** No background, no border. Accent-text color, 12.5px. Hover reduces opacity. Used for "Add bullet", "Add detail" actions.
- **Disabled:** 0.6 opacity, cursor not-allowed. No other visual change.

### Badges (Status)
- **Shape:** Full pill (999px radius), tiny (2px 8px padding, 11.5px text)
- **Structure:** Tinted background + matching text color + 1px tinted border + 5px dot indicator via ::before pseudo-element
- **Variants:** One per job status (saved/applied/interview/offer/rejected), each with its own hue but identical structure
- **Philosophy:** Quiet and deliberate. Status is communicated through color coding, not size or prominence.

### Skill Tags
- **Shape:** Full pill, indigo-subtle background, indigo-text color, 1px indigo border
- **Size:** 12.5px, 450 weight, 3px 10px padding
- **Remove:** Inline × button, same color, opacity 0.6 → 1 on hover

### Cards / Containers
- **Corner Style:** Generous but not bubbly (12px radius)
- **Background:** Surface (oklch 100%)
- **Border:** 1px border-light. Always present; this is how cards are defined.
- **Internal Padding:** 16px 20px for card-header and card-body
- **No shadow.** Ever.

### Inputs / Fields
- **Style:** Surface-raised background, 1px border-light, 6px radius, 8px 10px padding
- **Hover:** Border strengthens to border-strong
- **Focus:** Background shifts to surface (lighter), border becomes accent indigo, 3px ring in oklch(85% 0.06 264 / 0.35). The ring is the only place the accent "glows."
- **Error:** Border and focus ring shift to status-rejected hue
- **Textarea:** Same treatment, resize vertical, min-height 80px

### Navigation
- **Sidebar:** 220px fixed width, surface background, 1px right border. Items are 13.5px, text-secondary at rest.
- **Item hover:** Surface-sunken background, text shifts to primary. 120ms ease.
- **Item active:** Indigo-subtle background, indigo-text color, 500 weight. Icon opacity goes to 1.
- **Topbar:** 48px height, surface background, 1px bottom border. Logo + divider + meta + spacer + sign-out.
- **Filter tabs:** Underline style. 2px bottom border on active (accent color). Text shifts to accent-text. Inactive tabs are text-secondary with transparent border.
- **Responsive (≤900px):** Sidebar collapses to horizontal scrollable row below topbar.

### Lists (Job Rows, Section Rows)
- **Structure:** Vertical stack inside a bordered, rounded container. Items separated by 1px border-bottom.
- **Hover:** Background shifts to surface-raised. 100ms ease.
- **Active (section rows):** Background stays raised, chevron rotates 180°.
- **No alternating row colors.** Uniformity is the point.

## 6. Do's and Don'ts

### Do:
- **Do** use oklch for every color value. The system is built in oklch; hex approximations are for the frontmatter only.
- **Do** keep the accent to primary buttons, active nav, and status-applied. Nothing else gets indigo.
- **Do** use 120ms ease for state transitions (hover, focus, active). This is the system's tempo.
- **Do** use 1px borders as the primary spatial separator. Borders define structure here.
- **Do** use Inter's OpenType features (cv02, cv03, cv04, cv11) for disambiguated letterforms.
- **Do** keep page content under 900px max-width. The tool is dense, not wide.
- **Do** use negative letter-spacing on headings (-0.02em at 18px).
- **Do** use the skeleton pulse animation (opacity 0.55–0.85, 1.6s ease-in-out) for loading states.

### Don't:
- **Don't** add box-shadows. The system is flat by doctrine, not by accident.
- **Don't** use pure black or pure white. Every neutral is tinted at hue 250.
- **Don't** use the accent color decoratively. It means "actionable" or "active." Using it for illustration or emphasis dilutes its meaning.
- **Don't** add motion beyond state transitions. No entrance animations, no scroll-driven effects, no choreography.
- **Don't** use border-left or border-right greater than 1px as a colored accent stripe.
- **Don't** use gradient text or glassmorphism.
- **Don't** add filler copy, restated headings, or AI-sounding explanations. Every word earns its place.
- **Don't** interrupt the user's flow with modals when inline alternatives exist.
- **Don't** use cards where a simple bordered list would suffice. Cards are for grouped metadata, not individual items.
- **Don't** introduce new colors without a named role. If it doesn't have a CSS custom property, it doesn't belong.
