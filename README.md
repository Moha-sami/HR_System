# HR Management System (HRMS)

[![CI/CD Build Status](https://github.com/Moha-sami/HR_system/actions/workflows/ci.yml/badge.svg)](https://github.com/Moha-sami/HR_system/actions)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-blue.svg)](https://github.com/Moha-sami/HR_system)
[![Backend](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Frontend](https://img.shields.io/badge/Angular-18+-red.svg)](https://angular.dev/)
[![Jira Space](https://img.shields.io/badge/Jira-SCRUM-0052CC.svg)](https://buy2-hrms.atlassian.net)
[![GitHub Contributors](https://img.shields.io/github/contributors/Moha-sami/HR_system.svg?style=flat-square)](https://github.com/Moha-sami/HR_system/graphs/contributors)

An enterprise-grade open-source HR Management System built with **.NET 10 Clean Architecture** and **Angular 18+**. Buy2 HRMS features advanced shift scheduling engines, geofenced clock-in attendance, gamification points ledgers, and digital reward voucher stores.

---

## 📌 Project Milestones & Module Status

Sprint 1 is actively underway using an atomic, step-by-step contribution model.

| Feature Module | Technical Components | Status |
| :--- | :--- | :--- |
| **Identity & Access Management (IAM)** | `BaseEntity`, `Role` Entity, Role DTOs, Permissions Json | ✅ Completed |
| **Workforce & Job Roles** | `JobRole`, `Employee` Entities & DTOs | ✅ Completed |
| **Sites & Geofencing** | `Site` Entity, Coordinate bounds, MAC whitelist DTOs | ✅ Completed |
| **Attendance & Roster** | `AttendanceProfile`, `Shift`, `ShiftClaim` Entities, Enums & DTOs | ✅ Completed |
| **Points & Gamification** | `PointsRule`, `RewardItem` Entities & DTOs | ✅ Completed |
| **Application Layer Contracts** | `IRepository<T>`, `IUnitOfWork`, `IJwtTokenGenerator`, `IScheduleValidationEngine` | ✅ Completed |
| **Frontend Foundation** | Angular 18+ App Setup, TypeScript Data Models, Common Components | ✅ Completed |

---

## 🏗️ Architecture & Project Structure

The codebase strictly adheres to **Clean Architecture** principles to ensure decoupled dependencies and maximum testability:

```
HR_system/
├── src/
│   ├── Buy2.Domain/           # Entities, Enums, Value Objects, Navigation Properties
│   ├── Buy2.Application/      # DTOs, Application Interfaces, CQRS Contracts
│   ├── Buy2.Infrastructure/   # EF Core DbContext, Fluent API Configurations, Repositories, JWT
│   ├── Buy2.Api/              # REST API Controllers, Middleware, DI Container Setup
│   └── Buy2.Frontend/         # Angular 18 SPA (Signals, Standalone Components, Material UI)
├── docs/                      # ERD Diagrams, System Specs, Jira Import Maps
└── AVAILABLE_TASKS.md         # Master Contributor Task Backlog
```

---

## 🗄️ Database Architecture (ERD)

### Entity Relationship Diagram Overview
![High-Level ERD Overview](./docs/clean_white_erd_diagram.jpg)

*Detailed relationship documentation available at [`docs/ERD.md`](./docs/ERD.md).*

---

## 📋 Jira Board & Automation Workflow

The project uses Atlassian Jira (**Space**: `Buy2 HRMS`, **Key**: `SCRUM`) integrated with GitHub Actions.

### Automation & Protection Rules:
1. **Branch & PR Naming**: Branch titles must include Jira Key (e.g. `SCRUM-101-create-employee-document-entity`).
2. **Auto-Transition to Done**: Merging a PR into `main` automatically transitions linked Jira cards to **Done**.
3. **CI/CD Build Checks**: Every PR triggers automated `.NET` builds and lint checks via GitHub Actions.

---

## 🚀 How to Contribute

We welcome team members and open-source contributors!

1. Open **[`AVAILABLE_TASKS.md`](./AVAILABLE_TASKS.md)** to view available tasks.
2. Pick an unassigned task card from the **Jira Sprint Board**.
3. Create a working git branch named after your Jira key (e.g., `SCRUM-101-create-employee-document-entity`).
4. Submit a Pull Request targeting `main`. Once merged, Jira auto-updates your task to **Done**!

---

## 💻 Local Setup & Development

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Node.js 20+](https://nodejs.org/) & Angular CLI (`npm i -g @angular/cli`)
- Git

### Build & Run
```bash
# Clone repository
git clone https://github.com/Moha-sami/HR_system.git
cd HR_system

# Build .NET Solution
dotnet build HR_system.slnx

# Run API Backend
cd src/Buy2.Api
dotnet run

# Run Angular Frontend (in separate terminal)
cd src/Buy2.Frontend/Front
npm install
ng serve
```

---

## 🔗 Key Links & References
- **GitHub Repository**: [https://github.com/Moha-sami/HR_system.git](https://github.com/Moha-sami/HR_system.git)
- **Figma UI/UX Design**: [BUY2 HRMS Figma Design](https://www.figma.com/design/JQ67DCkObzVjER8Safb5sw/BUY2-Junk-File?node-id=882-3040&p=f&t=GxfjGebSZZA9X7B0-0)
- **Jira Board**: `buy2-hrms.atlassian.net` (Key: `SCRUM`)

---

## 👥 Contributors

Thanks goes to all our amazing contributors!

[![GitHub Contributors](https://img.shields.io/github/contributors/Moha-sami/HR_system.svg?style=flat-square)](https://github.com/Moha-sami/HR_system/graphs/contributors)

*Want to contribute? Check out [`AVAILABLE_TASKS.md`](./AVAILABLE_TASKS.md) to pick an open task!*

