# Features

Each subfolder is an independent feature with its own routes, services, models, and components.

## Features List

| Folder          | BRD Section | Description                                                  |
| --------------- | ----------- | ------------------------------------------------------------ |
| `auth/`         | §1          | Authentication, OTP, RBAC, custom roles                      |
| `employees/`    | §2.2        | Employee profiles, documents, disciplinary, payroll baseline |
| `jobs/`         | §2.1        | Job roles, qualifications, success metrics, fixed tasks      |
| `sites/`        | §3.1        | Site management, geofencing, IP whitelist, SOPs              |
| `attendance/`   | §3.2        | Attendance profiles, records, check-in/out                   |
| `scheduling/`   | §4          | Schedule board, draft/publish, templates, block days         |
| `shift-market/` | §5          | Shift claims, escrow, approval workflows                     |
| `gamification/` | §6          | Points wallet, automated rules, manual transactions          |
| `rewards/`      | §7          | Reward catalog, inventory, bulk upload, redemption           |
| `departments/`  | §2          | Department management                                        |
| `dashboard/`    | §8          | Analytics: workforce, attendance, gamification, coverage     |
| `settings/`     | —           | User profile, password, company config                       |

## Convention

```
features/<feature>/
├── models/               # TypeScript interfaces, DTOs, enums
├── services/             # HTTP service (API calls only)
├── store/                # Signal store (optional)
├── <component>/          # Page/section component
│   ├── .component.ts
│   ├── .component.html
│   └── .component.css
└── <feature>.routes.ts   # Lazy-loaded routes
```

## Rules

- Each feature is lazy-loaded via `loadChildren` in `app.routes.ts`.
- Features do NOT import from each other — shared code goes in `core/` or `shared/`.
- Feature route files export `Routes` — consumed by the parent route config.
