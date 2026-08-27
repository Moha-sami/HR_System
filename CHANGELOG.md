# Changelog

All notable changes to the Buy2 HR Management System (HRMS) project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## 2026-08-27 — Refactor Job Creation & Authorization Fixes [SCRUM-284]
- Files:
  - `src/Buy2.Application/Common/Security/AuthorizeAttribute.cs`
  - `src/Buy2.Application/Features/Jobs/CreateJob/CreateJobCommand.cs`
  - `src/Buy2.Application/Features/Jobs/UpdateJob/UpdateJobCommand.cs`
  - `src/Buy2.Application/Features/Jobs/CreateJob/CreateJobCommandHandler.cs`
- Summary: Removed speculative `AuthorizeAttribute.cs` and `[Authorize]` attributes in `CreateJobCommand.cs` and `UpdateJobCommand.cs`. Fixed atomicity in `CreateJobCommandHandler` by moving title uniqueness check and work model validation before resolving or persisting a new inline department.

## 2026-08-27 — Job Creation & Modification Wizard Backend [SCRUM-283]
- Files:
  - `src/Buy2.Application/Features/Jobs/CreateJob/CreateJobCommand.cs`
  - `src/Buy2.Application/Features/Jobs/UpdateJob/UpdateJobCommand.cs`
  - `src/Buy2.Application/Features/Jobs/DTOs/JobWizardDtos.cs`
  - `src/Buy2.Api/Controllers/JobsController.cs`
- Summary: Implemented Job Creation & Modification Wizard Backend. Added CreateJobCommand, UpdateJobCommand, and corresponding JobsController endpoints. Implemented JobWizardDtos for data transfer. Implemented Work Model rules and duplicate title checks. Added comprehensive tests and verified architecture rules.
- Tests: PASS
- Security review: PASS

## 2026-08-25 — JobRole, Department, Qualification Domain Entities Schema and EF Core Mapping [SCRUM-278]
- Files:
  - `src/Buy2.Domain/Entities/JobRole.cs`
  - `src/Buy2.Domain/Entities/Department.cs`
  - `src/Buy2.Domain/Entities/Qualification.cs`
  - `src/Buy2.Infrastructure/Persistence/Configurations/JobRoleConfiguration.cs`
  - `src/Buy2.Infrastructure/Persistence/Configurations/DepartmentConfiguration.cs`
  - `src/Buy2.Infrastructure/Persistence/Configurations/QualificationConfiguration.cs`
  - `src/Buy2.Infrastructure/Persistence/Buy2DbContext.cs`
  - `tests/Buy2.Domain.Tests/Jobs/JobEntityTests.cs`
- Summary: Implemented domain entities and EF Core entity configurations for JobRole, Department, and Qualification. Configured domain models extending `BaseEntity` with audit properties (`CreatedAt`, `UpdatedAt`, `IsActive`), navigation properties, default values, JSON storage properties (`RequiredQualificationsJson`, `OnlineWorkdaysJson`, `OfflineWorkdaysJson`), composite index `(Title, DepartmentId)` and `Restrict` delete behavior on `JobRole`, unique index on `Department.Name`, and unique index on `Qualification.Name`. Registered `DbSet<JobRole>`, `DbSet<Department>`, and `DbSet<Qualification>` in `Buy2DbContext`. Added comprehensive domain entity unit tests covering default value initialization and property assignments in `JobEntityTests.cs`.
- Tests: PASS
- Security review: PASS

## 2026-08-25 — Job Listing, Details & Roster Management (GET /api/v1/jobs, GET /api/v1/jobs/{id}, GET /api/v1/jobs/{id}/employees) [SCRUM-277]
- Files:
  - `src/Buy2.Application/Features/Jobs/DTOs/JobDtos.cs`
  - `src/Buy2.Application/Features/Jobs/GetJobs/GetJobsQuery.cs`
  - `src/Buy2.Application/Features/Jobs/GetJobById/GetJobByIdQuery.cs`
  - `src/Buy2.Application/Features/Jobs/GetJobEmployees/GetJobEmployeesQuery.cs`
  - `src/Buy2.Api/Controllers/JobsController.cs`
  - `src/Buy2.Domain/Entities/JobRole.cs`
  - `src/Buy2.Infrastructure/Persistence/Configurations/JobRoleConfiguration.cs`
  - `tests/Buy2.Domain.Tests/Jobs/JobQueriesTests.cs`
  - `docs/API_ENDPOINTS.md`
  - `CHANGELOG.md`
- Summary: Implemented CQRS queries, DTOs, EF entity configurations, and REST controller endpoints for Job Role Management. Added `GET /api/v1/jobs` for paginated filtering and searching across titles and departments, `GET /api/v1/jobs/{id}` for detailed job role specs with parsed JSON workdays and qualifications, and `GET /api/v1/jobs/{id}/employees` for paginated active employee roster retrieval. Protected endpoints with role-based authorization (`[Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]`). Added unit test suite covering listing, filtering, details retrieval, and roster pagination. Updated API documentation.
- Tests: PASS
- Security review: PASS

## 2026-08-25 — Role Lookup Endpoint (GET /api/v1/roles/lookup) [SCRUM-267]
- Files:
  - `src/Buy2.Application/DTOs/Roles/RoleLookupItemDto.cs`
  - `src/Buy2.Application/Features/Roles/GetRoleLookup/GetRoleLookupQuery.cs`
  - `src/Buy2.Api/Controllers/RolesController.cs`
  - `tests/Buy2.Domain.Tests/Roles/GetRoleLookupQueryTests.cs`
  - `docs/API_ENDPOINTS.md`
  - `CHANGELOG.md`
- Summary: Added DTO `RoleLookupItemDto(int Id, string Name)`. Implemented query and handler `GetRoleLookupQuery` co-located in `GetRoleLookupQuery.cs` using `IRepository<Role>` with `.AsNoTracking()`, filtering active roles (`IsActive == true`) sorted alphabetically by name and supporting optional role exclusion (`excludeRoleId`). Added `GET /api/v1/roles/lookup` endpoint to `RolesController` with `[Authorize]`. Added unit tests in `GetRoleLookupQueryTests.cs`.
- Tests: PASS
- Security review: PASS

## 2026-08-24 — Atomic User Reassignment & Role Deletion (POST /api/v1/roles/{id}/reassign-and-delete) [SCRUM-266]
- Files:
  - `src/Buy2.Application/DTOs/Roles/ReassignUsersAndDeleteRoleDto.cs`
  - `src/Buy2.Application/DTOs/Roles/RoleDeletionResultDto.cs`
  - `src/Buy2.Application/Features/Roles/DeleteRole/ReassignUsersAndDeleteRoleCommand.cs`
  - `src/Buy2.Application/Features/Roles/DeleteRole/ReassignUsersAndDeleteRoleCommandHandler.cs`
  - `src/Buy2.Application/Features/Roles/DeleteRole/ReassignUsersAndDeleteRoleDtoValidator.cs`
  - `src/Buy2.Application/Validators/ReassignUsersAndDeleteRoleDtoValidator.cs`
  - `src/Buy2.Application/Common/Interfaces/IBuy2DbContext.cs`
  - `src/Buy2.Infrastructure/Persistence/Buy2DbContext.cs`
  - `src/Buy2.Infrastructure/DependencyInjection.cs`
  - `src/Buy2.Api/Controllers/RolesController.cs`
  - `tests/Buy2.Domain.Tests/Roles/ReassignUsersAndDeleteRoleCommandTests.cs`
  - `tests/Buy2.Domain.Tests/Roles/ReassignUsersAndDeleteRoleDtoValidatorTests.cs`
  - `docs/API_ENDPOINTS.md`
  - `CHANGELOG.md`
- Summary: Implemented `ReassignUsersAndDeleteRoleCommand` and handler for atomic user role reassignment and target role deletion/decommissioning. Open EF Core database transaction to reassign affected employees to valid active replacement roles and deactivate the role (`IsActive = false`, `UpdatedAt = UtcNow`). Prevented system role deletion (`IsSystemRole == true`), unmapped employees, and self-reassignment or invalid replacement roles. Added `POST /api/v1/roles/{id}/reassign-and-delete` endpoint to `RolesController` with full unit test coverage and updated API documentation.
- Tests: PASS
- Security review: PASS

## 2026-08-23 — Create New Role Endpoint (POST /api/v1/roles)
- Files:
  - `src/Buy2.Application/Features/Roles/CreateRole/CreateRoleCommand.cs`
  - `src/Buy2.Api/Controllers/RolesController.cs`
  - `tests/Buy2.Domain.Tests/Roles/CreateRoleCommandTests.cs`
  - `docs/API_ENDPOINTS.md`
  - `CHANGELOG.md`
- Summary: Refactored `CreateRoleCommand` to return `CreateRoleResult(Success, IsConflict, CreatedRole, ErrorMessage)`. Implemented case-insensitive role name uniqueness check ignoring query filters. Created role entities with `IsSystemRole = false`, `IsActive = true`, serialized permissions, trimmed strings, and mapped to `RoleDetailsDto` on creation. Added `POST /api/v1/roles` action to `RolesController.cs` protected by `[Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]` returning `201 Created` with `RoleDetailsDto` or `409 Conflict` on duplicate role name. Deleted obsolete `CreateRoleController.cs`. Created unit test suite in `CreateRoleCommandTests.cs` verifying 201 creation, 409 conflict, default system role false, and whitespace trimming.
- Tests: PASS
- Security review: PASS

## 2026-08-23 — Employee Payroll Profile History Integration (Task 3)
- Files:
  - `src/Buy2.Application/DTOs/Employees/PayrollDtos.cs`
  - `src/Buy2.Application/Features/Employees/GetEmployeePayroll/GetEmployeePayrollProfileQuery.cs`
  - `tests/Buy2.Domain.Tests/Employees/GetEmployeePayrollProfileQueryTests.cs`
  - `docs/API_ENDPOINTS.md`
  - `CHANGELOG.md`
- Summary: Added `PayrollRecordDto` and updated `EmployeePayrollProfileDto` to include `List<PayrollRecordDto> PayrollRecords`. Injected `IRepository<PayrollRecord>` in `GetEmployeePayrollProfileQueryHandler`, querying payroll history records using `.AsNoTracking()`, sorted in descending order of period (`PeriodYear` DESC, `PeriodMonth` DESC), and mapped with safe UTC start and end period dates. Added comprehensive unit tests in `GetEmployeePayrollProfileQueryTests.cs`. Updated `docs/API_ENDPOINTS.md` and `CHANGELOG.md`.
- Tests: PASS
- Security review: PASS


## 2026-08-23 — Performance & Attendance ERD Entity Inclusions (Task 2)
- Files:
  - `src/Buy2.Domain/Entities/EmployeeAchievement.cs`
  - `src/Buy2.Domain/Entities/Badge.cs`
  - `src/Buy2.Domain/Entities/AttendanceRecord.cs`
  - `src/Buy2.Infrastructure/Persistence/Configurations/EmployeeAchievementConfiguration.cs`
  - `src/Buy2.Infrastructure/Persistence/Configurations/AttendanceRecordConfiguration.cs`
  - `src/Buy2.Application/Features/Employees/GetPerformanceOverview/PerformanceOverviewDto.cs`
  - `src/Buy2.Application/Features/Employees/GetPerformanceOverview/GetPerformanceOverviewQuery.cs`
  - `src/Buy2.Application/Features/Employees/GetAttendanceCalendar/GetAttendanceCalendarQuery.cs`
  - `tests/Buy2.Domain.Tests/Employees/GetPerformanceOverviewQueryTests.cs`
  - `tests/Buy2.Domain.Tests/Employees/GetAttendanceCalendarQueryTests.cs`
  - `docs/API_ENDPOINTS.md`
  - `CHANGELOG.md`
- Summary: Enhanced domain entities and EF Core configurations for ERD integration. Added `BadgeId` (nullable `int?`), `Badge? Badge` navigation, and `PointsAwarded` to `EmployeeAchievement`, with `DeleteBehavior.SetNull` catalog relationship configured in `EmployeeAchievementConfiguration`. Added `PointsAwarded` to `Badge`. Added `ScheduledShift? ScheduledShift` navigation to `AttendanceRecord` with `DeleteBehavior.SetNull` relationship configured in `AttendanceRecordConfiguration`. Updated `GetPerformanceOverviewQueryHandler` to eager load `Badge` entity (`.Include(a => a.Badge)`), added `.AsNoTracking()` to EF queries, and updated `AchievementBadgeDto` to return rich badge metadata (`Id`, `Title`, `Description`, `IconUrl`, `PointsAwarded`, `EarnedAt`) with fallback mapping for legacy string records. Updated `GetAttendanceCalendarQueryHandler` by injecting `IRepository<ShiftEntity>` and `IRepository<Request>`, eager loading `ScheduledShift` on `AttendanceRecord`, calculating target break times and scheduled shift hours dynamically (e.g. 60m break for >=8h shifts, 30m for >=4h shifts), mapping approved leave requests directly onto calendar days, and adding `.AsNoTracking()` to EF queries. Added comprehensive unit tests in `GetPerformanceOverviewQueryTests.cs` and `GetAttendanceCalendarQueryTests.cs`. Updated `docs/API_ENDPOINTS.md` and `CHANGELOG.md`.
- Tests: PASS
- Security review: PASS


## 2026-08-23 — Employee Directory & Profile Department/Region ERD Integration (Task 1)
- Files:
  - `src/Buy2.Domain/Entities/JobRole.cs`
  - `src/Buy2.Infrastructure/Persistence/Configurations/JobRoleConfiguration.cs`
  - `src/Buy2.Application/Features/Employees/GetEmployees/GetEmployeesQuery.cs`
  - `src/Buy2.Application/Features/Employees/GetEmployee/GetEmployeeProfileQuery.cs`
  - `tests/Buy2.Domain.Tests/Employees/GetEmployeesAndProfileDepartmentRegionTests.cs`
  - `docs/API_ENDPOINTS.md`
  - `CHANGELOG.md`
- Summary: Updated `JobRole` entity with nullable `DepartmentId` and `Department? Department` navigation property, and created `JobRoleConfiguration` in infrastructure with EF Core foreign key mapping (`OnDelete(DeleteBehavior.Restrict)`). Enhanced `GetEmployeesQueryHandler` to eager load `JobRole.Department` and `Site.Region`, providing support for filtering employees by Department (both integer ID and name substring match) and Region (both integer ID and name substring match). Updated `GetEmployeeProfileQueryHandler` to eager load `JobRole.Department` and map `Department` in `EmployeeJobDetailsDto` (`employee.JobRole?.Department?.Name ?? "N/A"`). Added full unit test suite in `GetEmployeesAndProfileDepartmentRegionTests.cs` (79/79 tests passing). Updated `docs/API_ENDPOINTS.md` and `CHANGELOG.md`.
- Tests: PASS (79/79 passed)
- Security review: PASS


## 2026-08-23 — Implement Get Role By ID Endpoint (GET /api/v1/roles/{id})
- Files:
  - `src/Buy2.Application/Features/Roles/GetRoleById/GetRoleByIdQuery.cs`
  - `src/Buy2.Api/Controllers/RolesController.cs`
  - `docs/API_ENDPOINTS.md`
  - `CHANGELOG.md`
- Summary: Implemented `GetRoleByIdQuery` and `GetRoleByIdQueryHandler` in `Buy2.Application` to retrieve detailed role information by ID (`RoleDetailsDto`). Handler queries the database using `.IgnoreQueryFilters().AsNoTracking().Include(r => r.Employees)` to fetch complete role data regardless of global query filters. Calculates active assigned employee counts filtering soft-deleted records (`!e.IsDeleted`) and performs resilient, safe permissions JSON parsing into `List<ModulePermissionDto>` (supporting direct JSON module permission arrays, `RolePermissionsDocument` value objects, and legacy string lists). Exposed `GET /api/v1/roles/{id}` endpoint on `RolesController` protected by `[Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]` returning `200 OK` with `RoleDetailsDto` (containing `id`, `name`, `description`, `isSystemRole`, `isActive`, `assignedEmployeesCount`, `createdAt`, `updatedAt`, and `permissions`), `404 NotFound` when role is missing, `401 Unauthorized`, and `403 Forbidden`. Updated `docs/API_ENDPOINTS.md` and `CHANGELOG.md`.
- Tests: PASS
- Security review: PASS

## 2026-08-23 — Implement Get Paginated Roles List Endpoint (GET /api/v1/roles)
- Files:
  - `src/Buy2.Application/Features/Roles/GetRoles/GetRolesQuery.cs`
  - `src/Buy2.Api/Controllers/RolesController.cs`
  - `tests/Buy2.Domain.Tests/Roles/GetRolesQueryTests.cs`
  - `docs/API_ENDPOINTS.md`
- Summary: Implemented `GetRolesQuery` and `GetRolesQueryHandler` in `Buy2.Application` to retrieve paginated roles with search filtering by name or description (`SearchTerm`), active status filtering (`IsActive`), and clamped pagination (`PageNumber` default 1 min 1, `PageSize` default 10 range 1..100). Computes non-deleted assigned employee counts (`AssignedEmployeesCount`), orders system roles first then alphabetically by name, and safely parses permissions JSON into `PermissionsSummary` module lists. Exposed `GET /api/v1/roles` endpoint on `RolesController` protected by `[Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]` returning `200 OK` with `RolePaginatedResponseDto`, `401 Unauthorized`, and `403 Forbidden`. Added comprehensive unit tests in `GetRolesQueryTests.cs` and updated `docs/API_ENDPOINTS.md`.
- Tests: PASS (70/70 passed)
- Security review: PASS

## 2026-08-23 — Role Deletion Impact Analysis & Batch Reassignment DTOs and Validation Rules
- Files:
  - `src/Buy2.Application/DTOs/Roles/RoleDeletionImpactDto.cs`
  - `src/Buy2.Application/DTOs/Roles/ReassignUsersAndDeleteRoleDto.cs`
  - `src/Buy2.Application/DTOs/Roles/RoleDeletionResultDto.cs`
  - `src/Buy2.Application/Validators/ReassignUsersAndDeleteRoleDtoValidator.cs`
  - `tests/Buy2.Domain.Tests/Roles/ReassignUsersAndDeleteRoleDtoValidatorTests.cs`
- Summary: Added DTO records (`AffectedEmployeeDto`, `RoleDeletionImpactDto`, `UserRoleReassignmentItemDto`, `ReassignUsersAndDeleteRoleDto`, `RoleDeletionResultDto`) in `Buy2.Application.DTOs.Roles` for analyzing role deletion impact on assigned employees and performing batch user role reassignments upon role deletion. Implemented FluentValidation rules in `ReassignUsersAndDeleteRoleDtoValidator` preventing self/identical role replacement, enforcing positive identifier constraints (`> 0`), preventing duplicate employee reassignments, and requiring at least one reassignment mechanism when assigned employees exist. Added complete unit test suite in `ReassignUsersAndDeleteRoleDtoValidatorTests.cs`.
- Tests: PASS (64/64 passed)
- Security review: PASS

## 2026-08-23 — Define Request Payload DTOs and FluentValidation Rules for Role Creation and Modification
- Files:
  - `src/Buy2.Application/Buy2.Application.csproj`
  - `src/Buy2.Application/DTOs/Roles/PermissionScopeDto.cs`
  - `src/Buy2.Application/DTOs/Roles/ModulePermissionDto.cs`
  - `src/Buy2.Application/DTOs/Roles/CreateRoleDto.cs`
  - `src/Buy2.Application/DTOs/Roles/UpdateRoleDto.cs`
  - `src/Buy2.Application/DTOs/Roles/RoleDtos.cs`
  - `src/Buy2.Application/Validators/PermissionScopeDtoValidator.cs`
  - `src/Buy2.Application/Validators/ModulePermissionDtoValidator.cs`
  - `src/Buy2.Application/Validators/CreateRoleDtoValidator.cs`
  - `src/Buy2.Application/Validators/UpdateRoleDtoValidator.cs`
  - `tests/Buy2.Domain.Tests/Roles/RoleDtoValidatorTests.cs`
- Summary: Added `FluentValidation` v11.11.0 package reference to `Buy2.Application.csproj`. Created DTO records `PermissionScopeDto`, `ModulePermissionDto`, `CreateRoleDto`, and `UpdateRoleDto` in `Buy2.Application.DTOs.Roles`, and updated `RoleDtos.cs` to eliminate duplicate `ModulePermissionDto` definitions. Implemented FluentValidation rules across `CreateRoleDtoValidator`, `UpdateRoleDtoValidator`, `ModulePermissionDtoValidator`, and `PermissionScopeDtoValidator` validating role name (required, non-empty, trimmed length 2-100), optional description (max 500 characters), non-empty permissions list with case-insensitive unique module check, valid module name parsing (`PermissionModule`), action catalog whitelist check (`ModuleActionCatalog.IsActionSupported`), valid scope type parsing (`AccessScope`), module-scope compatibility (`ModuleActionCatalog.IsScopeSupported`), and target ID invariants (`TargetIds` empty when `ScopeType` is `All`, non-empty list of positive integers `> 0` when `ScopeType` is not `All`). Added comprehensive unit test suite in `RoleDtoValidatorTests.cs` passing with 0 warnings and 0 errors.
- Tests: PASS (47/47 passed)
- Security review: PASS

## 2026-08-23 — Align Employee Profile DTO 1:1 with Figma Information Tab Mockup
- Files:
  - `src/Buy2.Application/DTOs/Employees/EmployeeProfileDtos.cs`
  - `src/Buy2.Application/Features/Employees/GetEmployee/GetEmployeeProfileQuery.cs`
  - `tests/Buy2.Domain.Tests/Employees/EmployeeProfileTests.cs`
  - `docs/API_ENDPOINTS.md`
- Summary: Aligned `EmployeePersonalInfoDto` 1:1 with the Figma Information Tab Mockup by updating its record parameters to `(string Name, DateTime? Birthdate, string Email, string PhoneNumber, Gender Gender)` and removing `Address`, `EmergencyContact`, and `NationalId`. Updated `GetEmployeeProfileQueryHandler` mapping to map employee name, birthdate, email, phone number, and gender into `EmployeePersonalInfoDto`, and fixed CS0136 variable shadowing by renaming direct manager name to `managerFullName`. Updated unit tests in `EmployeeProfileTests.cs` to test the updated DTO structure and JSON serialization. Updated OpenAPI documentation and example JSON responses in `docs/API_ENDPOINTS.md`.
- Tests: PASS (31/31 passed)
- Security review: PASS

## 2026-08-23 — Enrich GET /api/v1/employees/{id} to Return Full Figma Information Tab Data
- Files:
  - `src/Buy2.Application/DTOs/Employees/EmployeeProfileDtos.cs`
  - `src/Buy2.Application/Features/Employees/GetEmployee/GetEmployeeProfileQuery.cs`
  - `src/Buy2.Api/Controllers/EmployeeDirectoryController.cs`
  - `docs/API_ENDPOINTS.md`
  - `tests/Buy2.Domain.Tests/Employees/EmployeeProfileTests.cs`
  - `tests/Buy2.Domain.Tests/Buy2.Domain.Tests.csproj`
- Summary: Enriched `EmployeePersonalInfoDto` with `Address`, `EmergencyContact`, and `NationalId`. Enriched `EmployeeJobDetailsDto` with `Qualifications`, `AttendanceType`, `OnlineWorkdays`, and `OfflineWorkdays`. Added `EmployeePayrollSummaryDto` capturing `SalaryType`, `PaymentAmount`, `PayoutPeriod`, `PayoutDay`, `WorkWeekStartDay`, `WorkWeekEndDay`, `OvertimeEnabled`, `OvertimeThresholdHours`, `OvertimeRateMultiplier`, and `AssignedWorkSiteIds`. Updated `EmployeeProfileDto` with optional `EmployeePayrollSummaryDto? Payroll = null`. Enhanced `GetEmployeeProfileQueryHandler` to eager-load `PayrollProfile` and `EmployeeSites`, safely parse JSON workdays/qualifications using local helper `ParseJsonList`, format manager names using explicit default pattern, and assemble the enriched profile DTO. Restricted `GET /api/v1/employees/{id}` to `[Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]` on `EmployeeDirectoryController` to protect sensitive PII and payroll summary data. Added comprehensive unit tests in `EmployeeProfileTests.cs`. Updated OpenAPI and endpoint documentation in `docs/API_ENDPOINTS.md`.
- Tests: PASS (31/31 passed)
- Security review: PASS

## 2026-08-23 — Define Role DTO Data Contracts
- Files:
  - `src/Buy2.Application/DTOs/Roles/RoleDtos.cs`
- Summary: Defined Role DTO data contracts in `Buy2.Application.DTOs.Roles` namespace: `ModulePermissionDto` (granular module permission model with Module, Actions, Scope, ScopeTargetIds), `RoleListItemDto` (role summary representation for list views with Id, Name, Description, AssignedEmployeesCount, IsSystemRole, IsActive, CreatedAt, PermissionsSummary), `RoleDetailsDto` (full detailed role representation with granular module permissions, audit timestamps, and assigned employees count), `RoleFilterQueryDto` (query filter with safe defaults for pagination and filtering), `RolePaginatedResponseDto` (standardized paginated response envelope with Items, TotalCount, PageNumber, PageSize, TotalPages), and `RoleLookupItemDto` (lightweight lookup model for UI selection dropdowns).
- Tests: PASS (26/26)
- Security review: PASS

## 2026-08-22 — Implement Role Permission Catalogs, Value Objects & Domain Validation Rules
- Files:
  - `src/Buy2.Domain/Enums/PermissionModule.cs`
  - `src/Buy2.Domain/Enums/AccessScope.cs`
  - `src/Buy2.Domain/Exceptions/InvalidRolePermissionException.cs`
  - `src/Buy2.Domain/Permissions/ModuleActionCatalog.cs`
  - `src/Buy2.Domain/ValueObjects/ModulePermission.cs`
  - `src/Buy2.Domain/ValueObjects/RolePermissionsDocument.cs`
  - `src/Buy2.Domain/Entities/Role.cs`
  - `tests/Buy2.Domain.Tests/Permissions/RolePermissionTests.cs`
- Summary: Defined `PermissionModule` and `AccessScope` enums in pure C# domain layer. Implemented `ModuleActionCatalog` with full action and scope mappings across all 6 system modules (`EmployeeManagement`, `JobManagement`, `SiteManagement`, `PointsManagement`, `NotificationsManagement`, `RewardManagement`) along with case-insensitive validation lookup methods (`IsActionSupported`, `GetSupportedActions`, `IsScopeSupported`, `GetSupportedScopes`). Implemented `ModulePermission` value object enforcing domain invariants (action whitelist, positive target IDs, non-empty IDs for scoped permissions, empty IDs for `AccessScope.All`, and module-supported scopes). Implemented `RolePermissionsDocument` value object enforcing module uniqueness, evaluating hierarchical `HasPermission` checks, and supporting robust JSON serialization/deserialization. Extended `Role` entity with typed domain helper methods (`GetPermissionsDocument`, `SetPermissionsDocument`, `HasPermission`). Added comprehensive xUnit test suite (`Buy2.Domain.Tests`) with 26 tests verifying all domain invariants, boundary conditions, error throwing, and JSON roundtrips.
- Tests: PASS (26/26)
- Security review: PASS

## 2026-08-22 — Implement Role Entity and EF Core RoleConfiguration
- Files:
  - `src/Buy2.Domain/Entities/Role.cs`
  - `src/Buy2.Infrastructure/Persistence/Configurations/RoleConfiguration.cs`
  - `src/Buy2.Infrastructure/Persistence/Configurations/EmployeeConfiguration.cs`
- Summary: Enhanced the `Role` domain entity with `Description`, `IsSystemRole`, `IsActive`, `PermissionsJson`, `UpdatedAt`, and `Employees` navigation collection. Implemented `RoleConfiguration` using EF Core Fluent API with table mapping, unique name constraint, default values, and `Restrict` delete behavior on employee foreign keys. Updated `EmployeeConfiguration` to map the bidirectional navigation relationship to `Role.Employees`.
- Tests: PASS
- Security review: PASS

## 2026-08-22 — Export Employee Violations as CSV (GET api/v1/employees/{id}/violations/export)
- Files:
  - `src/Buy2.Application/Features/Employees/ExportViolations/ExportViolationsQuery.cs`
  - `src/Buy2.Api/Controllers/EmployeeDirectoryController.cs`
- Summary: Implemented `ExportViolationsQuery` and `ExportViolationsQueryHandler` to export employee disciplinary violations as an RFC-4180-compliant CSV file with UTF-8 BOM preamble (`employee_{id}_violations.csv`). Supports filtering by violation type, severity level, and date range bounds. Eagerly loads reporter and action taker details with safe fallbacks and verifies employee existence and soft-delete status. Exposed `GET /api/v1/employees/{id}/violations/export` on `EmployeeDirectoryController` protected by `[Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]` returning `200 OK` (`text/csv`), `404 NotFound` (when employee is missing or soft-deleted), `401 Unauthorized`, and `403 Forbidden`.
- Tests: PASS
- Security review: PASS

## 2026-08-22 — Resolve Employee Disciplinary Violation (PATCH api/v1/employees/{id}/violations/{violationId}/resolve)
- Files:
  - `src/Buy2.Application/Features/Employees/ResolveViolation/ResolveViolationDto.cs`
  - `src/Buy2.Application/Features/Employees/ResolveViolation/ResolveViolationCommand.cs`
  - `src/Buy2.Api/Controllers/EmployeeDirectoryController.cs`
- Summary: Implemented `ResolveViolationCommand` and `ResolveViolationCommandHandler` to resolve an employee disciplinary violation, updating status to `Resolved`, recording action metadata (`ActionType`, `ActionDescription`, `ActionDate` defaulting to UTC now, and `ActionTakenById`), and validating employee and violation existence and soft-delete status. Prevents re-resolving already resolved violations. Exposed `PATCH /api/v1/employees/{id}/violations/{violationId}/resolve` on `EmployeeDirectoryController` protected by `[Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]` returning `204 NoContent`, `400 BadRequest` (already resolved), `404 NotFound` (employee or violation missing), `401 Unauthorized`, and `403 Forbidden`.
- Tests: PASS
- Security review: PASS

## 2026-08-22 — Get Employee Violation Detail (GET api/v1/employees/{id}/violations/{violationId})
- Files:
  - `src/Buy2.Application/Features/Employees/GetViolationDetail/ViolationDetailDto.cs`
  - `src/Buy2.Application/Features/Employees/GetViolationDetail/GetViolationDetailQuery.cs`
  - `src/Buy2.Api/Controllers/EmployeeDirectoryController.cs`
- Summary: Implemented `GetViolationDetailQuery` and `GetViolationDetailQueryHandler` to retrieve detailed information for a specific disciplinary violation, including reporter, action taker, safe parsing of witnesses list, and conditional action details. Exposed `GET /api/v1/employees/{id}/violations/{violationId}` on `EmployeeDirectoryController` protected by `[Authorize]` returning `200 OK` with `ViolationDetailDto` or `404 NotFound`.
- Tests: PASS
- Security review: PASS

## 2026-08-22 — Get Employee Violations (GET api/v1/employees/{id}/violations)
- Files:
  - `src/Buy2.Application/Features/Employees/GetViolations/ViolationDto.cs`
  - `src/Buy2.Application/Features/Employees/GetViolations/GetViolationsQuery.cs`
  - `src/Buy2.Api/Controllers/EmployeeDirectoryController.cs`
- Summary: Implemented `GetViolationsQuery` and `GetViolationsQueryHandler` to retrieve disciplinary violations for an employee with optional filtering by violation type, severity level, and date range bounds. Eagerly loads `ReportedBy` supervisor details with fallback formatting and verifies employee existence and soft-delete status. Exposed `GET /api/v1/employees/{id}/violations` endpoint on `EmployeeDirectoryController` protected by `[Authorize]` returning `200 OK` with a list of `ViolationDto` or `404 NotFound`.
- Tests: PASS
- Security review: PASS

## 2026-08-22 — Get Employee Points Transactions (GET api/v1/employees/{id}/points/transactions)
- Files:
  - `src/Buy2.Application/Features/Employees/GetPointsTransactions/PointsTransactionDto.cs`
  - `src/Buy2.Application/Features/Employees/GetPointsTransactions/GetPointsTransactionsQuery.cs`
  - `src/Buy2.Api/Controllers/EmployeeDirectoryController.cs`
- Summary: Implemented `GetPointsTransactionsQuery` and `GetPointsTransactionsQueryHandler` to retrieve paginated employee points ledger transaction history with filtering by transaction type (`Earned` / `Redeemed`), `TriggeredBy` reason / rule key, and `DateFrom` / `DateTo` ranges. Exposed `GET /api/v1/employees/{id}/points/transactions` endpoint on `EmployeeDirectoryController` protected by `[Authorize]` returning `200 OK` with `PaginatedPointsTransactionsDto` or `404 NotFound` for missing or soft-deleted employees.
- Tests: PASS
- Security review: PASS

## 2026-08-22 — Get Employee Points Summary (GET api/v1/employees/{id}/points/summary)
- Files:
  - `src/Buy2.Application/Features/Employees/GetPointsSummary/PointsSummaryDto.cs`
  - `src/Buy2.Application/Features/Employees/GetPointsSummary/GetPointsSummaryQuery.cs`
  - `src/Buy2.Api/Controllers/EmployeeDirectoryController.cs`
- Summary: Implemented `GetPointsSummaryQuery` and `GetPointsSummaryQueryHandler` to retrieve employee gamification points summary including current point balance (sum of transaction amounts), total points redeemed (sum of redemption transaction amounts), total count of redeemed rewards, and total rewards cost in points from `RewardRedemption` records. Exposed `GET /api/v1/employees/{id}/points/summary` endpoint on `EmployeeDirectoryController` protected by `[Authorize]` returning `200 OK` with `PointsSummaryDto` or `404 NotFound`.
- Tests: PASS
- Security review: PASS

## 2026-08-21 — Get Employee Attendance Calendar (GET api/v1/employees/{id}/attendance/calendar)
- Files:
  - `src/Buy2.Domain/Entities/AttendanceRecord.cs`
  - `src/Buy2.Infrastructure/Persistence/Buy2DbContext.cs`
  - `src/Buy2.Infrastructure/Persistence/Configurations/AttendanceRecordConfiguration.cs`
  - `src/Buy2.Application/Features/Employees/GetAttendanceCalendar/GetAttendanceCalendarQuery.cs`
  - `src/Buy2.Api/Controllers/EmployeeDirectoryController.cs`
- Summary: Implemented `GetAttendanceCalendarQuery` and `GetAttendanceCalendarQueryHandler` to generate a full monthly attendance calendar breakdown with summary metrics (`AttendanceRate`, `PunctualityScore`, `AverageLatenessMinutes`, `RecordedHours`, `TargetHours`), fallback hour computation from clock-in/out stamps, workday/weekend resolution, and soft-delete protection. Created `AttendanceRecord` entity and EF Core configuration. Exposed `GET /api/v1/employees/{id}/attendance/calendar` endpoint on `EmployeeDirectoryController` protected by `[Authorize]`.
- Tests: PASS
- Security review: PASS

## 2026-08-21 — Get Employee Tasks (GET api/v1/employees/{id}/performance/tasks)
- Files:
  - `src/Buy2.Application/Features/Employees/GetEmployeeTasks/EmployeeTaskDto.cs`
  - `src/Buy2.Application/Features/Employees/GetEmployeeTasks/GetEmployeeTasksQuery.cs`
  - `src/Buy2.Api/Controllers/EmployeeDirectoryController.cs`
- Summary: Implemented `GetEmployeeTasksQuery` and `GetEmployeeTasksQueryHandler` to retrieve assigned tasks for an employee, with optional filtering by task status (`Todo`, `InProgress`, `Done`). Returns task details ordered chronologically by due date and creation timestamp, with UTC timestamps and soft-deleted employee check. Exposed `GET /api/v1/employees/{id}/performance/tasks` endpoint protected by `[Authorize]` returning `200 OK` or `404 NotFound`.
- Tests: PASS
- Security review: PASS

## 2026-08-21 — Get Employee Metric Detail (GET api/v1/employees/{id}/performance/metrics/{metricId})
- Files:
  - `src/Buy2.Application/Features/Employees/GetMetricDetail/MetricDetailDto.cs`
  - `src/Buy2.Application/Features/Employees/GetMetricDetail/GetMetricDetailQuery.cs`
  - `src/Buy2.Api/Controllers/EmployeeDirectoryController.cs`
- Summary: Implemented `GetMetricDetailQuery` and `GetMetricDetailQueryHandler` to retrieve granular performance metric details for an employee. Calculates all-time average score, period-filtered average score with rating label (`Needs Improvement`, `Satisfactory`, `Good Performance`, `Excellent`), chronological monthly trend points, and detailed individual submission records with feedback notes and evaluator name. Exposed `GET /api/v1/employees/{id}/performance/metrics/{metricId}` endpoint protected by `[Authorize]` returning `200 OK`, `400 BadRequest` on invalid filter bounds, and `404 NotFound` when employee or metric does not exist.
- Tests: PASS
- Security review: PASS

## 2026-08-21 — Get Employee Performance Overview (GET api/v1/employees/{id}/performance/overview)
- Files:
  - `src/Buy2.Application/Features/Employees/GetPerformanceOverview/PerformanceOverviewDto.cs`
  - `src/Buy2.Application/Features/Employees/GetPerformanceOverview/GetPerformanceOverviewQuery.cs`
  - `src/Buy2.Api/Controllers/EmployeeDirectoryController.cs`
- Summary: Implemented `GetPerformanceOverviewQuery` and `GetPerformanceOverviewQueryHandler` to retrieve employee performance metrics and task statistics. Computes overall weighted performance score, rating labels (`Needs Improvement`, `Satisfactory`, `Good Performance`, `Excellent`), task completion counts, overdue tasks, deadline compliance rate, earned achievement badges, chronological daily score trend points, and performance submission details. Supports flexible date range resolution via `period` (`today`, `thisWeek`, `thisMonth`, `thisYear`), rolling window `days`, or explicit `from`/`to` date ranges. Exposed `GET /api/v1/employees/{id}/performance/overview` endpoint protected by `[Authorize]` returning `200 OK` or `404 NotFound` when employee is missing or soft-deleted.
- Tests: PASS
- Security review: PASS

## 2026-08-21 — Update / Upsert Employee Payroll & Work Profile (PUT api/v1/employees/{id}/payroll)
- Files:
  - `src/Buy2.Application/Features/Employees/UpdatePayrollProfile/UpdatePayrollProfileDto.cs`
  - `src/Buy2.Application/Features/Employees/UpdatePayrollProfile/UpdatePayrollProfileCommand.cs`
  - `src/Buy2.Api/Controllers/EmployeeDirectoryController.cs`
- Summary: Implemented `UpdatePayrollProfileCommand` and `UpdatePayrollProfileCommandHandler` to update or upsert an employee's `PayrollProfile` (salary type, payout period, payout day, work week schedule, payment amount, and overtime thresholds/rates). Synchronizes `EmployeeSite` join table with site ID validation (returning `400 BadRequest` if any site is invalid). Serializes online and offline workdays to JSON on both `Employee` and `PayrollProfile`. Handled atomic persistence via `IUnitOfWork`. Exposed `PUT /api/v1/employees/{id}/payroll` endpoint protected by `[Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]` returning `204 NoContent` on success, `400 BadRequest` on validation failure, and `404 NotFound` when employee is not found or soft-deleted.
- Tests: PASS
- Security review: PASS

## 2026-08-21 — Get Employee Payroll & Work Profile (GET api/v1/employees/{id}/payroll)
- Files:
  - `src/Buy2.Application/DTOs/Employees/PayrollDtos.cs`
  - `src/Buy2.Application/Features/Employees/GetEmployeePayroll/GetEmployeePayrollProfileQuery.cs`
  - `src/Buy2.Api/Controllers/EmployeeDirectoryController.cs`
- Summary: Implemented `GetEmployeePayrollProfileQuery` & `GetEmployeePayrollProfileQueryHandler` to retrieve employee compensation structure, salary rate, work week schedules, assigned work site IDs, attendance models, and safely deserialized hybrid online/offline workdays. Handled default fallbacks for unconfigured payroll profiles (`IsConfigured = false`) and soft-deleted employee filtering. Exposed `GET /api/v1/employees/{id}/payroll` endpoint protected by `[Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]` returning `200 OK` or `404 NotFound`.
- Tests: PASS
- Security review: PASS

## 2026-08-21 — Update Employee Job Details (PUT api/v1/employees/{id}/job)
- Files:
  - `src/Buy2.Application/Features/Employees/UpdateJobDetails/UpdateJobDetailsDto.cs`
  - `src/Buy2.Application/Features/Employees/UpdateJobDetails/UpdateJobDetailsCommand.cs`
  - `src/Buy2.Api/Controllers/EmployeeDirectoryController.cs`
- Summary: Implemented partial update of employee job details (`JobRoleId`, `RoleId`, `SiteId`, `DirectManagerId`, `SeniorityLevel`, `ExperienceYears`, `JobType`, `AttendanceType`, `JoinDate`) via `UpdateJobDetailsCommand` / `UpdateJobDetailsDto`. Includes validation for foreign keys (`JobRole`, `Role`, `Site`, `DirectManager`) and self-manager assignment prevention. Exposed `PUT /api/v1/employees/{id}/job` endpoint protected by `[Authorize(Roles = "Admin,Manager")]` returning `204 NoContent` on success, `400 BadRequest` on FK/self-manager validation failure, and `404 NotFound` when employee is not found.
- Tests: PASS
- Security review: PASS

## 2026-08-20 — Update Employee Personal Info (PUT api/v1/employees/{id}/personal)
- Files:
  - `src/Buy2.Domain/Entities/Employee.cs`
  - `src/Buy2.Infrastructure/Persistence/Configurations/EmployeeConfiguration.cs`
  - `src/Buy2.Application/Features/Employees/UpdatePersonalInfo/UpdateEmployeePersonalInfoDto.cs`
  - `src/Buy2.Application/Features/Employees/UpdatePersonalInfo/UpdateEmployeePersonalInfoCommand.cs`
  - `src/Buy2.Api/Controllers/EmployeeDirectoryController.cs`
- Summary: Implemented partial update of employee personal details (`Address`, `EmergencyContact`, `NationalId`, `FirstName`, `LastName`, `PhoneNumber`, and `DateOfBirth`) via `UpdateEmployeePersonalInfoCommand` / `UpdateEmployeePersonalInfoDto`. Added EF Core entity mappings with max-length constraints. Exposed `PUT /api/v1/employees/{id}/personal` endpoint protected by `[Authorize(Roles = "Admin,Manager")]` returning `204 NoContent` on success and `404 NotFound` when not found.
- Tests: PASS
- Security review: PASS

## [Unreleased]

## 2026-08-28 — Update Job Title, Department, Qualifications & Work Model [SCRUM-285]
- Files: `src/Buy2.Api/Controllers/JobsController.cs`, `src/Buy2.Application/Features/Jobs/UpdateJobCommand.cs`
- Summary: Added job role update endpoint `PUT /api/v1/jobs/{id}` in `JobsController.cs`. Refactored `UpdateJobCommand` with helper methods (`ValidateDepartmentAsync`, `ValidateTitleUniquenessAsync`, `UpdateJobProperties`, `MapToResponse`) ensuring cyclomatic complexity <= 6. Added boundary condition unit tests covering missing job 404, title collision 409 (excluding self), and null-coalescing fallbacks.
- Tests: PASS
- Security review: N/A
## 2026-08-27 — 4-Step Wizard Job Role Creation [SCRUM-284]
- Files: `src/Buy2.Api/Controllers/JobsController.cs`, `src/Buy2.Application/Features/Jobs/CreateJob/CreateJobCommand.cs`
- Summary: Added 4-step wizard job role creation endpoint `POST /api/v1/jobs` with inline department creation resolution when `NewDepartmentName` is provided in `CreateJobCommand`. Implemented duplicate title validation in department returning HTTP 409 Conflict, and enforced `[Authorize(Roles = "HRAdmin,Admin,SuperAdmin")]`.
- Tests: PASS
- Security review: PASS

## 2026-08-27 — Job Creation & Modification Wizard Backend [SCRUM-283]
- Files: `src/Buy2.Application/Features/Jobs/DTOs/JobDtos.cs`, `src/Buy2.Application/Features/Jobs/CreateJobCommand.cs`, `src/Buy2.Application/Features/Jobs/UpdateJobCommand.cs`, `src/Buy2.Api/Controllers/JobsController.cs`, `src/Buy2.Api/Program.cs`
- Summary: Implemented Job Creation & Modification Wizard Backend. Added 4-step wizard DTOs `CreateJobDto`, `UpdateJobDto`, and `JobResponseDto`. Added MediatR commands (`CreateJobCommand`, `UpdateJobCommand`) with co-located handlers. Exposed `POST` and `PUT` endpoints in `JobsController` protected by strict role-based authorization (`[Authorize(Roles = "HRAdmin,Admin,SuperAdmin")]`) and set JWT clock skew to zero.
- Tests: PASS
- Security review: PASS

### Added
- **Employee Directory CSV Export (`[EP-2]`)**:
  - Implemented `ExportEmployeesQuery` MediatR request and `ExportEmployeesQueryHandler` in [`ExportEmployeesQuery.cs`](file:///F:/C%23%20projects/HR_system/src/Buy2.Application/Features/Employees/ExportEmployees/ExportEmployeesQuery.cs).
  - Added `GET /api/v1/employees/export` endpoint to [`EmployeeDirectoryController.cs`](file:///F:/C%23%20projects/HR_system/src/Buy2.Api/Controllers/EmployeeDirectoryController.cs).
  - Protected with role-based authorization `[Authorize(Roles = "Admin,Manager")]`.
  - Supports comprehensive server-side filtering by:
    - Search text (`FirstName`, `LastName`, `Email`, `EmployeeCode`)
    - Department (`JobRole.DepartmentId` / `JobRole.Title`)
    - Region (`Site.SiteName`)
    - Sorting by name, employee code, email, job title, and join date (ascending/descending).
  - Generates RFC-4180-compliant CSV with UTF-8 BOM preamble for full Microsoft Excel compatibility.

## 2026-08-20 — Soft Delete / Deactivate Employee (DELETE api/v1/employees/{id})
- Files:
  - `src/Buy2.Application/Features/Employees/DeleteEmployee/DeleteEmployeeCommand.cs`
  - `src/Buy2.Api/Controllers/EmployeeDirectoryController.cs`
- Summary: Implemented soft delete / deactivation for employees via MediatR (`DeleteEmployeeCommand` and `DeleteEmployeeCommandHandler`) and exposed `DELETE /api/v1/employees/{id}` endpoint. Sets `IsDeleted = true`, `DeletedAt = DateTimeOffset.UtcNow`, and `IsActive = false` while preserving related records (payroll, points, tasks, violations). Automatically filtered from standard queries via EF Core global query filters. Protected by `[Authorize(Roles = "Admin,SuperAdmin")]`.
- Tests: PASS
- Security review: PASS

## 2026-08-20 — Bulk Onboard Employees
- Files:
  - `src/Buy2.Application/Features/Employees/BulkOnboard/BulkOnboardDto.cs`
  - `src/Buy2.Application/Features/Employees/BulkOnboard/BulkOnboardCommand.cs`
  - `src/Buy2.Application/DTOs/Employees/EmployeeProfileDtos.cs`
  - `src/Buy2.Api/Controllers/EmployeeOnboardingController.cs`
- Summary: Implemented bulk employee onboarding (`BulkOnboardCommand`) via MediatR with batch entity lookup and duplicate email checking to prevent N+1 queries. Added payload duplicate filtering, SHA-256 password hashing for default passwords, flexible role/site/job-role resolution, and per-record partial-failure tracking (`BulkOnboardResultDto`). Exposed `POST api/v1/employees/bulk-onboard` endpoint protected by `[Authorize(Roles = "Admin,Manager")]`.
- Tests: PASS
- Security review: PASS

## 2026-08-20 — Get Employee Profile Query and Endpoint (GET api/v1/employees/{id})
- Files:
  - `src/Buy2.Application/Features/Employees/GetEmployee/GetEmployeeProfileQuery.cs`
  - `src/Buy2.Api/Controllers/EmployeeDirectoryController.cs`
- Summary: Implemented `GetEmployeeProfileQuery` and `GetEmployeeProfileQueryHandler` to retrieve full employee profile details by ID with eager loading for JobRole, Site, DirectManager, and Role. Added live stats calculation for total points, tasks, and gifts, qualification extraction, and exposed the `GET /api/v1/employees/{id}` endpoint with authorization.
- Tests: PASS
- Security review: PASS

