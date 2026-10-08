# HR Management System (HRMS)

[![CI/CD Build Status](https://github.com/Moha-sami/HR_system/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Moha-sami/HR_system/actions)
[![Unit Tests](https://img.shields.io/badge/Tests-1032%20Passing-brightgreen.svg)](https://github.com/Moha-sami/HR_system)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-blue.svg)](https://github.com/Moha-sami/HR_system)
[![Backend](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Frontend](https://img.shields.io/badge/Angular-18+-red.svg)](https://angular.dev/)
[![Jira Space](https://img.shields.io/badge/Jira-SCRUM-0052CC.svg)](https://buy2-hrms.atlassian.net)
[![GitHub Contributors](https://img.shields.io/github/contributors/Moha-sami/HR_system.svg?style=flat-square)](https://github.com/Moha-sami/HR_system/graphs/contributors)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](./LICENSE)

An enterprise-grade open-source HR Management System built with **.NET 10 Clean Architecture** and **Angular 18+ (Signals & Standalone Components)**. Buy2 HRMS provides advanced multi-branch site management, role-based access control (RBAC), intelligent shift scheduling, open shift market, automated points ledger & rewards voucher store, employee request approvals, and corporate social engagement with news feeds, threaded discussions, emoji reactions, and peer recognitions.

---

## 📌 Project Milestones & Module Status

| Feature Module | Backend Status | Frontend Status | Test Coverage | Key Capabilities |
| :--- | :---: | :---: | :---: | :--- |
| **Domain & Persistence Foundation** | ✅ Done | N/A | 100% | 25+ Entities, EF Core 10, UoW, Generic Repositories, Migrations |
| **Authentication & Identity** | ✅ Done | ✅ Done | 100% | JWT Bearer, Self-Service Password Reset, Role Claims |
| **Employee Directory & Onboarding** | ✅ Done | ✅ Done | 100% | Single/Bulk Onboard, Multi-Column Filter/Sort, CSV Export (UTF-8 BOM) |
| **Employee 360° Profile & Tabs** | ✅ Done | ✅ Done | 100% | Personal, Job, Documents, Payroll (`EmployeeSite` sync), Disciplinary Violations |
| **Performance & Attendance** | ✅ Done | ✅ Done | 100% | Weighted Scores, Rating Labels, Monthly Calendar, Punctuality Tracking |
| **Role Management & RBAC** | ✅ Done | ✅ Done | 100% | Granular Permission Matrix, Safe Delete with Employee Reassignment |
| **Site & Branch Management** | ✅ Done | ✅ Done | 100% | GPS Geofencing, Radius Bounds, MAC Whitelisting, Operating Hours, SOPs |
| **Job Roles & Organization Catalog**| ✅ Done | ✅ Done | 100% | Job Specifications, Department & Qualification Catalogs, Rosters |
| **Shift Templates & Scheduling Engine**| ✅ Done | ✅ Done | 100% | Shift Templates, Pre-Flight Validation (Overlap/Hours/Skills), Atomic Publish |
| **Shift Market & Open Claims** | ✅ Done | ✅ Done | 100% | Open Shifts Board, Candidate Matching Engine, Claim Approval Desk |
| **Points Ledger & Gamification** | ✅ Done | ✅ Done | 100% | Double-Entry Points Ledger, Automation Trigger Rules, Manual Adjustments |
| **Rewards Catalog & Voucher Store** | ✅ Done | ✅ Done | 100% | Multi-Category Store, Excel/CSV Batch Voucher Import, Atomic Redemption |
| **Employee Requests & Approvals** | ✅ Done | ✅ Done | 100% | Request Types Catalog, Leave/Asset Forms, Manager Review Desk, Withdrawal |
| **News Posts & Lifecycle Management**| ✅ Done | ✅ Done | 100% | Draft/Scheduled/Published Lifecycle, Media Uploads, Soft Delete Cascade |
| **Social Engagement & Comments** | ✅ Done | ✅ Done | 100% | Threaded Hierarchies, Nested Replies, Moderation Tombstones |
| **Emoji Reactions Engine** | ✅ Done | ✅ Done | 100% | Mutually Exclusive Reactions (Like, Dislike, Laugh, Wow, Heart, Angry) |
| **Peer Recognitions & Points Grant**| ✅ Done | ✅ Done | 100% | Peer Shoutouts, Points Grant Engine, Safe Deletion with Points Reversal |

---

## 🏗️ Architecture & Clean Design Principles

The system strictly adheres to **Clean Architecture** and **CQRS (Command Query Responsibility Segregation)** using **MediatR**:

```
HR_system/
├── src/
│   ├── Buy2.Domain/           # Enterprise Entities, Domain Enums, Entity Configurations
│   ├── Buy2.Application/      # CQRS Commands, Queries, Handlers, FluentValidation, DTOs
│   ├── Buy2.Infrastructure/   # EF Core DbContext, Repositories, Unit of Work, JWT, Migrations
│   ├── Buy2.Api/              # ASP.NET Core 10 Web API, Controllers, Middleware, Swagger Spec
│   └── Buy2.Frontend/         # Angular 18 Single Page Application (Front/)
│       └── Front/src/app/
│           ├── core/          # Authentication Guards, HTTP Interceptors, Base Services
│           ├── features/      # Standalone Feature Modules (Employees, Shifts, News, etc.)
│           └── shared/        # Reusable UI Components, Pipes, Directives, Tailwind Layouts
├── tests/
│   └── Buy2.Domain.Tests/     # 1,032 Comprehensive Unit & Integration Tests (xUnit, InMemory EF)
├── docs/                      # Architectural Documentation, ERD Diagrams, Jira Task Specs
│   ├── ERD_v2.png             # Full 25-Entity Relationship Diagram
│   ├── API_ENDPOINTS.md       # Comprehensive REST API Specification (1,150+ lines)
│   └── jira/                  # Jira Import CSVs and Functional Task Specifications
```

### Key Architectural Guidelines Enforced:
1. **CQRS Master Orchestrator Pattern**: Multi-step workflows coordinate sub-queries and sub-commands through MediatR; sub-handlers never trigger `SaveChangesAsync` directly to ensure single atomic transactions.
2. **Read-Side Optimization**: Read queries strictly leverage `.AsNoTracking()` and database-level projections to optimize throughput and memory footprints.
3. **Database Concurrency & Integrity**: Explicit soft-deletion query filters (`HasQueryFilter(e => !e.IsDeleted)`), unique compound indexes, and cascade restrictions prevent orphaned foreign records.
4. **Resilient Rollbacks**: Atomic transactions pass `CancellationToken.None` during catch-block rollbacks to guarantee completion even under client-aborted HTTP requests.

---

## 🚀 REST API Overview

Below is a consolidated summary of the **29 controller endpoints** exposed by `Buy2.Api`:

### 1. Authentication & Security (`/api/v1/auth`)
| Method | Route | Description |
| :--- | :--- | :--- |
| `POST` | `/api/v1/auth/login` | Authenticate user, issue JWT Bearer token, and return employee session profile |
| `POST` | `/api/v1/auth/password/reset` | Self-service password reset workflow via email OTP |

### 2. Employee Directory & Profiles (`/api/v1/employees`)
| Method | Route | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/employees` | Paginated employee directory with multi-field search, sorting, and department/region filters |
| `GET` | `/api/v1/employees/export` | Export filtered employee roster to CSV (UTF-8 BOM for Microsoft Excel) |
| `POST` | `/api/v1/employees/onboard` | Onboard single employee with initial role and site allocation |
| `POST` | `/api/v1/employees/bulk-onboard` | High-throughput batch onboarding with per-item partial success reporting |
| `GET` | `/api/v1/employees/{id}` | Complete 360° profile (personal info, job details, live stats, gamification) |
| `PUT` | `/api/v1/employees/{id}/personal` | Update employee personal data, national ID, emergency contacts |
| `PUT` | `/api/v1/employees/{id}/job` | Update job role, direct manager, seniority, attendance mode |
| `GET` | `/api/v1/employees/{id}/payroll` | Retrieve employee payroll setup, work week hours, and overtime rates |
| `PUT` | `/api/v1/employees/{id}/payroll` | Upsert payroll parameters and atomically synchronize `EmployeeSite` junction records |
| `DELETE` | `/api/v1/employees/{id}` | Soft delete employee record while preserving historical audit trails |
| `GET` | `/api/v1/employees/{id}/documents` | Retrieve uploaded compliance documents (IDs, contracts, medical records) |
| `POST` | `/api/v1/employees/{id}/documents` | Upload employee compliance document metadata |
| `DELETE` | `/api/v1/employees/{id}/documents/{docId}` | Delete employee document record |
| `POST` | `/api/v1/employees/{id}/violations` | Log disciplinary violation with severity rating and notes |
| `GET` | `/api/v1/employees/{id}/performance/overview` | Weighted performance score, ratings, and task metrics |
| `GET` | `/api/v1/employees/{id}/attendance/calendar` | Monthly attendance calendar, punctuality scores, and lateness statistics |

### 3. Role & Permission Management (`/api/v1/roles`)
| Method | Route | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/roles` | List all system and custom roles with permission configurations |
| `GET` | `/api/v1/roles/{id}` | Get detailed role information and granular permission matrix |
| `POST` | `/api/v1/roles` | Create custom role with permissions JSON |
| `PUT` | `/api/v1/roles/{id}` | Update role details and modify permission scopes |
| `DELETE` | `/api/v1/roles/{id}` | Soft delete role (safeguarded against roles with assigned users) |
| `POST` | `/api/v1/roles/{id}/reassign-and-delete`| Reassign assigned employees to an alternative role and delete obsolete role |

### 4. Sites & Branch Management (`/api/v1/sites`)
| Method | Route | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/sites` | List branch sites with GPS coordinates and region filters |
| `GET` | `/api/v1/sites/{id}` | Detailed site configuration, geofence radius, and MAC whitelists |
| `POST` | `/api/v1/sites` | Register new branch site with coordinates and operating hours |
| `PUT` | `/api/v1/sites/{id}` | Update branch site configuration and network parameters |
| `DELETE` | `/api/v1/sites/{id}` | Soft delete branch site |
| `GET` | `/api/v1/sites/regions` | Retrieve active region lookup catalog |
| `POST` | `/api/v1/sites/regions` | Create region inline with uniqueness validation |
| `GET` | `/api/v1/sites/{id}/employees` | Query roster of primary and secondary assigned staff |
| `POST` | `/api/v1/sites/{id}/documents` | Upload site-specific SOPs and guidelines |

### 5. Job Roles & Organization Lookups (`/api/v1/jobs`, `/departments`, `/qualifications`)
| Method | Route | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/jobs` | Paginated job catalog with department and work model filters |
| `GET` | `/api/v1/jobs/{id}` | Full job role specification, required qualifications, and headcounts |
| `POST` | `/api/v1/jobs` | Create new job role with qualification requirements |
| `PUT` | `/api/v1/jobs/{id}` | Update job role details |
| `DELETE` | `/api/v1/jobs/{id}` | Soft delete job role with employee reassignment safeguards |
| `GET` | `/api/v1/jobs/{id}/employees` | Query list of employees currently holding the job role |
| `GET` | `/api/v1/departments` | Lookup list of all organization departments |
| `POST` | `/api/v1/departments` | Create organization department with case-insensitive uniqueness check |
| `GET` | `/api/v1/qualifications` | Lookup list of qualifications and certifications |
| `POST` | `/api/v1/qualifications` | Register new qualification inline |

### 6. Shift Scheduling & Open Market (`/api/v1/shift-templates`, `/schedules`, `/shift-market`)
| Method | Route | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/shift-templates` | List reusable shift templates (start/end times, breaks, roles) |
| `POST` | `/api/v1/shift-templates` | Create new shift template |
| `PUT` | `/api/v1/shift-templates/{id}` | Update shift template parameters |
| `DELETE` | `/api/v1/shift-templates/{id}` | Archive shift template |
| `GET` | `/api/v1/schedules/overview` | Query scheduling grid for calendar period across sites/roles |
| `POST` | `/api/v1/schedules/validate-draft` | Pre-flight validation checking double-booking, rest hours, and skills |
| `POST` | `/api/v1/schedules/publish` | Atomically publish shift schedule drafts with employee notifications |
| `GET` | `/api/v1/shifts/candidates` | Candidate ranking engine identifying qualified, unassigned staff |
| `GET` | `/api/v1/shift-market/open-shifts` | Browse open unassigned shifts available for peer claiming |
| `POST` | `/api/v1/shift-market/claims/{shiftId}` | Submit shift claim request |
| `POST` | `/api/v1/shift-market/claims/{claimId}/approve` | Manager approves claim, binds shift, and cancels competing claims |
| `POST` | `/api/v1/shift-market/claims/{claimId}/reject` | Reject claim with manager feedback |

### 7. Gamification Points Ledger & Rules (`/api/v1/points`)
| Method | Route | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/employees/{id}/points/summary` | Current wallet balance, lifetime earned points, and redeemed rewards count |
| `GET` | `/api/v1/employees/{id}/points/transactions` | Paginated transaction ledger with audit rules and timestamps |
| `POST` | `/api/v1/points/adjust` | Manual point adjustment (bonus or deduction) with mandatory audit note |
| `GET` | `/api/v1/points/rules` | Directory of automated point trigger rules |
| `POST` | `/api/v1/points/rules` | Create automated point trigger rule (punctuality, tasks, milestones) |
| `PUT` | `/api/v1/points/rules/{id}` | Update automation rule criteria |
| `DELETE` | `/api/v1/points/rules/{id}` | Archive point automation rule |

### 8. Rewards Catalog & Digital Vouchers (`/api/v1/rewards`)
| Method | Route | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/rewards` | Browse rewards catalog with stock indicators and points cost |
| `GET` | `/api/v1/rewards/{id}` | Detailed reward item view with description and voucher stock |
| `POST` | `/api/v1/rewards` | Create new reward catalog item |
| `PUT` | `/api/v1/rewards/{id}` | Update reward details, points price, or active status |
| `DELETE` | `/api/v1/rewards/{id}` | Soft delete reward item |
| `POST` | `/api/v1/rewards/{id}/vouchers/batch` | Bulk import unique digital voucher codes via Excel / CSV |
| `POST` | `/api/v1/rewards/{id}/redeem` | Atomic redemption: deduct points, assign unique voucher code, issue receipt |

### 9. Employee Requests & Approvals (`/api/v1/requests`, `/request-types`)
| Method | Route | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/request-types` | Directory of request types with policy rules (leave pay, required dates) |
| `POST` | `/api/v1/request-types` | Create request type schema |
| `PUT` | `/api/v1/request-types/{id}` | Update request type configuration |
| `DELETE` | `/api/v1/request-types/{id}` | Delete request type (blocked if referenced by employee submissions) |
| `GET` | `/api/v1/requests` | Paginated employee request submissions with status and date filters |
| `POST` | `/api/v1/requests` | Submit formal request with dates, reason, and attachment |
| `GET` | `/api/v1/requests/{id}` | Detailed request with multi-stage approval workflow audit trail |
| `PUT` | `/api/v1/requests/{id}/status` | Manager approval desk (Approve, Reject, Cancel) with review comment |
| `DELETE` | `/api/v1/requests/{id}` | Requester cancellation / withdrawal |

### 10. News Feed, Social Engagement & Recognitions (`/api/v1/news`, `/recognitions`)
| Method | Route | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/news` | Role-based news feed with status filters, engagement stats, and search |
| `POST` | `/api/v1/news` | Create post with multipart media, draft/scheduled/published status |
| `GET` | `/api/v1/news/{id}` | Detailed post with reaction breakdown and caller active reaction |
| `PUT` | `/api/v1/news/{id}` | Update post content, scheduled date, and lifecycle state |
| `DELETE` | `/api/v1/news/{id}` | Soft delete post with cascade comment suppression and auditability |
| `GET` | `/api/v1/news/{postId}/comments` | Query threaded comments and nested sub-replies hierarchy |
| `POST` | `/api/v1/news/{postId}/comments` | Post top-level comment or threaded reply with parent attribution |
| `PUT` | `/api/v1/news/comments/{commentId}` | Author updates comment text |
| `DELETE` | `/api/v1/news/comments/{commentId}` | Author soft deletion or admin moderation tombstone masking |
| `POST` | `/api/v1/news/{targetType}/{targetId}/reactions` | Toggle emoji reaction (mutually exclusive per user: Like, Dislike, Laugh, Wow, Heart, Angry) |
| `GET` | `/api/v1/recognitions` | Query peer recognitions directory with points tally and sorting |
| `POST` | `/api/v1/recognitions` | Create peer recognition with recipient and optional points grant |
| `GET` | `/api/v1/recognitions/{id}` | Detailed recognition view with recipient profile and audit trail |
| `PUT` | `/api/v1/recognitions/{id}` | Update recognition content, scheduling, and points award |
| `DELETE` | `/api/v1/recognitions/{id}` | Delete recognition with automated points grant reversal safeguard |

---

## 🎨 Frontend Architecture (Angular 18+)

The frontend is located at `src/Buy2.Frontend/Front` and is designed for enterprise responsiveness, accessibility, and high performance:

- **Framework**: Angular 18+ with **Standalone Components** and native **Signals** for reactive state management.
- **Styling**: Tailwind CSS combined with Angular Material components.
- **Internationalization (i18n)**: Full English (`en`) and Arabic (`ar`) localization with dynamic RTL (Right-to-Left) layout switching.
- **Architecture**: Modular feature directories with smart/dumb component segregation, dedicated HTTP services, and strongly-typed TypeScript models matching backend DTOs.

---

## 🧪 Testing & Quality Assurance

The solution enforces a rigorous testing regimen to ensure zero regressions across critical business workflows:

```bash
# Execute the full automated test suite
dotnet test
```

- **Total Automated Tests**: **1,032 Passing Tests** across all application and domain features.
- **Persistence Testing**: Utilizes EF Core In-Memory database providers and schema synchronization guards (`HasPendingModelChanges` check) to guarantee migrations match domain entities.
- **Mutation Testing**: Configured with **Stryker.NET** for mutation testing analysis.
- **Code Standards**: Strict adherence to Karpathy AI coding rules, surgical modifications, and CQRS orchestrator invariants.

---

## 💻 Local Setup & Development

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Node.js 20+](https://nodejs.org/) & Angular CLI (`npm i -g @angular/cli`)
- [SQL Server](https://www.microsoft.com/sql-server) or Docker for database persistence
- Git

### Getting Started

```bash
# 1. Clone repository
git clone https://github.com/Moha-sami/HR_system.git
cd HR_system

# 2. Build the solution
dotnet build HR_system.slnx

# 3. Run the automated test suite
dotnet test

# 4. Start the API Backend
cd src/Buy2.Api
dotnet run

# 5. Start the Angular Frontend (in separate terminal)
cd ../Buy2.Frontend/Front
npm install
ng serve
```

Once running:
- **API Swagger UI**: `https://localhost:7136/swagger` (or configured launch URL)
- **Frontend App**: `http://localhost:4200`

---

## 🔗 Key Links & References
- **GitHub Repository**: [https://github.com/Moha-sami/HR_system.git](https://github.com/Moha-sami/HR_system.git)
- **Live API (Swagger)**: [https://hr-system-api.runasp.net](https://hr-system-api.runasp.net)
- **Figma UI/UX Design**: [BUY2 HRMS Figma Design](https://www.figma.com/design/JQ67DCkObzVjER8Safb5sw/BUY2-Junk-File?node-id=882-3040&p=f&t=GxfjGebSZZA9X7B0-0)
- **Jira Board**: `buy2-hrms.atlassian.net` (Key: `SCRUM`)
- **API Endpoints Reference**: [`docs/API_ENDPOINTS.md`](./docs/API_ENDPOINTS.md)
- **Entity Relationship Model**: [`docs/ERD.md`](./docs/ERD.md)

---

## 📄 License

This project is licensed under the **Apache License 2.0**. See the [`LICENSE`](./LICENSE) file for details.
