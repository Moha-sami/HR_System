---
name: HR System
description: Bilingual internal HR operations tool with a strict utilitarian interface.
colors:
  brand: "#234199"
  brand-deep: "#122358"
  brand-tint: "#d9e1f2"
  brand-wash: "rgba(35, 65, 153, 0.1)"
  ink: "#111827"
  body-text: "#374151"
  muted: "#6b7280"
  faint: "#9ca3af"
  border: "#e5e7eb"
  wash: "#f9fafb"
  surface: "#ffffff"
  danger: "#ec1d27"
  danger-text: "#eb0000"
  danger-wash: "rgba(255, 0, 0, 0.06)"
  success: "#289e45"
  warning: "#f5b800"
typography:
  display:
    fontSize: "1.5rem"
    fontWeight: 600
    lineHeight: 2rem
  title:
    fontSize: "0.875rem"
    fontWeight: 500
    lineHeight: 1.25rem
  body:
    fontSize: "0.875rem"
    fontWeight: 400
    lineHeight: 1.25rem
  label:
    fontSize: "0.75rem"
    fontWeight: 400
    lineHeight: 1rem
rounded:
  sm: "4px"
  md: "8px"
  lg: "12px"
  xl: "16px"
  full: "9999px"
spacing:
  xs: "4px"
  sm: "8px"
  md: "16px"
  lg: "24px"
  xl: "32px"
components:
  button-primary:
    backgroundColor: "{colors.brand}"
    textColor: "{colors.surface}"
    rounded: "{rounded.lg}"
    padding: "8px 16px"
  button-primary-hover:
    backgroundColor: "{colors.brand-deep}"
    textColor: "{colors.surface}"
    rounded: "{rounded.lg}"
    padding: "8px 16px"
  button-secondary:
    backgroundColor: "#e8e8e8"
    textColor: "{colors.body-text}"
    rounded: "{rounded.lg}"
    padding: "8px 16px"
  button-danger:
    backgroundColor: "{colors.danger}"
    textColor: "{colors.surface}"
    rounded: "{rounded.lg}"
    padding: "8px 16px"
  button-ghost:
    backgroundColor: "transparent"
    textColor: "{colors.brand}"
    rounded: "{rounded.lg}"
    padding: "8px 16px"
  input-field:
    backgroundColor: "{colors.wash}"
    textColor: "{colors.ink}"
    rounded: "{rounded.lg}"
    padding: "10px 16px"
  chip-info:
    backgroundColor: "{colors.brand-wash}"
    textColor: "{colors.brand}"
    rounded: "{rounded.sm}"
    padding: "2px 8px"
---

# Design System: HR System

## Overview

**Creative North Star: "The Well Kept Ledger"**

Every screen is a ledger page: ruled lines, exact columns, nothing decorative that does not earn its place. The system is strictly utilitarian in mood — calm neutrals carry the content, a single institutional blue marks action and identity, and color appears only where it changes a decision (danger, success, warning, risk). Density is high but ordered; forms, tables, and timelines share one spacing rhythm and one corner language so any new screen feels filed in the same book.

**Key Characteristics:**
- One accent (Brand Blue) on a neutral paper system.
- Flat surfaces; depth only on floating layers.
- Rounded-lg containers, full-round pills for status.
- Bilingual EN/AR with mirrored layout; no direction-specific styling.

## Colors

A single-accent ledger palette: Brand Blue does all structural work, neutrals carry content, and semantic colors appear only at decision points.

### Primary
- **Brand Blue** (#234199): navigation, primary buttons, table headers, links, focus rings, active states. The only hue permitted for chrome.

### Neutral
- **Ink** (#111827): headings and primary text.
- **Body Text** (#374151): secondary text and labels.
- **Muted** (#6b7280): hints, placeholders, tertiary text.
- **Border** (#e5e7eb): dividers, card and input borders.
- **Wash** (#f9fafb): page and input backgrounds.
- **Surface** (#ffffff): cards, modals, dropdowns.

### Named Rules
**The One Accent Rule.** Brand Blue is the only non-semantic hue on any screen. Everything else is neutral or semantic.
**The Decision Color Rule.** Red, green, and amber appear only where the user must decide or be warned: destructive actions, errors, success feedback, risk badges.

## Typography

**Display Font:** system sans default (no custom family; Arabic and Latin share the stack).
**Body Font:** system sans default.

**Character:** ledger clerical — medium weights for structure, regular for content, small sizes throughout. No display typography; the largest text on a screen is the page title (24px semibold).

### Hierarchy
- **Display** (600, 24px/32px): page titles only.
- **Title** (500, 14px/20px): section headings, card titles.
- **Body** (400, 14px/20px): form values, table cells, paragraphs.
- **Label** (400, 12px/16px): field labels, hints, badges, table meta.

## Layout

Single-column work surfaces under a persistent sidebar and breadcrumb. Content sits in bordered cards (rounded-lg, neutral border) with 24px page padding and 16px internal rhythm. Tables use full-width rows with 16px horizontal and 12px vertical cell padding; header text is Brand Blue. Forms are label-left rows (112px label column) collapsing to stacked on narrow screens. Spacing scale: 4 / 8 / 16 / 24 / 32px. RTL mirrors via logical properties; never use physical left/right positioning.

## Elevation & Depth

Flat by default. Surfaces carry no shadow; separation comes from 1px neutral borders and tonal washes (page wash vs. white cards).

### Shadow Vocabulary
- **Overlay lift** (Tailwind shadow-lg): dropdown menus, modals, drag previews — floating layers only.

### Named Rules
**The Flat-By-Default Rule.** Shadows appear only on floating layers. If it is in the page flow, it is flat.

## Shapes

Gently rounded containers (12px radius) throughout: cards, inputs, dropdowns, buttons, timeline bars. Status and badges go fully round (pill). Icon-only actions are square with 4-8px rounding. Borders are 1px neutral; focus replaces the border color with Brand Blue plus a matching ring, never a glow.

## Components

### Buttons
Character: quiet and rectangular with soft 12px corners.
- **Shape:** rounded-lg (12px); circle variant fully round for icon buttons.
- **Primary:** Brand Blue fill, white text, 8px 16px padding; hover deepens toward Brand Deep.
- **Hover / Focus:** darker fill on hover; visible Brand Blue focus ring on keyboard focus; 50% opacity when disabled.
- **Secondary:** light neutral fill with dark text. **Danger:** red fill for destructive confirms. **Ghost:** transparent with Brand Blue text for tertiary actions.
- **Sizes:** small (12px 6px, 14px text), medium (16px 8px, 16px text), large (24px 12px, 18px text).

### Chips
- **Style:** tinted wash background, colored text, 4px radius, no border (e.g. blue wash with Brand Blue text for roles; amber/green/red washes for risk badges).
- **State:** static labels; filter chips toggle between wash (off) and filled (on).

### Cards / Containers
- **Corner Style:** rounded-lg (12px).
- **Background:** white on page wash, or wash on white for nested sections.
- **Shadow Strategy:** flat; see Elevation.
- **Border:** 1px neutral-200.
- **Internal Padding:** 16-24px.

### Inputs / Fields
- **Style:** wash background, 1px neutral border, rounded-lg, 10px 16px padding.
- **Focus:** border shifts to Brand Blue with matching ring.
- **Error / Disabled:** red border plus red message text for errors; reduced opacity when disabled.

### Navigation
- **Sidebar:** dark Brand Blue rail, white labels, icon plus text rows; active item highlighted; groups expand in place. Top bar holds user, language switcher, and logout.
- **Breadcrumb:** text links with chevron separators ending in the current page name.

## Do's and Don'ts

### Do:
- **Do** use Brand Blue for every interactive or structural accent (The One Accent Rule).
- **Do** keep flow surfaces flat and reserve shadow-lg for floating layers (The Flat-By-Default Rule).
- **Do** mirror with logical properties so Arabic layouts work without special cases.
- **Do** write every user-facing string as an EN/AR i18n key.

### Don't:
- **Don't** introduce a new hue outside the semantic set.
- **Don't** use custom CSS per component; style with Tailwind utilities and the theme tokens.
- **Don't** put physical left/right positioning or direction-specific margins in new code.
- **Don't** invent display typography; page titles stay 24px semibold.
