# Available Tasks for Contributors - Buy2 HRMS Project

Welcome to the **Buy2 HR Management System (HRMS)** open-source repository!

This file breaks down the entire project into atomic, step-by-step tasks suitable for contributors of all skill levels.

---

## Git Branching & Commit Naming Guidelines

To ensure automatic tracking between GitHub and Jira, follow these simple naming rules:

1. **Git Branch Name**: Include the Jira Issue Key (e.g., `SCRUM-105`):
   - Example: `SCRUM-105-create-log-disciplinary-violation-dto` or `feature/SCRUM-105-log-disciplinary-violation-dto`
   - *Tip: Click "Create branch" inside your Jira task card to copy the exact branch name automatically.*

2. **Commit Message Format**:
   - Example: `SCRUM-105: [Application] Create LogDisciplinaryViolationDto Record`

3. **Pull Request (PR) Title**:
   - Example: `SCRUM-105: [Application] Create LogDisciplinaryViolationDto Record`

---

## Instructions for Team Members & Contributors

1. Pick an unassigned task from your **Jira Sprint Board**.
2. Create your working git branch including the Jira key (e.g., `SCRUM-105-log-disciplinary-violation-dto`).
3. Each task requires creating an **empty structure only**: a class, record DTO, enum, interface, DbContext configuration, or API controller.
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
- `controller`: API HTTP controller.

### Difficulty Levels
- **Very Easy**: Simple class/enum/interface with properties or empty signatures only.
- **Easy**: Small file with basic inheritance or simple dependency injection.
- **Medium**: EF Core configuration, repository pattern, or complex service/controller.

---

## Completed Tasks (Domain Layer & Application Contracts) ✅

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

## Active Backend Roadmap (Tasks 32-68)

---

### Phase 2: Application Layer (Remaining DTOs)

#### Task 32: `[Application] Create LogDisciplinaryViolationDto Record`
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/Employees/DTOs/ViolationDtos.cs`
- **Instructions**: Create record DTO for logging violations with positional parameters `EmployeeId`, `Severity`, and `Description`.

#### Task 33: `[Application] Create PointsTransactionDto Record`
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/Points/DTOs/PointsTransactionDtos.cs`
- **Instructions**: Create record DTO for points transactions with positional parameters `EmployeeId`, nullable `PointsRuleId`, `Amount`, and `TransactionType`.

#### Task 34: `[Application] Create RedeemRewardDto Record`
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/Rewards/DTOs/RedemptionDtos.cs`
- **Instructions**: Create record DTO for reward redemption with positional parameters `RewardItemId` and `EmployeeId`.

---

### Phase 3: Infrastructure Layer (Persistence, Configurations & Services)

#### Task 35: `[Infrastructure] Create Buy2DbContext Class`
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Buy2DbContext.cs`
- **Instructions**: Inherit from `DbContext`. Configure `DbSet<T>` for all domain entities. Override `OnModelCreating` to apply configurations from assembly.

#### Task 36: `[Infrastructure] Create EmployeeConfiguration Class`
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/EmployeeConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<Employee>`. Configure column specs: `FirstName` (required `nvarchar(50)`), `LastName` (required `nvarchar(50)`), `Email` (required `varchar(150)`, unique index), `PhoneNumber` (optional `varchar(20)`). Configure one-to-many relationships for `JobRole`, `Role`, `Site`, and `AttendanceProfile` with `DeleteBehavior.Restrict`.

#### Task 37: `[Infrastructure] Create RoleConfiguration Class`
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/RoleConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<Role>`. Configure column specs: `RoleName` (required `nvarchar(50)`, unique index), `PermissionsJson` (required `nvarchar(max)`).

#### Task 38: `[Infrastructure] Create SiteConfiguration Class`
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/SiteConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<Site>`. Configure column specs: `SiteName` (required `nvarchar(100)`), `Latitude` & `Longitude` (required decimal 9,6), `MacAddressWhitelistJson` (optional `nvarchar(max)`).

#### Task 39: `[Infrastructure] Create ShiftConfiguration Class`
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/ShiftConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<Shift>`. Configure column specs: `StartTime` & `EndTime` (required `datetimeoffset`), `IsPublished` (required bool, default false). Configure relationships for `Employee`, `Site`, and `JobRole` with `DeleteBehavior.Restrict`.

#### Task 40: `[Infrastructure] Create ShiftClaimConfiguration Class`
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/ShiftClaimConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<ShiftClaim>`. Configure column specs: `Status` (required `varchar(20)`), `OvertimeJustification` (optional `nvarchar(500)`). Configure relationships: `Shift` (`DeleteBehavior.Cascade`) and `Employee` (`DeleteBehavior.Restrict`).

#### Task 41: `[Infrastructure] Create EmployeeDocumentConfiguration Class`
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/EmployeeDocumentConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<EmployeeDocument>`. Configure column specs: `Category` (required `nvarchar(50)`), `StorageUrl` (required `varchar(500)`). Configure relationship to `Employee` with `DeleteBehavior.Cascade`.

#### Task 42: `[Infrastructure] Create DisciplinaryViolationConfiguration Class`
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/DisciplinaryViolationConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<DisciplinaryViolation>`. Configure column specs: `Severity` (required `varchar(20)`), `Description` (required `nvarchar(1000)`). Configure relationship to `Employee` with `DeleteBehavior.Cascade`.

#### Task 43: `[Infrastructure] Create PointsTransactionConfiguration Class`
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/PointsTransactionConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<PointsTransaction>`. Configure column specs: `Amount` (required int), `TransactionType` (required `varchar(30)`). Configure relationships: `Employee` (`DeleteBehavior.Restrict`) and optional `PointsRule` (`DeleteBehavior.SetNull`).

#### Task 44: `[Infrastructure] Create RewardRedemptionConfiguration Class`
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/RewardRedemptionConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<RewardRedemption>`. Configure column specs: `VoucherCode` (required `varchar(100)`, unique index), `RedeemedAt` (required `datetimeoffset`). Configure relationships for `Employee` and `RewardItem` with `DeleteBehavior.Restrict`.

#### Task 45: `[Infrastructure] Create GenericRepository Implementation`
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `repository`
- **Location**: `Buy2.Infrastructure/Persistence/Repositories/GenericRepository.cs`
- **Instructions**: Implement `IRepository<T>` using `Buy2DbContext`. Provide basic EF Core CRUD calls.

#### Task 46: `[Infrastructure] Create UnitOfWork Implementation`
- **Difficulty**: Easy
- **Labels**: `layer:infrastructure`, `repository`
- **Location**: `Buy2.Infrastructure/Persistence/Repositories/UnitOfWork.cs`
- **Instructions**: Implement `IUnitOfWork` wrapping `Buy2DbContext.SaveChangesAsync()`.

#### Task 47: `[Infrastructure] Create JwtTokenGenerator Implementation`
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `authentication`
- **Location**: `Buy2.Infrastructure/Authentication/JwtTokenGenerator.cs`
- **Instructions**: Implement `IJwtTokenGenerator` using `System.IdentityModel.Tokens.Jwt`.

#### Task 48: `[Infrastructure] Create ScheduleValidationEngine Stub`
- **Difficulty**: Easy
- **Labels**: `layer:infrastructure`, `service`
- **Location**: `Buy2.Infrastructure/Services/ScheduleValidationEngine.cs`
- **Instructions**: Implement `IScheduleValidationEngine`. Return mock `PreFlightValidationResultDto(true, new(), new())`.

#### Task 49: `[Infrastructure] Create ExcelVoucherParser Stub`
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `service`
- **Location**: `Buy2.Infrastructure/Services/ExcelVoucherParser.cs`
- **Instructions**: Create class `ExcelVoucherParser` with method `List<string> ParseExcelCodes(Stream stream)` throwing `NotImplementedException`.

#### Task 50: `[Infrastructure] Create Infrastructure DependencyInjection Setup`
- **Difficulty**: Easy
- **Labels**: `layer:infrastructure`, `service`
- **Location**: `Buy2.Infrastructure/DependencyInjection.cs`
- **Instructions**: Create extension method `AddInfrastructureServices` for `IServiceCollection` accepting `IConfiguration`. Register DbContext and repositories into DI container.

---

### Phase 4: API Layer Controllers (Single Endpoint per Controller)

#### Task 51: `[API] Create AuthLoginController`
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/AuthLoginController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/auth")]`. Add `POST login` endpoint stub accepting `LoginRequestDto`.

#### Task 52: `[API] Create AuthPasswordResetController`
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/AuthPasswordResetController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/auth")]`. Add `POST password/reset` endpoint stub.

#### Task 53: `[API] Create CreateRoleController`
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/CreateRoleController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/roles")]`. Add `POST` create role endpoint stub accepting `CreateRoleDto`.

#### Task 54: `[API] Create DeleteRoleController`
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/DeleteRoleController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/roles")]`. Add `DELETE {id}` soft delete endpoint stub.

#### Task 55: `[API] Create EmployeeOnboardingController`
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/EmployeeOnboardingController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/employees")]`. Add `POST onboard` endpoint stub accepting `OnboardEmployeeDto`.

#### Task 56: `[API] Create EmployeeAttendanceConfigController`
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/EmployeeAttendanceConfigController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/employees")]`. Add `PUT {id}/attendance-config` endpoint stub.

#### Task 57: `[API] Create EmployeeDocumentsController`
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/EmployeeDocumentsController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/employees")]`. Add `POST {id}/documents` endpoint stub accepting `UploadEmployeeDocumentDto`.

#### Task 58: `[API] Create DisciplinaryViolationsController`
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/DisciplinaryViolationsController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/employees")]`. Add `POST {id}/violations` endpoint stub accepting `LogDisciplinaryViolationDto`.

#### Task 59: `[API] Create CreateSiteController`
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/CreateSiteController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/sites")]`. Add `POST` create site endpoint stub accepting `CreateSiteDto`.

#### Task 60: `[API] Create GetSitesController`
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/GetSitesController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/sites")]`. Add `GET` all sites endpoint stub.

#### Task 61: `[API] Create ScheduleValidationController`
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/ScheduleValidationController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/schedules")]`. Add `POST validate-draft` endpoint stub.

#### Task 62: `[API] Create SchedulePublishController`
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/SchedulePublishController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/schedules")]`. Add `POST publish` endpoint stub.

#### Task 63: `[API] Create OpenShiftsController`
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/OpenShiftsController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/shift-market")]`. Add `GET open-shifts` endpoint stub.

#### Task 64: `[API] Create ShiftClaimsController`
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/ShiftClaimsController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/shift-market")]`. Add `POST claims/{id}` endpoint stub accepting `ClaimShiftDto`.

#### Task 65: `[API] Create CreateRewardController`
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/CreateRewardController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/rewards")]`. Add `POST` reward creation endpoint stub accepting `RewardItemDto`.

#### Task 66: `[API] Create RewardInventoryController`
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/RewardInventoryController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/rewards")]`. Add `POST {id}/inventory/upload` endpoint stub for bulk voucher Excel file.

#### Task 67: `[API] Create PointsRulesController`
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/PointsRulesController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/points")]`. Add `POST rules` endpoint stub accepting `CreatePointsRuleDto`.

#### Task 68: `[API] Create RewardRedemptionController`
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/RewardRedemptionController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/rewards")]`. Add `POST {id}/redeem` endpoint stub accepting `RedeemRewardDto`.
