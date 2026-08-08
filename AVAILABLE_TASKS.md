# Available Tasks for Contributors - Buy2 HRMS Project

Welcome to the **Buy2 HR Management System (HRMS)** open-source repository!

This file breaks down the entire project into atomic, step-by-step tasks suitable for contributors of all skill levels.

---

## Git Branching & Commit Naming Guidelines

To ensure automatic tracking between GitHub and Jira, follow these simple naming rules:

1. **Git Branch Name**: Include the Jira Issue Key (e.g., `SCRUM-6`):
   - Example: `SCRUM-6-create-base-entity` or `feature/SCRUM-6-base-entity`
   - *Tip: Click "Create branch" inside your Jira task card to copy the exact branch name automatically.*

2. **Commit Message Format**:
   - Example: `SCRUM-6: Create BaseEntity class`

3. **Pull Request (PR) Title**:
   - Example: `SCRUM-6: Create BaseEntity Class`

---

## Instructions for Team Members & Contributors

1. Pick an unassigned task from your **Jira Sprint Board** (e.g., `SCRUM-6`).
2. Create your working git branch including the Jira key (e.g., `SCRUM-6-base-entity`).
3. Each task requires creating an **empty structure only**: a class, record DTO, enum, interface, DbContext configuration, or Angular component.
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
- `layer:frontend`: Code belongs in `Buy2.Frontend` (Angular).
- `entity`: Entity model class.
- `enum`: Named options enumeration.
- `dto`: Data transfer object record.
- `interface`: Contract definition.
- `controller`: API HTTP controller.
- `component`: Angular component.

### Difficulty Levels
- **Very Easy**: Simple class/enum/interface with properties or empty signatures only.
- **Easy**: Small file with basic inheritance or simple dependency injection.
- **Medium**: EF Core configuration, repository pattern, or complex Angular service/component.

---

## Phase 1: Domain Layer (Tasks 1-12, 51-54)

Focus on core domain models, enums, navigation properties, and base entities. No database dependencies or framework logic.

### Task 1: Create BaseEntity Class ✅ **[DONE - PR #1]**
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `Buy2.Domain/Entities/BaseEntity.cs`
- **Instructions**: Create an abstract class named `BaseEntity`. Add an integer `Id` property and a `CreatedAt` (`DateTimeOffset`) property.

### Task 2: Create Role Entity ✅ **[DONE - PR #10]**
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `Buy2.Domain/Entities/Role.cs`
- **Instructions**: Create a public class `Role` inheriting from `BaseEntity`. Add `RoleName` (`string`), `PermissionsJson` (`string`), and virtual navigation collection property `Employees`.

### Task 3: Create JobRole Entity ✅ **[DONE - PR #9]**
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `Buy2.Domain/Entities/JobRole.cs`
- **Instructions**: Create class `JobRole` inheriting from `BaseEntity`. Add `Title` (`string`), `DepartmentId` (`int`), `RequiredQualificationsJson` (`string`), and virtual navigation collection properties for `Employees` and `Shifts`.

### Task 4: Create Employee Entity ✅ **[DONE - PR #11]**
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `Buy2.Domain/Entities/Employee.cs`
- **Instructions**: Create class `Employee` inheriting from `BaseEntity`. Add primitive properties (`FirstName`, `LastName`, `Email`, `PhoneNumber`, `JobRoleId`, `RoleId`, `SiteId`, `AttendanceProfileId`) and virtual navigation properties for `JobRole`, `Role`, `Site`, `AttendanceProfile`, and collections for `Shifts`, `ShiftClaims`, `Documents`, `DisciplinaryViolations`, `PointsTransactions`, and `RewardRedemptions`.

### Task 5: Create Site Entity ✅ **[DONE - PR #12]**
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `Buy2.Domain/Entities/Site.cs`
- **Instructions**: Create class `Site` inheriting from `BaseEntity`. Add `SiteName`, `Latitude` (`double`), `Longitude` (`double`), `MacAddressWhitelistJson` (`string`), and virtual navigation collection properties for `Employees` and `Shifts`.

### Task 6: Create AttendanceProfile Entity ✅ **[DONE - PR #16]**
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `Buy2.Domain/Entities/AttendanceProfile.cs`
- **Instructions**: Create class `AttendanceProfile` inheriting from `BaseEntity`. Add `ProfileName`, `ExpectedClockIn` (`TimeSpan`), `ExpectedClockOut` (`TimeSpan`), `RequiredWorkHours` (`double`), and virtual navigation collection property `Employees`.

### Task 7: Create Shift Entity ✅ **[DONE - PR #17]**
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `Buy2.Domain/Entities/Shift.cs`
- **Instructions**: Create class `Shift` inheriting from `BaseEntity`. Add primitive FK properties (`EmployeeId`, `SiteId`, `JobRoleId`), schedule properties (`StartTime`, `EndTime`, `IsPublished`), virtual navigation properties (`Employee`, `Site`, `JobRole`), and collection property `ShiftClaims`.

### Task 8: Create ShiftClaim Entity ✅ **[DONE - PR #26]**
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `Buy2.Domain/Entities/ShiftClaim.cs`
- **Instructions**: Create class `ShiftClaim` inheriting from `BaseEntity`. Add `ShiftId` (`int`), `EmployeeId` (`int`), `Status` (`string`), `OvertimeJustification` (`string`), and virtual navigation properties for `Shift` and `Employee`.

### Task 9: Create PointsRule Entity ✅ **[DONE - PR #23]**
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `Buy2.Domain/Entities/PointsRule.cs`
- **Instructions**: Create class `PointsRule` inheriting from `BaseEntity`. Add `RuleKey`, `EventType`, `ConditionExpression`, `ActionType`, `PointValue` (`int`), and virtual navigation collection property `PointsTransactions`.

### Task 10: Create RewardItem Entity ✅ **[DONE - PR #24]**
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `Buy2.Domain/Entities/RewardItem.cs`
- **Instructions**: Create class `RewardItem` inheriting from `BaseEntity`. Add `RewardName`, `CostInPoints` (`int`), `AvailableStock` (`int`), and virtual navigation collection property `RewardRedemptions`.

### Task 11: Create Gender and SalaryType Enums ✅ **[DONE - PR #18]**
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:domain`, `enum`
- **Location**: `Buy2.Domain/Enums/DomainEnums.cs`
- **Instructions**: Create public enums `Gender` (`Male = 1, Female = 2`) and `SalaryType` (`Fixed = 1, Hourly = 2`).

### Task 12: Create ShiftStatus and ClaimStatus Enums ✅ **[DONE - PR #19]**
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:domain`, `enum`
- **Location**: `Buy2.Domain/Enums/ShiftEnums.cs`
- **Instructions**: Create public enums `ShiftStatus` (`Draft = 0, Published = 1, Cancelled = 2`) and `ClaimStatus` (`Pending = 0, Approved = 1, Rejected = 2`).

### Task 51: Create EmployeeDocument Entity
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `Buy2.Domain/Entities/EmployeeDocument.cs`
- **Instructions**: Create class `EmployeeDocument` inheriting from `BaseEntity`. Add `EmployeeId` (`int`), `Category` (`string`), `StorageUrl` (`string`), and virtual navigation property `Employee`.

### Task 52: Create DisciplinaryViolation Entity
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `Buy2.Domain/Entities/DisciplinaryViolation.cs`
- **Instructions**: Create class `DisciplinaryViolation` inheriting from `BaseEntity`. Add `EmployeeId` (`int`), `Severity` (`string`), `Description` (`string`), and virtual navigation property `Employee`.

### Task 53: Create PointsTransaction Entity
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `Buy2.Domain/Entities/PointsTransaction.cs`
- **Instructions**: Create class `PointsTransaction` inheriting from `BaseEntity`. Add `EmployeeId` (`int`), `PointsRuleId` (`int?`), `Amount` (`int`), `TransactionType` (`string`), and virtual navigation properties for `Employee` and optional `PointsRule`.

### Task 54: Create RewardRedemption Entity
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `Buy2.Domain/Entities/RewardRedemption.cs`
- **Instructions**: Create class `RewardRedemption` inheriting from `BaseEntity`. Add `RewardItemId` (`int`), `EmployeeId` (`int`), `VoucherCode` (`string`), `RedeemedAt` (`DateTimeOffset`), and virtual navigation properties for `Employee` and `RewardItem`.

---

## Phase 2: Application Layer (Tasks 13-25, 55-58)

Application interfaces, DTO records, and CQRS contracts.

### Task 13: Create IRepository Generic Interface ✅ **[DONE - PR #41]**
- **Difficulty**: Medium
- **Labels**: `good first issue`, `layer:application`, `interface`
- **Location**: `Buy2.Application/Common/Interfaces/IRepository.cs`
- **Instructions**: Create generic interface `IRepository<T>` where `T : BaseEntity`. Add method signatures: `GetByIdAsync`, `GetAllAsync`, `AddAsync`, `Update`, `Delete`.

### Task 14: Create IUnitOfWork Interface
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:application`, `interface`
- **Location**: `Buy2.Application/Common/Interfaces/IUnitOfWork.cs`
- **Instructions**: Create interface `IUnitOfWork` with `SaveChangesAsync(CancellationToken cancellationToken = default)` signature.

### Task 15: Create IJwtTokenGenerator Interface ✅ **[DONE - PR #29]**
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:application`, `interface`
- **Location**: `Buy2.Application/Common/Interfaces/IJwtTokenGenerator.cs`
- **Instructions**: Create interface `IJwtTokenGenerator` with method signature `string GenerateToken(int userId, string email, string role)`.

### Task 16: Create Login DTO Records ✅ **[DONE - PR #30]**
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/Auth/DTOs/AuthDtos.cs`
- **Instructions**: Create record DTOs `LoginRequestDto(string Email, string Password)` and `LoginResponseDto(string Token, int ExpiresIn, string Role)`.

### Task 17: Create Role DTO Records ✅ **[DONE - PR #31]**
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/Roles/DTOs/RoleDtos.cs`
- **Instructions**: Create record DTO `CreateRoleDto(string RoleName, Dictionary<string, List<string>> Permissions)`.

### Task 18: Create Employee DTO Records ✅ **[DONE - PR #32]**
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/Employees/DTOs/EmployeeDtos.cs`
- **Instructions**: Create record DTO `OnboardEmployeeDto(string FirstName, string LastName, string Email, string PhoneNumber, int JobRoleId, int RoleId, int SiteId)`.

### Task 19: Create Site DTO Records ✅ **[DONE - PR #34]**
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/Sites/DTOs/SiteDtos.cs`
- **Instructions**: Create record DTO `CreateSiteDto(string SiteName, double Latitude, double Longitude, List<string> MacWhitelist)`.

### Task 20: Create DraftShiftDto Record ✅ **[DONE - PR #35]**
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/Schedules/DTOs/ScheduleDtos.cs`
- **Instructions**: Create record DTO `DraftShiftDto(int EmployeeId, int JobRoleId, int SiteId, DateTimeOffset StartTime, DateTimeOffset EndTime)`.

### Task 21: Create PreFlightValidationResultDto Record ✅ **[DONE - PR #36]**
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/Schedules/DTOs/ValidationResultDto.cs`
- **Instructions**: Create record `PreFlightValidationResultDto(bool IsValid, List<string> Warnings, List<string> Errors)`.

### Task 22: Create ClaimShiftDto Record ✅ **[DONE - PR #37]**
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/ShiftMarket/DTOs/ShiftMarketDtos.cs`
- **Instructions**: Create record `ClaimShiftDto(int ShiftId, int EmployeeId, string OvertimeJustification)`.

### Task 23: Create CreatePointsRuleDto Record ✅ **[DONE - PR #38]**
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/Points/DTOs/PointsDtos.cs`
- **Instructions**: Create record `CreatePointsRuleDto(string RuleKey, string EventType, string ConditionExpression, string ActionType, int PointValue)`.

### Task 24: Create RewardItemDto Record ✅ **[DONE - PR #39]**
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/Rewards/DTOs/RewardDtos.cs`
- **Instructions**: Create record `RewardItemDto(int Id, string RewardName, int CostInPoints, int AvailableStock)`.

### Task 25: Create IScheduleValidationEngine Interface ✅ **[DONE - PR #40]**
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:application`, `interface`
- **Location**: `Buy2.Application/Schedules/Interfaces/IScheduleValidationEngine.cs`
- **Instructions**: Create interface `IScheduleValidationEngine` with method signature `Task<PreFlightValidationResultDto> ValidateDraftAsync(List<DraftShiftDto> shifts)`.

### Task 55: Create UploadEmployeeDocumentDto Record
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/Employees/DTOs/DocumentDtos.cs`
- **Instructions**: Create record `UploadEmployeeDocumentDto(int EmployeeId, string Category, string StorageUrl)`.

### Task 56: Create LogDisciplinaryViolationDto Record
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/Employees/DTOs/ViolationDtos.cs`
- **Instructions**: Create record `LogDisciplinaryViolationDto(int EmployeeId, string Severity, string Description)`.

### Task 57: Create PointsTransactionDto Record
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/Points/DTOs/PointsTransactionDtos.cs`
- **Instructions**: Create record `PointsTransactionDto(int EmployeeId, int? PointsRuleId, int Amount, string TransactionType)`.

### Task 58: Create RedeemRewardDto Record
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/Rewards/DTOs/RedemptionDtos.cs`
- **Instructions**: Create record `RedeemRewardDto(int RewardItemId, int EmployeeId)`.

---

## Phase 3: Infrastructure Layer (Tasks 26-35, 59-63, 77)

EF Core persistence, DbContext configurations, Fluent API rules, and external services.

### Task 26: Create Buy2DbContext Class
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Buy2DbContext.cs`
- **Instructions**: Create class `Buy2DbContext` inheriting from `DbContext`. Configure `DbSet<T>` for all domain entities (`Employee`, `Role`, `JobRole`, `Site`, `AttendanceProfile`, `Shift`, `ShiftClaim`, `PointsRule`, `RewardItem`, `EmployeeDocument`, `DisciplinaryViolation`, `PointsTransaction`, `RewardRedemption`). Apply entity configurations from assembly in `OnModelCreating`.

### Task 27: Create EmployeeConfiguration Class
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/EmployeeConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<Employee>`. Configure column specs: `FirstName` (required `nvarchar(50)`), `LastName` (required `nvarchar(50)`), `Email` (required `varchar(150)`, unique index), `PhoneNumber` (optional `varchar(20)`). Configure one-to-many relationships for `JobRole`, `Role`, `Site`, and `AttendanceProfile` with `DeleteBehavior.Restrict`.

### Task 28: Create RoleConfiguration Class
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/RoleConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<Role>`. Configure column specs: `RoleName` (required `nvarchar(50)`, unique index), `PermissionsJson` (required `nvarchar(max)`).

### Task 29: Create SiteConfiguration Class
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/SiteConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<Site>`. Configure column specs: `SiteName` (required `nvarchar(100)`), `Latitude` & `Longitude` (required decimal with precision 9,6), `MacAddressWhitelistJson` (optional `nvarchar(max)`).

### Task 30: Create ShiftConfiguration Class
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/ShiftConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<Shift>`. Configure column specs: `StartTime` & `EndTime` (required `datetimeoffset`), `IsPublished` (required boolean with default value `false`). Configure relationships for `Employee`, `Site`, and `JobRole` with `DeleteBehavior.Restrict`.

### Task 31: Create GenericRepository Class Implementation
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `repository`
- **Location**: `Buy2.Infrastructure/Persistence/Repositories/GenericRepository.cs`
- **Instructions**: Implement `IRepository<T>` using `Buy2DbContext`. Provide basic EF Core CRUD calls.

### Task 32: Create UnitOfWork Class Implementation
- **Difficulty**: Easy
- **Labels**: `layer:infrastructure`, `repository`
- **Location**: `Buy2.Infrastructure/Persistence/Repositories/UnitOfWork.cs`
- **Instructions**: Implement `IUnitOfWork` wrapping `Buy2DbContext.SaveChangesAsync()`.

### Task 33: Create JwtTokenGenerator Class Implementation
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `authentication`
- **Location**: `Buy2.Infrastructure/Authentication/JwtTokenGenerator.cs`
- **Instructions**: Implement `IJwtTokenGenerator` using `System.IdentityModel.Tokens.Jwt`.

### Task 34: Create ScheduleValidationEngine Class Stub
- **Difficulty**: Easy
- **Labels**: `layer:infrastructure`, `service`
- **Location**: `Buy2.Infrastructure/Services/ScheduleValidationEngine.cs`
- **Instructions**: Implement `IScheduleValidationEngine`. Return mock `PreFlightValidationResultDto(true, new(), new())`.

### Task 35: Create ExcelVoucherParser Class Stub
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `service`
- **Location**: `Buy2.Infrastructure/Services/ExcelVoucherParser.cs`
- **Instructions**: Create class `ExcelVoucherParser` with method `List<string> ParseExcelCodes(Stream stream)` throwing `NotImplementedException`.

### Task 59: Create EmployeeDocumentConfiguration Class
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/EmployeeDocumentConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<EmployeeDocument>`. Configure column specs: `Category` (required `nvarchar(50)`), `StorageUrl` (required `varchar(500)`). Configure relationship to `Employee` with `DeleteBehavior.Cascade`.

### Task 60: Create DisciplinaryViolationConfiguration Class
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/DisciplinaryViolationConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<DisciplinaryViolation>`. Configure column specs: `Severity` (required `varchar(20)`), `Description` (required `nvarchar(1000)`). Configure relationship to `Employee` with `DeleteBehavior.Cascade`.

### Task 61: Create PointsTransactionConfiguration Class
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/PointsTransactionConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<PointsTransaction>`. Configure column specs: `Amount` (required int), `TransactionType` (required `varchar(30)`). Configure relationships: `Employee` with `DeleteBehavior.Restrict` and optional `PointsRule` with `DeleteBehavior.SetNull`.

### Task 62: Create RewardRedemptionConfiguration Class
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/RewardRedemptionConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<RewardRedemption>`. Configure column specs: `VoucherCode` (required `varchar(100)`, unique index), `RedeemedAt` (required `datetimeoffset`). Configure relationships for `Employee` and `RewardItem` with `DeleteBehavior.Restrict`.

### Task 63: Create Infrastructure DependencyInjection Setup
- **Difficulty**: Easy
- **Labels**: `layer:infrastructure`, `service`
- **Location**: `Buy2.Infrastructure/DependencyInjection.cs`
- **Instructions**: Create extension method `AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)` registering repositories and DbContext.

### Task 77: Create ShiftClaimConfiguration Class
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/ShiftClaimConfiguration.cs`
- **Instructions**: Implement `IEntityTypeConfiguration<ShiftClaim>`. Configure column specs: `Status` (required `varchar(20)`), `OvertimeJustification` (optional `nvarchar(500)`). Configure relationships: `Shift` with `DeleteBehavior.Cascade` and `Employee` with `DeleteBehavior.Restrict`.

---

## Phase 4: API Layer Split Controllers (Tasks 36-42, 64-74)

*Note: Controllers are split into single-endpoint/single-responsibility classes so multiple backend developers can work in parallel without merge conflicts.*

### Task 36: Create AuthLoginController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/AuthLoginController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/auth")]`. Add `POST login` endpoint stub accepting `LoginRequestDto`.

### Task 64: Create AuthPasswordResetController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/AuthPasswordResetController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/auth")]`. Add `POST password/reset` endpoint stub.

### Task 37: Create CreateRoleController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/CreateRoleController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/roles")]`. Add `POST` create role endpoint stub accepting `CreateRoleDto`.

### Task 65: Create DeleteRoleController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/DeleteRoleController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/roles")]`. Add `DELETE {id}` soft delete endpoint stub.

### Task 38: Create EmployeeOnboardingController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/EmployeeOnboardingController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/employees")]`. Add `POST onboard` endpoint stub accepting `OnboardEmployeeDto`.

### Task 66: Create EmployeeAttendanceConfigController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/EmployeeAttendanceConfigController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/employees")]`. Add `PUT {id}/attendance-config` endpoint stub.

### Task 67: Create EmployeeDocumentsController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/EmployeeDocumentsController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/employees")]`. Add `POST {id}/documents` endpoint stub accepting `UploadEmployeeDocumentDto`.

### Task 68: Create DisciplinaryViolationsController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/DisciplinaryViolationsController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/employees")]`. Add `POST {id}/violations` endpoint stub accepting `LogDisciplinaryViolationDto`.

### Task 39: Create CreateSiteController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/CreateSiteController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/sites")]`. Add `POST` create site endpoint stub accepting `CreateSiteDto`.

### Task 69: Create GetSitesController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/GetSitesController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/sites")]`. Add `GET` all sites endpoint stub.

### Task 40: Create ScheduleValidationController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/ScheduleValidationController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/schedules")]`. Add `POST validate-draft` endpoint stub.

### Task 70: Create SchedulePublishController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/SchedulePublishController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/schedules")]`. Add `POST publish` endpoint stub.

### Task 41: Create OpenShiftsController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/OpenShiftsController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/shift-market")]`. Add `GET open-shifts` endpoint stub.

### Task 71: Create ShiftClaimsController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/ShiftClaimsController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/shift-market")]`. Add `POST claims/{id}` endpoint stub accepting `ClaimShiftDto`.

### Task 42: Create CreateRewardController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/CreateRewardController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/rewards")]`. Add `POST` reward creation endpoint stub accepting `RewardItemDto`.

### Task 72: Create RewardInventoryController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/RewardInventoryController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/rewards")]`. Add `POST {id}/inventory/upload` endpoint stub for bulk voucher Excel file.

### Task 73: Create PointsRulesController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/PointsRulesController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/points")]`. Add `POST rules` endpoint stub accepting `CreatePointsRuleDto`.

### Task 74: Create RewardRedemptionController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/RewardRedemptionController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/rewards")]`. Add `POST {id}/redeem` endpoint stub accepting `RedeemRewardDto`.

---

## Phase 5: Angular Frontend Layer (Tasks 43-50, 75-76)

> **Figma Design Reference**: [https://www.figma.com/design/JQ67DCkObzVjER8Safb5sw/BUY2-Junk-File?node-id=882-3040&p=f&t=GxfjGebSZZA9X7B0-0](https://www.figma.com/design/JQ67DCkObzVjER8Safb5sw/BUY2-Junk-File?node-id=882-3040&p=f&t=GxfjGebSZZA9X7B0-0)
> All UI components must implement layouts, colors, and responsive specs from the official Figma design link above.

### Task 43: Create TypeScript Data Models ✅ **[DONE - PR #3]**
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:frontend`, `models`
- **Location**: `Buy2.Frontend/src/app/core/models/hrms.models.ts`
- **Instructions**: Define TypeScript interfaces for `Employee`, `Role`, `Site`, `Shift`, `ValidationResult`, and `RewardItem`.

### Task 44: Create AuthService Angular Service
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:frontend`, `service`
- **Location**: `Buy2.Frontend/src/app/core/services/auth.service.ts`
- **Instructions**: Create injectable Angular service `AuthService` with `login(req: LoginRequest)` method calling HttpClient.

### Task 45: Create ScheduleService Angular Service
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:frontend`, `service`
- **Location**: `Buy2.Frontend/src/app/core/services/schedule.service.ts`
- **Instructions**: Create injectable Angular service `ScheduleService` with `validateDraft(shifts: Shift[])` method.

### Task 46: Create LoginComponent Component
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:frontend`, `component`
- **Location**: `Buy2.Frontend/src/app/features/auth/login.component.ts`
- **Instructions**: Create standalone Angular component `LoginComponent` with email/password reactive form.

### Task 47: Create EmployeeDirectoryComponent
- **Difficulty**: Medium
- **Labels**: `good first issue`, `layer:frontend`, `component`
- **Location**: `Buy2.Frontend/src/app/features/employees/employee-directory.component.ts`
- **Instructions**: Create component `EmployeeDirectoryComponent` with table listing employees and search filter input.

### Task 48: Create ScheduleBoardComponent
- **Difficulty**: Medium
- **Labels**: `layer:frontend`, `component`
- **Location**: `Buy2.Frontend/src/app/features/schedules/schedule-board.component.ts`
- **Instructions**: Create component `ScheduleBoardComponent` displaying weekly shift grid and validation warning badges.

### Task 49: Create ShiftMarketComponent
- **Difficulty**: Medium
- **Labels**: `good first issue`, `layer:frontend`, `component`
- **Location**: `Buy2.Frontend/src/app/features/shift-market/shift-market.component.ts`
- **Instructions**: Create component `ShiftMarketComponent` listing available open shifts with 'Claim Shift' button.

### Task 50: Create RewardsStoreComponent
- **Difficulty**: Medium
- **Labels**: `good first issue`, `layer:frontend`, `component`
- **Location**: `Buy2.Frontend/src/app/features/rewards/rewards-store.component.ts`
- **Instructions**: Create component `RewardsStoreComponent` with points balance display, rewards card grid, and Excel upload button.

### Task 75: Create EmployeeDocumentsComponent
- **Difficulty**: Medium
- **Labels**: `good first issue`, `layer:frontend`, `component`
- **Location**: `Buy2.Frontend/src/app/features/employees/employee-documents.component.ts`
- **Instructions**: Create component `EmployeeDocumentsComponent` displaying document uploads and list of employee compliance files.

### Task 76: Create PointsRulesAdminComponent
- **Difficulty**: Medium
- **Labels**: `layer:frontend`, `component`
- **Location**: `Buy2.Frontend/src/app/features/points/points-rules-admin.component.ts`
- **Instructions**: Create component `PointsRulesAdminComponent` for managing point automation triggers and penalty/reward values.
