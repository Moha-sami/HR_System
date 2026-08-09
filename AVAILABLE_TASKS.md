# Available Tasks for Contributors - Buy2 HRMS Project

Welcome to the **Buy2 HR Management System (HRMS)** open-source repository!

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

#### Task 47: `[Infrastructure] Create JwtTokenGenerator Implementation`
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

#### Task 50: `[Infrastructure] Create Infrastructure DependencyInjection Setup`
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
  - *Why*: Authentication is the entry gateway. Users must prove identity and obtain a signed JWT token before accessing HR features.
  - *Steps*:
    1. Define login command record with email and password parameters.
    2. In handler: Query employee by email to verify account existence, verify password hash for security, generate JWT token, and return login response DTO.
    3. In controller: Inject mediator sender and add HTTP POST login endpoint calling mediator.

#### Task 52: `[Feature Slice] Create Password Reset Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Authentication/ResetPassword/ResetPasswordCommand.cs`
  - `src/Buy2.Api/Controllers/AuthPasswordResetController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Enables self-service security recovery when a user forgets credentials or requires a password refresh.
  - *Steps*:
    1. Define password reset command record with email and new password.
    2. In handler: Query target employee by email, hash new password securely, update employee record via unit of work, and return success flag.
    3. In controller: Inject mediator sender and add HTTP POST password reset endpoint.

#### Task 53: `[Feature Slice] Create Role Creation Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Roles/CreateRole/CreateRoleCommand.cs`
  - `src/Buy2.Api/Controllers/CreateRoleController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Role-based access control requires dynamically defining administrative and operational roles with specific permission scopes.
  - *Steps*:
    1. Define create role command record with role name and permissions list.
    2. In handler: Check role name uniqueness to prevent duplicates, serialize permissions array to JSON string, save role entity via unit of work, and return new role id.
    3. In controller: Inject mediator sender and add HTTP POST role creation endpoint.

#### Task 54: `[Feature Slice] Create Soft Delete Role Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Roles/DeleteRole/DeleteRoleCommand.cs`
  - `src/Buy2.Api/Controllers/DeleteRoleController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Deleting roles must preserve historical audit logs and prevent breaking existing employee references.
  - *Steps*:
    1. Define delete role command record with role id.
    2. In handler: Retrieve role by id, ensure no active employees are assigned to prevent orphan references, mark role as inactive / soft-deleted, save via unit of work, and return true.
    3. In controller: Inject mediator sender and add HTTP DELETE endpoint.

#### Task 55: `[Feature Slice] Create Employee Onboarding Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Employees/OnboardEmployee/OnboardEmployeeCommand.cs`
  - `src/Buy2.Api/Controllers/EmployeeOnboardingController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Onboarding registers new staff into HR system with assigned job roles, default work site, and initial attendance profiles.
  - *Steps*:
    1. Define onboard employee command record with personal details, job role id, and site id.
    2. In handler: Check email uniqueness, verify JobRole and Site exist to enforce valid foreign keys, instantiate employee entity, save via unit of work, and return new employee id.
    3. In controller: Inject mediator sender and add HTTP POST onboarding endpoint.

#### Task 56: `[Feature Slice] Create Upload Employee Document Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Employees/UploadDocument/UploadEmployeeDocumentCommand.cs`
  - `src/Buy2.Api/Controllers/EmployeeDocumentsController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Employee compliance requires tracking identity documents, work permits, and contracts stored securely in cloud storage.
  - *Steps*:
    1. Define upload document command record with employee id, category, and storage URL.
    2. In handler: Verify target employee exists to prevent orphan documents, instantiate document record, save via unit of work, and return document id.
    3. In controller: Inject mediator sender and add HTTP POST document upload endpoint.

#### Task 57: `[Feature Slice] Create Log Disciplinary Violation Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Employees/LogViolation/LogDisciplinaryViolationCommand.cs`
  - `src/Buy2.Api/Controllers/DisciplinaryViolationsController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Tracks workplace violations and compliance warnings for HR performance reviews and penalty point deductions.
  - *Steps*:
    1. Define log violation command record with employee id, severity, and description.
    2. In handler: Verify employee exists, instantiate disciplinary violation record with severity level, save via unit of work, and return violation id.
    3. In controller: Inject mediator sender and add HTTP POST violation endpoint.

#### Task 58: `[Feature Slice] Create Site Creation Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Sites/CreateSite/CreateSiteCommand.cs`
  - `src/Buy2.Api/Controllers/CreateSiteController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Sites define physical branch locations with geofence coordinates and MAC address whitelists for mobile clock-in validation.
  - *Steps*:
    1. Define create site command record with name, latitude, longitude, and MAC whitelist.
    2. In handler: Instantiate site entity with coordinate bounds for GPS geofence checks, serialize MAC whitelist to JSON string for Wi-Fi clock-in validation, save via unit of work, and return site id.
    3. In controller: Inject mediator sender and add HTTP POST site creation endpoint.

#### Task 59: `[Feature Slice] Create Get Sites Endpoint (Query, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Sites/GetSites/GetSitesQuery.cs`
  - `src/Buy2.Api/Controllers/GetSitesController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Provides branch site listings for shift scheduling, employee site selection, and manager dropdowns.
  - *Steps*:
    1. Define get sites query record.
    2. In handler: Query all active sites via repository, map site entities to lightweight site DTO list, and return list.
    3. In controller: Inject mediator sender and add HTTP GET sites endpoint.

#### Task 60: `[Feature Slice] Create Validate Draft Schedule Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Schedules/ValidateDraft/ValidateScheduleDraftCommand.cs`
  - `src/Buy2.Api/Controllers/ScheduleValidationController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Pre-flight schedule engine validates shift overlap, rest periods, and overtime rules BEFORE managers publish schedules.
  - *Steps*:
    1. Define validate draft schedule command record with draft shifts list.
    2. In handler: Pass draft shifts list to schedule validation engine to evaluate compliance rules, and return pre-flight validation result containing warnings and conflict flags.
    3. In controller: Inject mediator sender and add HTTP POST validation endpoint.

#### Task 61: `[Feature Slice] Create Get Open Shifts Market Endpoint (Query, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/ShiftMarket/GetOpenShifts/GetOpenShiftsQuery.cs`
  - `src/Buy2.Api/Controllers/OpenShiftsController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Shift Market enables eligible employees to view and claim unassigned open shifts for extra work hours.
  - *Steps*:
    1. Define get open shifts query record.
    2. In handler: Query published shifts without an assigned employee and with future start time, map to shift DTO list, and return list.
    3. In controller: Inject mediator sender and add HTTP GET open shifts endpoint.

#### Task 62: `[Feature Slice] Create Claim Shift Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/ShiftMarket/ClaimShift/ClaimShiftCommand.cs`
  - `src/Buy2.Api/Controllers/ShiftClaimsController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Handles employee shift claims, logging overtime justifications for manager approval.
  - *Steps*:
    1. Define claim shift command record with shift id, employee id, and overtime justification.
    2. In handler: Verify target shift is open and unassigned to prevent double claiming, create shift claim record with Pending status, save via unit of work, and return true.
    3. In controller: Inject mediator sender and add HTTP POST claim endpoint.

#### Task 63: `[Feature Slice] Create Points Rule Creation Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Points/CreateRule/CreatePointsRuleCommand.cs`
  - `src/Buy2.Api/Controllers/PointsRulesController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Gamification system rewards punctual attendance and extra shifts with points redeemable in the reward store.
  - *Steps*:
    1. Define create points rule command record with rule name, points value, and trigger type.
    2. In handler: Instantiate points rule entity with trigger criteria (e.g. OnTimeClockIn, ClaimOpenShift), save via unit of work, and return rule id.
    3. In controller: Inject mediator sender and add HTTP POST points rule endpoint.

#### Task 64: `[Feature Slice] Create Redeem Reward Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Rewards/RedeemReward/RedeemRewardCommand.cs`
  - `src/Buy2.Api/Controllers/RewardRedemptionController.cs`
- **Thought Process & Business Logic Rationale**:
  - *Why*: Converts accumulated employee gamification points into digital voucher codes.
  - *Steps*:
    1. Define redeem reward command record with reward item id and employee id.
    2. In handler: Check employee total points balance against reward cost to ensure sufficient balance, reserve available voucher code from inventory to prevent duplicate distribution, create reward redemption record, deduct points via unit of work, and return voucher code string.
    3. In controller: Inject mediator sender and add HTTP POST redemption endpoint.
