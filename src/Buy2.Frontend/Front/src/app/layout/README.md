# Layout

Shell layout components — the persistent frame around all authenticated pages.

## Contents

| Component             | Purpose                                                          |
| --------------------- | ---------------------------------------------------------------- |
| `sidebar/`            | Collapsible sidebar with navigation links, role-based menu items |
| `header/`             | Top bar: user avatar, notifications bell, profile dropdown       |
| `footer/`             | Optional footer with copyright, links                            |
| `layout.component.ts` | Shell component — wraps `<router-outlet>` with sidebar + header  |

## How It Works

```
┌──────────┬──────────────────────────┐
│          │        Header            │
│  Sidebar ├──────────────────────────┤
│          │                          │
│          │     <router-outlet>      │
│          │     (page content)       │
│          │                          │
└──────────┴──────────────────────────┘
```

## Rules

- Layout is rendered by the root route in `app.routes.ts` — authenticated routes are children.
- Sidebar menu items come from a config array (not hardcoded in template).
- `layout.component.ts` handles sidebar open/close state (signal-based).
