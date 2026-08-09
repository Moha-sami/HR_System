# Available Tasks for Contributors - Buy2 HRMS Project

Welcome to the **Buy2 HR Management System (HRMS)** open-source repository!

**[🇪🇬 اقرأ شرح المهام بالعربية (Read API Tasks in Arabic)](./AVAILABLE_TASKS_AR.md) | [🇬🇧 Read in English](./AVAILABLE_TASKS.md)**

This file breaks down the entire project into atomic, step-by-step tasks suitable for contributors of all skill levels.

---

## Git Branching & Commit Naming Guidelines

To ensure automatic tracking between GitHub and Jira, follow these simple naming rules:

1. **Git Branch Name**: Include the Jira Issue Key (e.g., `SCRUM-105`):
   - Example: `SCRUM-105-create-login-endpoint-vertical-slice` or `feature/SCRUM-105-login-endpoint`
   - *Tip: Click "Create branch" inside your Jira task card to copy the exact branch name automatically.*

2. **Commit Message Format**:
   - Example: `SCRUM-105: [Feature Slice] Create Login Endpoint (Command, Handler & Controller)`

3. **Pull Request (PR) Title**:
   - Example: `SCRUM-105: [Feature Slice] Create Login Endpoint (Command, Handler & Controller)`

---

## Instructions for Team Members & Contributors

1. Pick an unassigned task from your **Jira Sprint Board**.
2. Create your working git branch including the Jira key (e.g., `SCRUM-105-login-endpoint`).
3. Each task represents a **Vertical Slice**: implement the MediatR Command/Query, the Handler with business logic in `Buy2.Application`, and the API Controller in `Buy2.Api` in a single PR!
4. Submit your **Pull Request directly targeting `main`**. Once merged by the lead, Jira automatically updates your task to **Done**!

---

## Labels & Difficulty Overview

### Suggested GitHub Labels
- `good first issue`: Ideal for beginners. Very small and safe.
- `layer:infrastructure`: Code belongs in `Buy2.Infrastructure`.
- `feature:slice`: Complete end-to-end feature (MediatR Command + Handler + Controller).
- `entity`: Entity model class.
- `database`: EF Core configuration / DbContext.
- `repository`: Repository pattern implementation.

---

## Completed Tasks (Domain Layer & Application Layer 100% DONE) ✅

- **Task 1** (`SCRUM-6`): BaseEntity.cs ✅ **[DONE - PR #1]**
- **Task 2** (`SCRUM-7`): Role.cs ✅ **[DONE - PR #10]**
- **Task 3** (`SCRUM-8`): JobRole.cs ✅ **[DONE - PR #9]**
- **Task 4** (`SCRUM-9`): Employee.cs ✅ **[DONE - PR #11]**
- **Task 5** (`SCRUM-10`): Site.cs ✅ **[DONE - PR #12]**
- **Task 6** (`SCRUM-11`): AttendanceProfile.cs ✅ **[DONE - PR #16]**
- **Task 7** (`SCRUM-12`): Shift.cs ✅ **[DONE - PR #17]**
- **Task 8** (`SCRUM-13`): ShiftClaim.cs ✅ **[DONE - PR #26]**
- **Task 9** (`SCRUM-14`): PointsRule.cs ✅ **[DONE - PR #23]**
- **Task 10** (`SCRUM-15`): RewardItem.cs ✅ **[DONE - PR #24]**
- **Task 11** (`SCRUM-16`): Gender and SalaryType Enums ✅ **[DONE - PR #18]**
- **Task 12** (`SCRUM-17`): ShiftStatus and ClaimStatus Enums ✅ **[DONE - PR #19]**
- **Task 13** (`SCRUM-18`): IRepository Generic Interface ✅ **[DONE - PR #41]**
- **Task 15** (`SCRUM-20`): IJwtTokenGenerator Interface ✅ **[DONE - PR #29]**
- **Task 47**: JwtTokenGenerator Implementation ✅ **[DONE - PR #70]**
- **Task 50**: Infrastructure DependencyInjection Setup ✅ **[DONE]**
- **Task 16** (`SCRUM-21`): Login DTO Records ✅ **[DONE - PR #30]**
- **Task 17** (`SCRUM-22`): Role DTO Records ✅ **[DONE - PR #31]**
- **Task 18** (`SCRUM-23`): Employee DTO Records ✅ **[DONE - PR #32]**
- **Task 19** (`SCRUM-24`): Site DTO Records ✅ **[DONE - PR #34]**
- **Task 20** (`SCRUM-25`): DraftShiftDto Record ✅ **[DONE - PR #35]**
- **Task 21** (`SCRUM-26`): PreFlightValidationResultDto Record ✅ **[DONE - PR #36]**
- **Task 22** (`SCRUM-27`): ClaimShiftDto Record ✅ **[DONE - PR #37]**
- **Task 23** (`SCRUM-28`): CreatePointsRuleDto Record ✅ **[DONE - PR #38]**
- **Task 24** (`SCRUM-29`): RewardItemDto Record ✅ **[DONE - PR #39]**
- **Task 25** (`SCRUM-30`): IScheduleValidationEngine Interface ✅ **[DONE - PR #40]**
- **Task 26** (`SCRUM-101`): EmployeeDocument Entity ✅ **[DONE - PR #45]**
- **Task 27** (`SCRUM-102`): DisciplinaryViolation Entity ✅ **[DONE - PR #47]**
- **Task 28** (`SCRUM-103`): PointsTransaction Entity ✅ **[DONE - PR #48]**
- **Task 29** (`SCRUM-104`): RewardRedemption Entity ✅ **[DONE - PR #49]**
- **Task 30** (`SCRUM-19`): IUnitOfWork Interface ✅ **[DONE - PR #44]**
- **Task 31** (`SCRUM-106`): UploadEmployeeDocumentDto Record ✅ **[DONE - PR #49]**
- **Task 32** (`SCRUM-107`): LogDisciplinaryViolationDto Record ✅ **[DONE - PR #50]**
- **Task 33** (`SCRUM-108`): PointsTransactionDto Record ✅ **[DONE - PR #51]**
- **Task 34** (`SCRUM-109`): RedeemRewardDto Record ✅ **[DONE - PR #53]**
- **Task 43** (`SCRUM-56`/`57`): TypeScript Data Models ✅ **[DONE - PR #3]**

---

## Active Backend Roadmap (Tasks 35-64)

---

### Phase 3: Infrastructure Layer (Persistence, Configurations & Services)

#### Task 35: `[Infrastructure] Create Buy2DbContext Class`
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Persistence/Buy2DbContext.cs`
- **Instructions**: Inherit from `DbContext`. Add `DbSet<T>` for all 13 domain entities. Override `OnModelCreating` to execute assembly configuration mapping.

#### Task 36: `[Infrastructure] Create EmployeeConfiguration Class`
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/EmployeeConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<Employee>`. Configure `FirstName` (`nvarchar(50)`), `LastName` (`nvarchar(50)`), `Email` (`varchar(150)`, unique index), `PhoneNumber` (`varchar(20)`).
- **Navigation Property Instructions**:
  - Configure one-to-many relationship to `JobRole` using `JobRoleId` foreign key with Restrict delete behavior.
  - Configure one-to-many relationship to `Role` using `RoleId` foreign key with Restrict delete behavior.
  - Configure one-to-many relationship to `Site` using `SiteId` foreign key with Restrict delete behavior.
  - Configure one-to-many relationship to `AttendanceProfile` using `AttendanceProfileId` foreign key with Restrict delete behavior.

#### Task 37: `[Infrastructure] Create RoleConfiguration Class`
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/RoleConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<Role>`. Configure `RoleName` (`nvarchar(50)`, unique index), `PermissionsJson` (`nvarchar(max)`).
- **Navigation Property Instruction**: Configure one-to-many relationship to `Employees` with Restrict delete behavior.

#### Task 38: `[Infrastructure] Create SiteConfiguration Class`
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/SiteConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<Site>`. Configure `SiteName` (`nvarchar(100)`), `Latitude` & `Longitude` (decimal 9,6), `MacAddressWhitelistJson` (`nvarchar(max)`).
- **Navigation Property Instructions**: Configure one-to-many relationships to `Employees` and `Shifts` with Restrict delete behavior.

#### Task 39: `[Infrastructure] Create ShiftConfiguration Class`
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/ShiftConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<Shift>`. Configure `StartTime` & `EndTime` (`datetimeoffset`), `IsPublished` (bool).
- **Navigation Property Instructions**:
  - Configure one-to-many relationship to `Employee` using `EmployeeId` foreign key with Restrict delete behavior.
  - Configure one-to-many relationship to `Site` using `SiteId` foreign key with Restrict delete behavior.
  - Configure one-to-many relationship to `JobRole` using `JobRoleId` foreign key with Restrict delete behavior.

#### Task 40: `[Infrastructure] Create ShiftClaimConfiguration Class`
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/ShiftClaimConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<ShiftClaim>`. Configure `Status` (`varchar(20)`), `OvertimeJustification` (`nvarchar(500)`).
- **Navigation Property Instructions**:
  - Configure relationship to `Shift` using `ShiftId` foreign key with Cascade delete behavior.
  - Configure relationship to `Employee` using `EmployeeId` foreign key with Restrict delete behavior.

#### Task 41: `[Infrastructure] Create EmployeeDocumentConfiguration Class`
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/EmployeeDocumentConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<EmployeeDocument>`. Configure `Category` (`nvarchar(50)`), `StorageUrl` (`varchar(500)`).
- **Navigation Property Instruction**: Configure relationship to `Employee` using `EmployeeId` foreign key with Cascade delete behavior.

#### Task 42: `[Infrastructure] Create DisciplinaryViolationConfiguration Class`
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/DisciplinaryViolationConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<DisciplinaryViolation>`. Configure `Severity` (`varchar(20)`), `Description` (`nvarchar(1000)`).
- **Navigation Property Instruction**: Configure relationship to `Employee` using `EmployeeId` foreign key with Cascade delete behavior.

#### Task 43: `[Infrastructure] Create PointsTransactionConfiguration Class`
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/PointsTransactionConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<PointsTransaction>`. Configure `Amount` (int), `TransactionType` (`varchar(30)`).
- **Navigation Property Instructions**:
  - Configure relationship to `Employee` using `EmployeeId` foreign key with Restrict delete behavior.
  - Configure optional relationship to `PointsRule` using `PointsRuleId` foreign key with SetNull delete behavior.

#### Task 44: `[Infrastructure] Create RewardRedemptionConfiguration Class`
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/RewardRedemptionConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<RewardRedemption>`. Configure `VoucherCode` (`varchar(100)`, unique index), `RedeemedAt` (`datetimeoffset`).
- **Navigation Property Instructions**:
  - Configure relationship to `Employee` using `EmployeeId` foreign key with Restrict delete behavior.
  - Configure relationship to `RewardItem` using `RewardItemId` foreign key with Restrict delete behavior.

#### Task 45: `[Infrastructure] Create GenericRepository Implementation`
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Persistence/Repositories/GenericRepository.cs`
- **Instructions**: Implement `IRepository<T>` interface using `Buy2DbContext`. Provide EF Core CRUD method implementations.

#### Task 46: `[Infrastructure] Create UnitOfWork Implementation`
- **Difficulty**: Easy
- **Location**: `Buy2.Infrastructure/Persistence/Repositories/UnitOfWork.cs`
- **Instructions**: Implement `IUnitOfWork` wrapping `Buy2DbContext.SaveChangesAsync()`.

#### Task 47: `[Infrastructure] Create JwtTokenGenerator Implementation` ✅ **[DONE]**
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Authentication/JwtTokenGenerator.cs`
- **Instructions**: Implement `IJwtTokenGenerator` using JWT security token handler. Generate user claims for id, email, and roles.

#### Task 48: `[Infrastructure] Create ScheduleValidationEngine Stub`
- **Difficulty**: Easy
- **Location**: `Buy2.Infrastructure/Services/ScheduleValidationEngine.cs`
- **Instructions**: Implement `IScheduleValidationEngine`. Return mock validation result.

#### Task 49: `[Infrastructure] Create ExcelVoucherParser Stub`
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Services/ExcelVoucherParser.cs`
- **Instructions**: Create class `ExcelVoucherParser` with method `List<string> ParseExcelCodes(Stream stream)` throwing `NotImplementedException`.

#### Task 50: `[Infrastructure] Create Infrastructure DependencyInjection Setup` ✅ **[DONE]**
- **Difficulty**: Easy
- **Location**: `Buy2.Infrastructure/DependencyInjection.cs`
- **Instructions**: Create static class `DependencyInjection` in `Buy2.Infrastructure`. Add extension method `AddInfrastructureServices` registering DbContext and repositories into DI container.

---

### Phase 4: Vertical Slice Endpoints (Command, Handler & Injected Controller in One PR)

#### Task 51: `[Feature Slice] Create Login Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Authentication/Login/LoginCommand.cs`
  - `src/Buy2.Api/Controllers/AuthLoginController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Authentication is the entry gateway. Before accessing HR features, users must prove identity and obtain a signed JWT token.
  - *Steps*:
    1. Define `LoginCommand(string Email, string Password) : IRequest<LoginResponseDto>`.
    2. In `LoginCommandHandler`:
       - Query employee by email using `IRepository<Employee>`. *Rationale: Ensures account exists.*
       - Verify password hash against stored hash. *Rationale: Security enforcement.*
       - Generate JWT bearer token via `IJwtTokenGenerator`. *Rationale: Provides stateless authentication token.*
       - Return `LoginResponseDto(token, employee)`.
    3. In `AuthLoginController`: Inject `ISender mediator`. Add `POST api/v1/auth/login` endpoint executing `await _mediator.Send(command)`.

#### Task 52: `[Feature Slice] Create Password Reset Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Authentication/ResetPassword/ResetPasswordCommand.cs`
  - `src/Buy2.Api/Controllers/AuthPasswordResetController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Enables self-service security recovery when a user forgets credentials or requires a password refresh.
  - *Steps*:
    1. Define `ResetPasswordCommand(string Email, string NewPassword) : IRequest<bool>`.
    2. In `ResetPasswordCommandHandler`:
       - Query employee by email. *Rationale: Locate target user account.*
       - Hash new password securely. *Rationale: Passwords must never be stored in plain text.*
       - Update employee record and commit via `IUnitOfWork`. *Rationale: Atomically persist security update.*
       - Return true on success.
    3. In `AuthPasswordResetController`: Inject `ISender mediator`. Add `POST api/v1/auth/password/reset` executing `await _mediator.Send(command)`.

#### Task 53: `[Feature Slice] Create Role Creation Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Roles/CreateRole/CreateRoleCommand.cs`
  - `src/Buy2.Api/Controllers/CreateRoleController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Role-based access control (RBAC) requires dynamically defining administrative and operational roles with specific permission scopes.
  - *Steps*:
    1. Define `CreateRoleCommand(string RoleName, List<string> Permissions) : IRequest<int>`.
    2. In `CreateRoleCommandHandler`:
       - Check if `RoleName` already exists. *Rationale: Prevents duplicate role definitions.*
       - Serialize permissions array into JSON string. *Rationale: Flexible string storage in database.*
       - Add `Role` entity via `IRepository<Role>` and commit via `IUnitOfWork`. *Rationale: Persist new role.*
       - Return `role.Id`.
    3. In `CreateRoleController`: Inject `ISender mediator`. Add `POST api/v1/roles` executing `await _mediator.Send(command)`.

#### Task 54: `[Feature Slice] Create Soft Delete Role Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Roles/DeleteRole/DeleteRoleCommand.cs`
  - `src/Buy2.Api/Controllers/DeleteRoleController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Deleting roles must preserve historical audit logs and prevent breaking existing employee references.
  - *Steps*:
    1. Define `DeleteRoleCommand(int RoleId) : IRequest<bool>`.
    2. In `DeleteRoleCommandHandler`:
       - Retrieve role by id. *Rationale: Verify role existence.*
       - Check if any active employee is assigned to this role. *Rationale: Prevents orphan employee references.*
       - Mark role as inactive / soft-deleted and save via `IUnitOfWork`. *Rationale: Safe preservation of audit logs.*
       - Return true.
    3. In `DeleteRoleController`: Inject `ISender mediator`. Add `DELETE api/v1/roles/{id}` executing `await _mediator.Send(command)`.

#### Task 55: `[Feature Slice] Create Employee Onboarding Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Employees/OnboardEmployee/OnboardEmployeeCommand.cs`
  - `src/Buy2.Api/Controllers/EmployeeOnboardingController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Onboarding registers new staff into HR system with assigned job roles, default work site, and initial attendance profiles.
  - *Steps*:
    1. Define `OnboardEmployeeCommand(string FirstName, string LastName, string Email, int JobRoleId, int SiteId) : IRequest<int>`.
    2. In `OnboardEmployeeCommandHandler`:
       - Check email uniqueness. *Rationale: Email is the unique user identifier.*
       - Verify assigned `JobRoleId` and `SiteId` exist. *Rationale: Enforces valid foreign key references.*
       - Instantiate `Employee` entity and commit via `IUnitOfWork`. *Rationale: Creates core workforce record.*
       - Return `employee.Id`.
    3. In `EmployeeOnboardingController`: Inject `ISender mediator`. Add `POST api/v1/employees/onboard` executing `await _mediator.Send(command)`.

#### Task 56: `[Feature Slice] Create Upload Employee Document Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Employees/UploadDocument/UploadEmployeeDocumentCommand.cs`
  - `src/Buy2.Api/Controllers/EmployeeDocumentsController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Employee compliance requires tracking identity documents, work permits, and contracts stored securely in blob storage.
  - *Steps*:
    1. Define `UploadEmployeeDocumentCommand(int EmployeeId, string Category, string StorageUrl) : IRequest<int>`.
    2. In `UploadEmployeeDocumentCommandHandler`:
       - Verify target employee exists. *Rationale: Prevent orphan document attachments.*
       - Instantiate `EmployeeDocument` entity with category and storage URL. *Rationale: Records cloud file metadata.*
       - Save via `IUnitOfWork` and return `document.Id`.
    3. In `EmployeeDocumentsController`: Inject `ISender mediator`. Add `POST api/v1/employees/{id}/documents` executing `await _mediator.Send(command)`.

#### Task 57: `[Feature Slice] Create Log Disciplinary Violation Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Employees/LogViolation/LogDisciplinaryViolationCommand.cs`
  - `src/Buy2.Api/Controllers/DisciplinaryViolationsController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Tracks workplace violations and compliance warnings for HR performance reviews and penalty point deductions.
  - *Steps*:
    1. Define `LogDisciplinaryViolationCommand(int EmployeeId, string Severity, string Description) : IRequest<int>`.
    2. In `LogDisciplinaryViolationCommandHandler`:
       - Verify employee exists. *Rationale: Ensures valid target staff.*
       - Instantiate `DisciplinaryViolation` record with severity level. *Rationale: Audit record creation.*
       - Save via `IUnitOfWork` and return `violation.Id`.
    3. In `DisciplinaryViolationsController`: Inject `ISender mediator`. Add `POST api/v1/employees/{id}/violations` executing `await _mediator.Send(command)`.

#### Task 58: `[Feature Slice] Create Site Creation Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Sites/CreateSite/CreateSiteCommand.cs`
  - `src/Buy2.Api/Controllers/CreateSiteController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Sites define physical branch locations with geofence coordinates and MAC address whitelists for mobile clock-in validation.
  - *Steps*:
    1. Define `CreateSiteCommand(string SiteName, decimal Latitude, decimal Longitude, List<string> MacWhitelist) : IRequest<int>`.
    2. In `CreateSiteCommandHandler`:
       - Instantiate `Site` entity with latitude/longitude bounds. *Rationale: Enables GPS geofence checks.*
       - Serialize MAC whitelist array to JSON string. *Rationale: Enables Wi-Fi clock-in validation.*
       - Save via `IUnitOfWork` and return `site.Id`.
    3. In `CreateSiteController`: Inject `ISender mediator`. Add `POST api/v1/sites` executing `await _mediator.Send(command)`.

#### Task 59: `[Feature Slice] Create Get Sites Endpoint (Query, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Sites/GetSites/GetSitesQuery.cs`
  - `src/Buy2.Api/Controllers/GetSitesController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Provides branch site listings for shift scheduling, employee site selection, and manager dropdowns.
  - *Steps*:
    1. Define `GetSitesQuery() : IRequest<List<SiteDto>>`.
    2. In `GetSitesQueryHandler`:
       - Query all active sites via `IRepository<Site>`. *Rationale: Retrieve active work locations.*
       - Map site entities to lightweight `SiteDto` list. *Rationale: Decouple domain entity from API response.*
       - Return list.
    3. In `GetSitesController`: Inject `ISender mediator`. Add `GET api/v1/sites` executing `await _mediator.Send(query)`.

#### Task 60: `[Feature Slice] Create Validate Draft Schedule Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Schedules/ValidateDraft/ValidateScheduleDraftCommand.cs`
  - `src/Buy2.Api/Controllers/ScheduleValidationController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Pre-flight schedule engine validates shift overlap, rest periods, and overtime rules BEFORE managers publish schedules.
  - *Steps*:
    1. Define `ValidateScheduleDraftCommand(List<DraftShiftDto> Shifts) : IRequest<PreFlightValidationResultDto>`.
    2. In `ValidateScheduleDraftCommandHandler`:
       - Pass draft shift list to `IScheduleValidationEngine.Validate()`. *Rationale: Evaluates compliance rules.*
       - Return `PreFlightValidationResultDto` containing warnings and error flags. *Rationale: Highlights scheduling conflicts to manager.*
    3. In `ScheduleValidationController`: Inject `ISender mediator`. Add `POST api/v1/schedules/validate-draft` executing `await _mediator.Send(command)`.

#### Task 61: `[Feature Slice] Create Get Open Shifts Market Endpoint (Query, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/ShiftMarket/GetOpenShifts/GetOpenShiftsQuery.cs`
  - `src/Buy2.Api/Controllers/OpenShiftsController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Shift Market enables eligible employees to view and claim unassigned open shifts for extra work hours.
  - *Steps*:
    1. Define `GetOpenShiftsQuery() : IRequest<List<ShiftDto>>`.
    2. In `GetOpenShiftsQueryHandler`:
       - Query published shifts where `EmployeeId == null` and `StartTime > DateTimeOffset.UtcNow`. *Rationale: Retrieves active open opportunities.*
       - Map to `ShiftDto` list and return. *Rationale: Formats data for UI market board.*
    3. In `OpenShiftsController`: Inject `ISender mediator`. Add `GET api/v1/shift-market/open-shifts` calling `await _mediator.Send(query)`.

#### Task 62: `[Feature Slice] Create Claim Shift Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/ShiftMarket/ClaimShift/ClaimShiftCommand.cs`
  - `src/Buy2.Api/Controllers/ShiftClaimsController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Handles employee shift claims, logging overtime justifications for manager approval.
  - *Steps*:
    1. Define `ClaimShiftCommand(int ShiftId, int EmployeeId, string OvertimeJustification) : IRequest<bool>`.
    2. In `ClaimShiftCommandHandler`:
       - Verify target shift is open and unassigned. *Rationale: Prevents double claiming.*
       - Create `ShiftClaim` record with status `Pending`. *Rationale: Creates claim audit record for manager review.*
       - Save via `IUnitOfWork` and return true.
    3. In `ShiftClaimsController`: Inject `ISender mediator`. Add `POST api/v1/shift-market/claims/{id}` calling `await _mediator.Send(command)`.

#### Task 63: `[Feature Slice] Create Points Rule Creation Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Points/CreateRule/CreatePointsRuleCommand.cs`
  - `src/Buy2.Api/Controllers/PointsRulesController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Gamification system rewards punctual attendance and extra shifts with points redeemable in the reward store.
  - *Steps*:
    1. Define `CreatePointsRuleCommand(string RuleName, int PointsValue, string TriggerType) : IRequest<int>`.
    2. In `CreatePointsRuleCommandHandler`:
       - Instantiate `PointsRule` entity with trigger criteria (e.g. `OnTimeClockIn`, `ClaimOpenShift`). *Rationale: Configures automation engine rule.*
       - Save via `IUnitOfWork` and return `rule.Id`.
    3. In `PointsRulesController`: Inject `ISender mediator`. Add `POST api/v1/points/rules` calling `await _mediator.Send(command)`.

#### Task 64: `[Feature Slice] Create Redeem Reward Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Rewards/RedeemReward/RedeemRewardCommand.cs`
  - `src/Buy2.Api/Controllers/RewardRedemptionController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Converts accumulated employee gamification points into digital voucher codes.
  - *Steps*:
    1. Define `RedeemRewardCommand(int RewardItemId, int EmployeeId) : IRequest<string>`.
    2. In `RedeemRewardCommandHandler`:
       - Check employee total points balance against reward cost. *Rationale: Ensures sufficient points balance.*
       - Reserve available voucher code from inventory. *Rationale: Prevents duplicate voucher distribution.*
       - Create `RewardRedemption` record and deduct points via `IUnitOfWork`. *Rationale: Atomically records transaction.*
       - Return voucher code string to employee.
    3. In `RewardRedemptionController`: Inject `ISender mediator`. Add `POST api/v1/rewards/{id}/redeem` calling `await _mediator.Send(command)`.
