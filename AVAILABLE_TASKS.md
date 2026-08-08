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
- **Instructions**: Inherit from `DbContext`. Add `DbSet<T>` for all domain entities. Override `OnModelCreating` to apply configurations from assembly.

#### Task 36: `[Infrastructure] Create EmployeeConfiguration Class`
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/EmployeeConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<Employee>`. Configure `FirstName` (`nvarchar(50)`), `LastName` (`nvarchar(50)`), `Email` (`varchar(150)`, unique index), `PhoneNumber` (`varchar(20)`). Configure relationships with `DeleteBehavior.Restrict`.

#### Task 37: `[Infrastructure] Create RoleConfiguration Class`
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/RoleConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<Role>`. Configure `RoleName` (`nvarchar(50)`, unique index), `PermissionsJson` (`nvarchar(max)`).

#### Task 38: `[Infrastructure] Create SiteConfiguration Class`
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/SiteConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<Site>`. Configure `SiteName` (`nvarchar(100)`), `Latitude` & `Longitude` (decimal 9,6), `MacAddressWhitelistJson` (`nvarchar(max)`).

#### Task 39: `[Infrastructure] Create ShiftConfiguration Class`
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/ShiftConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<Shift>`. Configure `StartTime` & `EndTime` (`datetimeoffset`), `IsPublished` (bool). Configure relationships with `DeleteBehavior.Restrict`.

#### Task 40: `[Infrastructure] Create ShiftClaimConfiguration Class`
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/ShiftClaimConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<ShiftClaim>`. Configure `Status` (`varchar(20)`), `OvertimeJustification` (`nvarchar(500)`). Configure relationships for `Shift` (`Cascade`) and `Employee` (`Restrict`).

#### Task 41: `[Infrastructure] Create EmployeeDocumentConfiguration Class`
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/EmployeeDocumentConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<EmployeeDocument>`. Configure `Category` (`nvarchar(50)`), `StorageUrl` (`varchar(500)`). Configure relationship to `Employee` (`DeleteBehavior.Cascade`).

#### Task 42: `[Infrastructure] Create DisciplinaryViolationConfiguration Class`
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/DisciplinaryViolationConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<DisciplinaryViolation>`. Configure `Severity` (`varchar(20)`), `Description` (`nvarchar(1000)`). Configure relationship to `Employee` (`DeleteBehavior.Cascade`).

#### Task 43: `[Infrastructure] Create PointsTransactionConfiguration Class`
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/PointsTransactionConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<PointsTransaction>`. Configure `Amount` (int), `TransactionType` (`varchar(30)`). Configure relationships for `Employee` (`Restrict`) and optional `PointsRule` (`SetNull`).

#### Task 44: `[Infrastructure] Create RewardRedemptionConfiguration Class`
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/RewardRedemptionConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<RewardRedemption>`. Configure `VoucherCode` (`varchar(100)`, unique index), `RedeemedAt` (`datetimeoffset`). Configure relationships with `DeleteBehavior.Restrict`.

#### Task 45: `[Infrastructure] Create GenericRepository Implementation`
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Persistence/Repositories/GenericRepository.cs`
- **Instructions**: Implement `IRepository<T>` using `Buy2DbContext`. Provide basic EF Core CRUD calls.

#### Task 46: `[Infrastructure] Create UnitOfWork Implementation`
- **Difficulty**: Easy
- **Location**: `Buy2.Infrastructure/Persistence/Repositories/UnitOfWork.cs`
- **Instructions**: Implement `IUnitOfWork` wrapping `Buy2DbContext.SaveChangesAsync()`.

#### Task 47: `[Infrastructure] Create JwtTokenGenerator Implementation`
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Authentication/JwtTokenGenerator.cs`
- **Instructions**: Implement `IJwtTokenGenerator` using `System.IdentityModel.Tokens.Jwt`.

#### Task 48: `[Infrastructure] Create ScheduleValidationEngine Stub`
- **Difficulty**: Easy
- **Location**: `Buy2.Infrastructure/Services/ScheduleValidationEngine.cs`
- **Instructions**: Implement `IScheduleValidationEngine` returning mock `PreFlightValidationResultDto(true, new(), new())`.

#### Task 49: `[Infrastructure] Create ExcelVoucherParser Stub`
- **Difficulty**: Medium
- **Location**: `Buy2.Infrastructure/Services/ExcelVoucherParser.cs`
- **Instructions**: Create class `ExcelVoucherParser` with method `List<string> ParseExcelCodes(Stream stream)` throwing `NotImplementedException`.

#### Task 50: `[Infrastructure] Create Infrastructure DependencyInjection Setup`
- **Difficulty**: Easy
- **Location**: `Buy2.Infrastructure/DependencyInjection.cs`
- **Instructions**: Create extension method `AddInfrastructureServices` registering DbContext and repositories into DI container.

---

### Phase 4: Vertical Slice Endpoints (Command, Handler & Injected Controller in One PR)

#### Task 51: `[Feature Slice] Create Login Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Authentication/Login/LoginCommand.cs`
  - `src/Buy2.Api/Controllers/AuthLoginController.cs`
- **Handler Business Logic**:
  1. Define `LoginCommand(string Email, string Password) : IRequest<LoginResponseDto>`.
  2. In `LoginCommandHandler`: Validate email exists via `IRepository<Employee>`, verify password hash, generate JWT token using `IJwtTokenGenerator`, return `LoginResponseDto(token, employee)`.
  3. In `AuthLoginController`: Inject `ISender mediator`. Add `POST api/v1/auth/login` endpoint calling `await _mediator.Send(command)`.

#### Task 52: `[Feature Slice] Create Password Reset Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Authentication/ResetPassword/ResetPasswordCommand.cs`
  - `src/Buy2.Api/Controllers/AuthPasswordResetController.cs`
- **Handler Business Logic**:
  1. Define `ResetPasswordCommand(string Email, string NewPassword) : IRequest<bool>`.
  2. In `ResetPasswordCommandHandler`: Query employee by email, hash new password, update employee record via `IUnitOfWork`, return true.
  3. In `AuthPasswordResetController`: Inject `ISender mediator`. Add `POST api/v1/auth/password/reset` calling `await _mediator.Send(command)`.

#### Task 53: `[Feature Slice] Create Role Creation Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Roles/CreateRole/CreateRoleCommand.cs`
  - `src/Buy2.Api/Controllers/CreateRoleController.cs`
- **Handler Business Logic**:
  1. Define `CreateRoleCommand(string RoleName, List<string> Permissions) : IRequest<int>`.
  2. In `CreateRoleCommandHandler`: Check role name uniqueness, map permissions to JSON string, add `Role` entity via `IRepository<Role>`, save via `IUnitOfWork`, return `role.Id`.
  3. In `CreateRoleController`: Inject `ISender mediator`. Add `POST api/v1/roles` calling `await _mediator.Send(command)`.

#### Task 54: `[Feature Slice] Create Soft Delete Role Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Roles/DeleteRole/DeleteRoleCommand.cs`
  - `src/Buy2.Api/Controllers/DeleteRoleController.cs`
- **Handler Business Logic**:
  1. Define `DeleteRoleCommand(int RoleId) : IRequest<bool>`.
  2. In `DeleteRoleCommandHandler`: Retrieve role by id, ensure no active employees assigned, soft delete / set inactive via `IUnitOfWork`, return true.
  3. In `DeleteRoleController`: Inject `ISender mediator`. Add `DELETE api/v1/roles/{id}` calling `await _mediator.Send(command)`.

#### Task 55: `[Feature Slice] Create Employee Onboarding Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Employees/OnboardEmployee/OnboardEmployeeCommand.cs`
  - `src/Buy2.Api/Controllers/EmployeeOnboardingController.cs`
- **Handler Business Logic**:
  1. Define `OnboardEmployeeCommand(string FirstName, string LastName, string Email, int JobRoleId, int SiteId) : IRequest<int>`.
  2. In `OnboardEmployeeCommandHandler`: Check email uniqueness, verify JobRole and Site exist, instantiate `Employee` entity, save via `IUnitOfWork`, return `employee.Id`.
  3. In `EmployeeOnboardingController`: Inject `ISender mediator`. Add `POST api/v1/employees/onboard` calling `await _mediator.Send(command)`.

#### Task 56: `[Feature Slice] Create Upload Employee Document Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Employees/UploadDocument/UploadEmployeeDocumentCommand.cs`
  - `src/Buy2.Api/Controllers/EmployeeDocumentsController.cs`
- **Handler Business Logic**:
  1. Define `UploadEmployeeDocumentCommand(int EmployeeId, string Category, string StorageUrl) : IRequest<int>`.
  2. In `UploadEmployeeDocumentCommandHandler`: Verify employee exists, instantiate `EmployeeDocument` entity, save via `IUnitOfWork`, return `document.Id`.
  3. In `EmployeeDocumentsController`: Inject `ISender mediator`. Add `POST api/v1/employees/{id}/documents` calling `await _mediator.Send(command)`.

#### Task 57: `[Feature Slice] Create Log Disciplinary Violation Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Employees/LogViolation/LogDisciplinaryViolationCommand.cs`
  - `src/Buy2.Api/Controllers/DisciplinaryViolationsController.cs`
- **Handler Business Logic**:
  1. Define `LogDisciplinaryViolationCommand(int EmployeeId, string Severity, string Description) : IRequest<int>`.
  2. In `LogDisciplinaryViolationCommandHandler`: Verify employee exists, instantiate `DisciplinaryViolation` entity, save via `IUnitOfWork`, return `violation.Id`.
  3. In `DisciplinaryViolationsController`: Inject `ISender mediator`. Add `POST api/v1/employees/{id}/violations` calling `await _mediator.Send(command)`.

#### Task 58: `[Feature Slice] Create Site Creation Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Sites/CreateSite/CreateSiteCommand.cs`
  - `src/Buy2.Api/Controllers/CreateSiteController.cs`
- **Handler Business Logic**:
  1. Define `CreateSiteCommand(string SiteName, decimal Latitude, decimal Longitude, List<string> MacWhitelist) : IRequest<int>`.
  2. In `CreateSiteCommandHandler`: Instantiate `Site` entity, serialize MAC whitelist to JSON string, save via `IUnitOfWork`, return `site.Id`.
  3. In `CreateSiteController`: Inject `ISender mediator`. Add `POST api/v1/sites` calling `await _mediator.Send(command)`.

#### Task 59: `[Feature Slice] Create Get Sites Endpoint (Query, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Sites/GetSites/GetSitesQuery.cs`
  - `src/Buy2.Api/Controllers/GetSitesController.cs`
- **Handler Business Logic**:
  1. Define `GetSitesQuery() : IRequest<List<SiteDto>>`.
  2. In `GetSitesQueryHandler`: Query all active sites via `IRepository<Site>`, map entities to `SiteDto` list, return list.
  3. In `GetSitesController`: Inject `ISender mediator`. Add `GET api/v1/sites` calling `await _mediator.Send(query)`.

#### Task 60: `[Feature Slice] Create Validate Draft Schedule Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Schedules/ValidateDraft/ValidateScheduleDraftCommand.cs`
  - `src/Buy2.Api/Controllers/ScheduleValidationController.cs`
- **Handler Business Logic**:
  1. Define `ValidateScheduleDraftCommand(List<DraftShiftDto> Shifts) : IRequest<PreFlightValidationResultDto>`.
  2. In `ValidateScheduleDraftCommandHandler`: Pass shift list to `IScheduleValidationEngine.Validate()`, return `PreFlightValidationResultDto`.
  3. In `ScheduleValidationController`: Inject `ISender mediator`. Add `POST api/v1/schedules/validate-draft` calling `await _mediator.Send(command)`.

#### Task 61: `[Feature Slice] Create Get Open Shifts Market Endpoint (Query, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/ShiftMarket/GetOpenShifts/GetOpenShiftsQuery.cs`
  - `src/Buy2.Api/Controllers/OpenShiftsController.cs`
- **Handler Business Logic**:
  1. Define `GetOpenShiftsQuery() : IRequest<List<ShiftDto>>`.
  2. In `GetOpenShiftsQueryHandler`: Query published shifts without assigned employee, map to `ShiftDto` list, return list.
  3. In `OpenShiftsController`: Inject `ISender mediator`. Add `GET api/v1/shift-market/open-shifts` calling `await _mediator.Send(query)`.

#### Task 62: `[Feature Slice] Create Claim Shift Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/ShiftMarket/ClaimShift/ClaimShiftCommand.cs`
  - `src/Buy2.Api/Controllers/ShiftClaimsController.cs`
- **Handler Business Logic**:
  1. Define `ClaimShiftCommand(int ShiftId, int EmployeeId, string OvertimeJustification) : IRequest<bool>`.
  2. In `ClaimShiftCommandHandler`: Verify shift is open, instantiate `ShiftClaim` with status `Pending`, save via `IUnitOfWork`, return true.
  3. In `ShiftClaimsController`: Inject `ISender mediator`. Add `POST api/v1/shift-market/claims/{id}` calling `await _mediator.Send(command)`.

#### Task 63: `[Feature Slice] Create Points Rule Creation Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Points/CreateRule/CreatePointsRuleCommand.cs`
  - `src/Buy2.Api/Controllers/PointsRulesController.cs`
- **Handler Business Logic**:
  1. Define `CreatePointsRuleCommand(string RuleName, int PointsValue, string TriggerType) : IRequest<int>`.
  2. In `CreatePointsRuleCommandHandler`: Instantiate `PointsRule` entity, save via `IUnitOfWork`, return `rule.Id`.
  3. In `PointsRulesController`: Inject `ISender mediator`. Add `POST api/v1/points/rules` calling `await _mediator.Send(command)`.

#### Task 64: `[Feature Slice] Create Redeem Reward Endpoint (Command, Handler & Controller)`
- **Locations**:
  - `src/Buy2.Application/Features/Rewards/RedeemReward/RedeemRewardCommand.cs`
  - `src/Buy2.Api/Controllers/RewardRedemptionController.cs`
- **Handler Business Logic**:
  1. Define `RedeemRewardCommand(int RewardItemId, int EmployeeId) : IRequest<string>`.
  2. In `RedeemRewardCommandHandler`: Check employee points balance, deduct points, reserve voucher code from inventory, create `RewardRedemption` record via `IUnitOfWork`, return voucher code string.
  3. In `RewardRedemptionController`: Inject `ISender mediator`. Add `POST api/v1/rewards/{id}/redeem` calling `await _mediator.Send(command)`.
