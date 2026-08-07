# Core

Singleton services, guards, interceptors, and shared models that run app-wide.

## Contents

| Folder          | Purpose                                                                             |
| --------------- | ----------------------------------------------------------------------------------- |
| `auth/`         | `AuthService`, `TokenService` — login/logout, JWT storage, token refresh            |
| `guards/`       | Route guards: `AuthGuard` (login required), `RoleGuard` (role-based access)         |
| `interceptors/` | HTTP interceptors: attach Bearer token, handle 401 → redirect to login              |
| `services/`     | Global singletons: `NotificationService` (toasts), `LoadingService` (spinner state) |
| `models/`       | Shared TypeScript interfaces and types used across multiple features                |

## Rules

- Services here are **providedIn: 'root'** — singletons.
- No UI components — use `shared/` for reusable UI.
- No feature-specific logic — keep that in the feature folder.
- Interceptors are registered in `app.config.ts` via `withInterceptors(...)`.
