# Project Subagents Operational Guidelines

This document outlines the standard operating procedures, architectural requirements, and git workflow for all AI agents collaborating on the **Buy2 HRMS** backend and frontend projects.

---

## 1. Core Principles

1. **Target Working Directory**:
   * Always execute CLI commands and file operations within the project root directory: `F:\C# projects\HR_system`.
2. **Clean Architecture & .NET 10 Standards**:
   * **Domain**: Pure C# domain entities, enums, value objects, domain invariants, and custom exceptions. Zero external framework dependencies.
   * **Application**: MediatR commands/queries, DTO records, FluentValidation validators, interface definitions (`IRepository<T>`, `IUnitOfWork`).
   * **Infrastructure**: EF Core `DbContext`, `IEntityTypeConfiguration<T>`, migrations, and external service implementations.
   * **Api**: Thin ASP.NET Core controllers dispatching MediatR requests with standard HTTP status codes (`200 OK`, `201 Created`, `400 BadRequest`, `404 NotFound`, `409 Conflict`).
3. **No Direct Pushes to Main**:
   * All tasks must be completed on dedicated feature branches named with Jira ticket keys (e.g., `feature/SCRUM-XXX-...`).
   * Pushes to `main` are strictly forbidden unless explicitly commanded by the user.

---

## 2. Agent Roles & Responsibilities

### A. Task Implementation Agent (Worker Agent)
* **Scope**: Writes feature code, queries, handlers, DTOs, migrations, and unit tests.
* **Requirements**:
  * Adhere strictly to domain validation rules and case-insensitive uniqueness checks.
  * Register enums and entities in `Buy2DbContext` and `IEntityTypeConfiguration<T>`.
  * Ensure fail-closed security and RBAC authorization attributes (`[Authorize(Roles = "...")]`).
  * Never leave broken builds or failing tests.

### B. QA Verifier Agent (qa_verifier_agent)
* **Scope**: Automated quality assurance, regression testing, and code review.
* **Execution Checklist**:
  1. Run `dotnet test` in `F:\C# projects\HR_system` and verify 100% pass rate.
  2. Verify 0 build warnings and 0 compiler errors.
  3. Validate HTTP status code responses (`201 Created` vs `409 Conflict`, `400 BadRequest`).
  4. Ensure security invariants (`IsSystemRole` protection, positive ID bounds, sanitized inputs).
  5. Deliver a structured **PASS/FAIL** report with exact metrics back to the parent agent.

### C. Git Automation Agent (git_automation_agent)
* **Scope**: Clean staging, atomic commits, branch creation, and push operations.
* **Execution Checklist**:
  1. Clean any temporary test results or cache folders (`tests/**/TestResults`).
  2. Create a clean branch from latest base: `git checkout -b feature/SCRUM-XXX-...`.
  3. Stage only relevant project files (`src/`, `tests/`, `docs/`).
  4. Create semantic commit message (e.g. `feat(roles): SCRUM-XXX Description`).
  5. Push upstream: `git push -u origin feature/SCRUM-XXX-...`.
  6. Return branch name and GitHub PR creation URL.

---

## 3. Standard Verification Commands

```powershell
# Run all unit tests
dotnet test "F:\C# projects\HR_system"

# Run build with zero warnings threshold
dotnet build "F:\C# projects\HR_system"

# Clean temporary test result outputs
Remove-Item -Recurse -Force "F:\C# projects\HR_system\tests\*\TestResults" -ErrorAction SilentlyContinue
```
