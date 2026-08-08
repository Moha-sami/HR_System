# Available Tasks for Contributors - Buy2 HRMS Project

Welcome to the **Buy2 HR Management System (HRMS)** open-source repository!

This file breaks down the entire project into atomic, step-by-step tasks suitable for contributors of all skill levels.

---

## Git Branching & Commit Naming Guidelines

To ensure automatic tracking between GitHub and Jira, follow these simple naming rules:

1. **Git Branch Name**: Include the Jira Issue Key (e.g., `SCRUM-105`):
   - Example: `SCRUM-105-create-login-command-and-handler` or `feature/SCRUM-105-login-command`
   - *Tip: Click "Create branch" inside your Jira task card to copy the exact branch name automatically.*

2. **Commit Message Format**:
   - Example: `SCRUM-105: [Application] Create LoginCommand and Handler`

3. **Pull Request (PR) Title**:
   - Example: `SCRUM-105: [Application] Create LoginCommand and Handler`

---

## Instructions for Team Members & Contributors

1. Pick an unassigned task from your **Jira Sprint Board**.
2. Create your working git branch including the Jira key (e.g., `SCRUM-105-login-command`).
3. Each task requires creating a **clean structure only**: a class, record DTO, MediatR command/handler, DbContext configuration, or API controller.
4. Methods should only have signatures throwing `NotImplementedException` or returning default values. No complex business logic is needed in initial stubs!
5. Submit your **Pull Request directly targeting `main`**. Once merged by the lead, Jira automatically updates your task to **Done**!

---

## Labels & Difficulty Overview

### Suggested GitHub Labels
- `good first issue`: Ideal for beginners. Very small and safe.
- `layer:domain`: Code belongs in `Buy2.Domain`.
- `layer:application`: Code belongs in `Buy2.Application`.
- `layer:infrastructure`: Code belongs in `Buy2.Infrastructure`.
- `layer:api`: Code belongs in `Buy2.Api`.
- `entity`: Entity model class.
- `enum`: Named options enumeration.
- `dto`: Data transfer object record.
- `interface`: Contract definition.
- `mediatr`: MediatR Command or Query + Handler.
- `controller`: API HTTP controller.

---

## Completed Tasks (Domain Layer & Core Contracts) ✅

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
- **Task 31** (`SCRUM-106`): UploadEmployeeDocumentDto Record ✅ **[DONE]**
- **Task 43** (`SCRUM-56`/`57`): TypeScript Data Models ✅ **[DONE - PR #3]**

---

## Active Backend Roadmap (Tasks 32-78)

---

### Phase 2: Application Layer (Remaining DTOs)

#### Task 32: `[Application] Create LogDisciplinaryViolationDto Record`
- **Difficulty**: Very Easy
- **Location**: `Buy2.Application/DTOs/Employees/ViolationDtos.cs`
- **Instructions**: Define record DTO for logging violations with positional parameters: `int EmployeeId`, `string Severity`, `string Description`.

#### Task 33: `[Application] Create PointsTransactionDto Record`
- **Difficulty**: Very Easy
- **Location**: `Buy2.Application/DTOs/Points/PointsTransactionDtos.cs`
- **Instructions**: Define record DTO for points transactions with positional parameters: `int EmployeeId`, `int? PointsRuleId`, `int Amount`, `string TransactionType`.

#### Task 34: `[Application] Create RedeemRewardDto Record`
- **Difficulty**: Very Easy
- **Location**: `Buy2.Application/DTOs/Rewards/RedemptionDtos.cs`
- **Instructions**: Define record DTO for reward redemption with positional parameters: `int RewardItemId`, `int EmployeeId`.

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

### Phase 4: MediatR Commands/Queries & Injected API Controllers (Feature Slices)

#### Task 51: `[Application] Create LoginCommand and Handler`
- **Location**: `Buy2.Application/Features/Authentication/Login/LoginCommand.cs`
- **Instructions**: Define `LoginCommand` record implementing `IRequest<LoginResponseDto>`. Implement `LoginCommandHandler` consuming `IUnitOfWork` and `IJwtTokenGenerator`.

#### Task 52: `[API] Create AuthLoginController (MediatR Injected)`
- **Location**: `Buy2.Api/Controllers/AuthLoginController.cs`
- **Instructions**: Create `AuthLoginController` at `api/v1/auth`. Inject `ISender mediator`. Add HTTP POST `login` endpoint executing `_mediator.Send(command)`.

#### Task 53: `[Application] Create ResetPasswordCommand and Handler`
- **Location**: `Buy2.Application/Features/Authentication/ResetPassword/ResetPasswordCommand.cs`
- **Instructions**: Define `ResetPasswordCommand` record implementing `IRequest<bool>`. Implement `ResetPasswordCommandHandler`.

#### Task 54: `[API] Create AuthPasswordResetController (MediatR Injected)`
- **Location**: `Buy2.Api/Controllers/AuthPasswordResetController.cs`
- **Instructions**: Create `AuthPasswordResetController` at `api/v1/auth`. Inject `ISender mediator`. Add HTTP POST `password/reset` endpoint executing `_mediator.Send(command)`.

#### Task 55: `[Application] Create CreateRoleCommand and Handler`
- **Location**: `Buy2.Application/Features/Roles/CreateRole/CreateRoleCommand.cs`
- **Instructions**: Define `CreateRoleCommand` record implementing `IRequest<int>`. Implement `CreateRoleCommandHandler` consuming `IUnitOfWork`.

#### Task 56: `[API] Create CreateRoleController (MediatR Injected)`
- **Location**: `Buy2.Api/Controllers/CreateRoleController.cs`
- **Instructions**: Create `CreateRoleController` at `api/v1/roles`. Inject `ISender mediator`. Add HTTP POST endpoint executing `_mediator.Send(command)`.

#### Task 57: `[Application] Create DeleteRoleCommand and Handler`
- **Location**: `Buy2.Application/Features/Roles/DeleteRole/DeleteRoleCommand.cs`
- **Instructions**: Define `DeleteRoleCommand(int RoleId)` record implementing `IRequest<bool>`. Implement `DeleteRoleCommandHandler`.

#### Task 58: `[API] Create DeleteRoleController (MediatR Injected)`
- **Location**: `Buy2.Api/Controllers/DeleteRoleController.cs`
- **Instructions**: Create `DeleteRoleController` at `api/v1/roles`. Inject `ISender mediator`. Add HTTP DELETE `{id}` endpoint executing `_mediator.Send(command)`.

#### Task 59: `[Application] Create OnboardEmployeeCommand and Handler`
- **Location**: `Buy2.Application/Features/Employees/OnboardEmployee/OnboardEmployeeCommand.cs`
- **Instructions**: Define `OnboardEmployeeCommand` record implementing `IRequest<int>`. Implement `OnboardEmployeeCommandHandler`.

#### Task 60: `[API] Create EmployeeOnboardingController (MediatR Injected)`
- **Location**: `Buy2.Api/Controllers/EmployeeOnboardingController.cs`
- **Instructions**: Create `EmployeeOnboardingController` at `api/v1/employees`. Inject `ISender mediator`. Add HTTP POST `onboard` endpoint executing `_mediator.Send(command)`.

#### Task 61: `[Application] Create UploadEmployeeDocumentCommand and Handler`
- **Location**: `Buy2.Application/Features/Employees/UploadDocument/UploadEmployeeDocumentCommand.cs`
- **Instructions**: Define `UploadEmployeeDocumentCommand` record implementing `IRequest<int>`. Implement `UploadEmployeeDocumentCommandHandler`.

#### Task 62: `[API] Create EmployeeDocumentsController (MediatR Injected)`
- **Location**: `Buy2.Api/Controllers/EmployeeDocumentsController.cs`
- **Instructions**: Create `EmployeeDocumentsController` at `api/v1/employees`. Inject `ISender mediator`. Add HTTP POST `{id}/documents` endpoint executing `_mediator.Send(command)`.

#### Task 63: `[Application] Create LogDisciplinaryViolationCommand and Handler`
- **Location**: `Buy2.Application/Features/Employees/LogViolation/LogDisciplinaryViolationCommand.cs`
- **Instructions**: Define `LogDisciplinaryViolationCommand` record implementing `IRequest<int>`. Implement `LogDisciplinaryViolationCommandHandler`.

#### Task 64: `[API] Create DisciplinaryViolationsController (MediatR Injected)`
- **Location**: `Buy2.Api/Controllers/DisciplinaryViolationsController.cs`
- **Instructions**: Create `DisciplinaryViolationsController` at `api/v1/employees`. Inject `ISender mediator`. Add HTTP POST `{id}/violations` endpoint executing `_mediator.Send(command)`.

#### Task 65: `[Application] Create CreateSiteCommand and Handler`
- **Location**: `Buy2.Application/Features/Sites/CreateSite/CreateSiteCommand.cs`
- **Instructions**: Define `CreateSiteCommand` record implementing `IRequest<int>`. Implement `CreateSiteCommandHandler`.

#### Task 66: `[API] Create CreateSiteController (MediatR Injected)`
- **Location**: `Buy2.Api/Controllers/CreateSiteController.cs`
- **Instructions**: Create `CreateSiteController` at `api/v1/sites`. Inject `ISender mediator`. Add HTTP POST endpoint executing `_mediator.Send(command)`.

#### Task 67: `[Application] Create GetSitesQuery and Handler`
- **Location**: `Buy2.Application/Features/Sites/GetSites/GetSitesQuery.cs`
- **Instructions**: Define `GetSitesQuery` record implementing `IRequest<List<SiteDto>>`. Implement `GetSitesQueryHandler`.

#### Task 68: `[API] Create GetSitesController (MediatR Injected)`
- **Location**: `Buy2.Api/Controllers/GetSitesController.cs`
- **Instructions**: Create `GetSitesController` at `api/v1/sites`. Inject `ISender mediator`. Add HTTP GET endpoint executing `_mediator.Send(query)`.

#### Task 69: `[Application] Create ValidateScheduleDraftCommand and Handler`
- **Location**: `Buy2.Application/Features/Schedules/ValidateDraft/ValidateScheduleDraftCommand.cs`
- **Instructions**: Define `ValidateScheduleDraftCommand` record implementing `IRequest<PreFlightValidationResultDto>`. Implement Handler consuming `IScheduleValidationEngine`.

#### Task 70: `[API] Create ScheduleValidationController (MediatR Injected)`
- **Location**: `Buy2.Api/Controllers/ScheduleValidationController.cs`
- **Instructions**: Create `ScheduleValidationController` at `api/v1/schedules`. Inject `ISender mediator`. Add HTTP POST `validate-draft` endpoint executing `_mediator.Send(command)`.

#### Task 71: `[Application] Create GetOpenShiftsQuery and Handler`
- **Location**: `Buy2.Application/Features/ShiftMarket/GetOpenShifts/GetOpenShiftsQuery.cs`
- **Instructions**: Define `GetOpenShiftsQuery` record implementing `IRequest<List<ShiftDto>>`. Implement `GetOpenShiftsQueryHandler`.

#### Task 72: `[API] Create OpenShiftsController (MediatR Injected)`
- **Location**: `Buy2.Api/Controllers/OpenShiftsController.cs`
- **Instructions**: Create `OpenShiftsController` at `api/v1/shift-market`. Inject `ISender mediator`. Add HTTP GET `open-shifts` endpoint executing `_mediator.Send(query)`.

#### Task 73: `[Application] Create ClaimShiftCommand and Handler`
- **Location**: `Buy2.Application/Features/ShiftMarket/ClaimShift/ClaimShiftCommand.cs`
- **Instructions**: Define `ClaimShiftCommand(int ShiftId, int EmployeeId, string Justification)` record implementing `IRequest<bool>`. Implement `ClaimShiftCommandHandler`.

#### Task 74: `[API] Create ShiftClaimsController (MediatR Injected)`
- **Location**: `Buy2.Api/Controllers/ShiftClaimsController.cs`
- **Instructions**: Create `ShiftClaimsController` at `api/v1/shift-market`. Inject `ISender mediator`. Add HTTP POST `claims/{id}` endpoint executing `_mediator.Send(command)`.

#### Task 75: `[Application] Create CreatePointsRuleCommand and Handler`
- **Location**: `Buy2.Application/Features/Points/CreateRule/CreatePointsRuleCommand.cs`
- **Instructions**: Define `CreatePointsRuleCommand` record implementing `IRequest<int>`. Implement `CreatePointsRuleCommandHandler`.

#### Task 76: `[API] Create PointsRulesController (MediatR Injected)`
- **Location**: `Buy2.Api/Controllers/PointsRulesController.cs`
- **Instructions**: Create `PointsRulesController` at `api/v1/points`. Inject `ISender mediator`. Add HTTP POST `rules` endpoint executing `_mediator.Send(command)`.

#### Task 77: `[Application] Create RedeemRewardCommand and Handler`
- **Location**: `Buy2.Application/Features/Rewards/RedeemReward/RedeemRewardCommand.cs`
- **Instructions**: Define `RedeemRewardCommand(int RewardItemId, int EmployeeId)` record implementing `IRequest<string>`. Implement `RedeemRewardCommandHandler`.

#### Task 78: `[API] Create RewardRedemptionController (MediatR Injected)`
- **Location**: `Buy2.Api/Controllers/RewardRedemptionController.cs`
- **Instructions**: Create `RewardRedemptionController` at `api/v1/rewards`. Inject `ISender mediator`. Add HTTP POST `{id}/redeem` endpoint executing `_mediator.Send(command)`.
