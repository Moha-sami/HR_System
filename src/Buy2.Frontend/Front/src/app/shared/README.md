# Shared

Reusable UI components, directives, and pipes available to all features.

## Contents

| Folder        | Purpose                                                                                                 |
| ------------- | ------------------------------------------------------------------------------------------------------- |
| `components/` | Generic UI: `ButtonComponent`, `ModalComponent`, `TableComponent`, `ToastComponent`, `SpinnerComponent` |
| `directives/` | Attribute/structural directives: `ClickOutsideDirective`, `HasRoleDirective`                            |
| `pipes/`      | Display pipes: `TimeAgoPipe`, `CurrencyPipe` (custom formatting)                                        |

## Rules

- Components here must be **stateless or dumb** — no HTTP calls, no service injection beyond inputs/outputs.
- If a component is feature-specific, put it in that feature's folder instead.
- Export via `standalone` — import directly where needed, no barrel exports unless shared widely.
- Keep styling scoped — use Tailwind utilities, avoid global CSS.
