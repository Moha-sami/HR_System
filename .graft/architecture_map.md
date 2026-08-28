# HR System Graft Architecture Map

This document is a deterministic codebase graph index for the HR System solution. Subagents (`hr-spec-brainstormer`, `hr-architecture-guardian`, `hr-worker`, `hr-project-status`) must consult this index to instantly inspect project structure, entities, features, repositories, and API routes without multi-file grep scans.

---

## 1. Solution Architecture & Layer Mapping

```
Buy2.Domain (src/Buy2.Domain)
  ├── Entities/ (BaseEntity, Employee, JobRole, Role, Department, Qualification, Site, Region, ShiftEntity, ShiftTemplate, Request, RequestType, EmployeeAchievement, Badge, PayrollRecord, AuditLog)
  └── Common/ (BaseEntity, ValueObject)

Buy2.Application (src/Buy2.Application)
  ├── Common/Interfaces/ (IRepository<T>, IUnitOfWork)
  └── Features/ (CQRS Feature Slices with co-located Query/Command records and Handlers)
      ├── Authentication/ (Login, ResetPassword)
      ├── Employees/ (GetEmployees, GetEmployeeProfile, GetPerformanceOverview, GetAttendanceCalendar, GetEmployeePayroll)
      ├── Roles/ (GetRoles, GetRoleById, CreateRole, UpdateRole, DeleteRole, GetRoleDeletionImpact, GetRoleLookup)
      ├── Jobs/ (GetJobs, GetJobById, GetJobEmployees, DTOs, Validators)
      ├── Departments/ (DTOs, Validators)
      ├── Qualifications/ (DTOs, Validators)
      ├── Sites/ (GetSites, Regions/GetRegions, CreateRegion)
      ├── Points/ (CreateRule)
      └── ShiftMarket/ (GetOpenShifts)

Buy2.Infrastructure (src/Buy2.Infrastructure)
  ├── Persistence/
  │   ├── Buy2DbContext.cs (EF Core DbSets)
  │   ├── Configurations/ (JobRoleConfiguration, DepartmentConfiguration, QualificationConfiguration, etc.)
  │   └── Repositories/ (GenericRepository<T>, UnitOfWork)
  └── DependencyInjection.cs

Buy2.Api (src/Buy2.Api)
  └── Controllers/
      ├── AuthController.cs
      ├── EmployeesController.cs
      ├── RolesController.cs
      ├── JobsController.cs
      ├── SitesController.cs
      ├── PointsRulesController.cs
      └── OpenShiftsController.cs
```

---

## 2. Mandatory Architectural Boundaries

1. **CQRS Co-location Rule**: Every MediatR Command or Query record AND its corresponding Handler class MUST be co-located in the SAME SINGLE C# file under `src/Buy2.Application/Features/<Area>/<Feature>/`.
2. **Persistence Boundary Rule**: `src/Buy2.Application` MUST NEVER inject `IBuy2DbContext`, `IDbContext`, or EF Core types. All database queries/mutations use `IRepository<T>`, and persistence uses `IUnitOfWork`.
3. **Delete Behavior**: All EF Core FK relationships use `DeleteBehavior.Restrict` (no cascade wipes). Soft delete policy (`!IsDeleted`) strictly enforced.
4. **Complexity Limit**: Cyclomatic complexity per handler method MUST be $\le 6$.
5. **Mutation Hardening**: Stryker.NET mutation testing score MUST be 100.0% with 0 surviving mutants.
