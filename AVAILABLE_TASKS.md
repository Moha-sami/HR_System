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

## Phase 1: Domain Layer (Tasks 1-12)

Focus on core domain models, enums, and base entities. No database dependencies or framework logic.

### Task 1: Create BaseEntity Class ✅ **[DONE]**
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `Buy2.Domain/Common/BaseEntity.cs`
- **Instructions**: Create an abstract class named `BaseEntity`. Add an integer `Id` property and a `CreatedAt` (`DateTimeOffset`) property.

### Task 2: Create Role Entity ✅ **[DONE]**
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `Buy2.Domain/Entities/Role.cs`
- **Instructions**: Create a public class `Role` inheriting from `BaseEntity`. Add `RoleName` (`string`) and `PermissionsJson` (`string`).

### Task 3: Create JobRole Entity ✅ **[DONE]**
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `Buy2.Domain/Entities/JobRole.cs`
- **Instructions**: Create class `JobRole` inheriting from `BaseEntity`. Add `Title` (`string`), `DepartmentId` (`int`), and `RequiredQualificationsJson` (`string`).

### Task 4: Create Employee Entity ✅ **[DONE]**
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `Buy2.Domain/Entities/Employee.cs`
- **Instructions**: Create class `Employee` inheriting from `BaseEntity`. Add `FirstName`, `LastName`, `Email`, `PhoneNumber`, `JobRoleId` (`int`), `RoleId` (`int`), and `SiteId` (`int`).

### Task 5: Create Site Entity ✅ **[DONE]**
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `Buy2.Domain/Entities/Site.cs`
- **Instructions**: Create class `Site` inheriting from `BaseEntity`. Add `SiteName`, `Latitude` (`double`), `Longitude` (`double`), and `MacAddressWhitelistJson` (`string`).

### Task 6: Create AttendanceProfile Entity ✅ **[DONE]**
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `Buy2.Domain/Entities/AttendanceProfile.cs`
- **Instructions**: Create class `AttendanceProfile` inheriting from `BaseEntity`. Add `ProfileName`, `ExpectedClockIn` (`TimeSpan`), `ExpectedClockOut` (`TimeSpan`), and `RequiredWorkHours` (`double`).

### Task 7: Create Shift Entity ✅ **[DONE]**
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `Buy2.Domain/Entities/Shift.cs`
- **Instructions**: Create class `Shift` inheriting from `BaseEntity`. Add `EmployeeId` (`int`), `SiteId` (`int`), `JobRoleId` (`int`), `StartTime` (`DateTimeOffset`), `EndTime` (`DateTimeOffset`), and `IsPublished` (`bool`).

### Task 8: Create ShiftClaim Entity
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `Buy2.Domain/Entities/ShiftClaim.cs`
- **Instructions**: Create class `ShiftClaim` inheriting from `BaseEntity`. Add `ShiftId` (`int`), `EmployeeId` (`int`), `Status` (`string`), and `OvertimeJustification` (`string`).

### Task 9: Create PointsRule Entity
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `Buy2.Domain/Entities/PointsRule.cs`
- **Instructions**: Create class `PointsRule` inheriting from `BaseEntity`. Add `RuleKey`, `EventType`, `ConditionExpression`, `ActionType`, and `PointValue` (`int`).

### Task 10: Create RewardItem Entity
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `Buy2.Domain/Entities/RewardItem.cs`
- **Instructions**: Create class `RewardItem` inheriting from `BaseEntity`. Add `RewardName`, `CostInPoints` (`int`), and `AvailableStock` (`int`).

### Task 11: Create Gender and SalaryType Enums ✅ **[DONE]**
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:domain`, `enum`
- **Location**: `Buy2.Domain/Enums/DomainEnums.cs`
- **Instructions**: Create public enums `Gender` (`Male = 1, Female = 2`) and `SalaryType` (`Fixed = 1, Hourly = 2`).

### Task 12: Create ShiftStatus and ClaimStatus Enums ✅ **[DONE]**
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:domain`, `enum`
- **Location**: `Buy2.Domain/Enums/ShiftEnums.cs`
- **Instructions**: Create public enums `ShiftStatus` (`Draft = 0, Published = 1, Cancelled = 2`) and `ClaimStatus` (`Pending = 0, Approved = 1, Rejected = 2`).

---

## Phase 2: Application Layer (Tasks 13-25)

Application interfaces, DTO records, and CQRS contracts.

### Task 13: Create IRepository Generic Interface
- **Difficulty**: Medium
- **Labels**: `good first issue`, `layer:application`, `interface`
- **Location**: `Buy2.Application/Common/Interfaces/IRepository.cs`
- **Instructions**: Create generic interface `IRepository<T>` where `T : BaseEntity`. Add method signatures: `GetByIdAsync`, `GetAllAsync`, `AddAsync`, `Update`, `Delete`.

### Task 14: Create IUnitOfWork Interface
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:application`, `interface`
- **Location**: `Buy2.Application/Common/Interfaces/IUnitOfWork.cs`
- **Instructions**: Create interface `IUnitOfWork` with `SaveChangesAsync(CancellationToken cancellationToken = default)` signature.

### Task 15: Create IJwtTokenGenerator Interface
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:application`, `interface`
- **Location**: `Buy2.Application/Common/Interfaces/IJwtTokenGenerator.cs`
- **Instructions**: Create interface `IJwtTokenGenerator` with method signature `string GenerateToken(int userId, string email, string role)`.

### Task 16: Create Login DTO Records
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/Auth/DTOs/AuthDtos.cs`
- **Instructions**: Create record DTOs `LoginRequestDto(string Email, string Password)` and `LoginResponseDto(string Token, int ExpiresIn, string Role)`.

### Task 17: Create Role DTO Records
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/Roles/DTOs/RoleDtos.cs`
- **Instructions**: Create record DTO `CreateRoleDto(string RoleName, Dictionary<string, List<string>> Permissions)`.

### Task 18: Create Employee DTO Records
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/Employees/DTOs/EmployeeDtos.cs`
- **Instructions**: Create record DTO `OnboardEmployeeDto(string FirstName, string LastName, string Email, string PhoneNumber, int JobRoleId, int RoleId, int SiteId)`.

### Task 19: Create Site DTO Records
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/Sites/DTOs/SiteDtos.cs`
- **Instructions**: Create record DTO `CreateSiteDto(string SiteName, double Latitude, double Longitude, List<string> MacWhitelist)`.

### Task 20: Create DraftShiftDto Record
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/Schedules/DTOs/ScheduleDtos.cs`
- **Instructions**: Create record DTO `DraftShiftDto(int EmployeeId, int JobRoleId, int SiteId, DateTimeOffset StartTime, DateTimeOffset EndTime)`.

### Task 21: Create PreFlightValidationResultDto Record
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/Schedules/DTOs/ValidationResultDto.cs`
- **Instructions**: Create record `PreFlightValidationResultDto(bool IsValid, List<string> Warnings, List<string> Errors)`.

### Task 22: Create ClaimShiftDto Record
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/ShiftMarket/DTOs/ShiftMarketDtos.cs`
- **Instructions**: Create record `ClaimShiftDto(int ShiftId, int EmployeeId, string OvertimeJustification)`.

### Task 23: Create CreatePointsRuleDto Record
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/Points/DTOs/PointsDtos.cs`
- **Instructions**: Create record `CreatePointsRuleDto(string RuleKey, string EventType, string ConditionExpression, string ActionType, int PointValue)`.

### Task 24: Create RewardItemDto Record
- **Difficulty**: Very Easy
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `Buy2.Application/Rewards/DTOs/RewardDtos.cs`
- **Instructions**: Create record `RewardItemDto(int Id, string RewardName, int CostInPoints, int AvailableStock)`.

### Task 25: Create IScheduleValidationEngine Interface
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:application`, `interface`
- **Location**: `Buy2.Application/Schedules/Interfaces/IScheduleValidationEngine.cs`
- **Instructions**: Create interface `IScheduleValidationEngine` with method signature `Task<PreFlightValidationResultDto> ValidateDraftAsync(List<DraftShiftDto> shifts)`.

---

## Phase 3: Infrastructure Layer (Tasks 26-35)

EF Core persistence, DbContext configurations, and external services.

### Task 26: Create Buy2DbContext Class
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Buy2DbContext.cs`
- **Instructions**: Create class `Buy2DbContext` inheriting from `DbContext`. Add `DbSet<Employee>`, `DbSet<Role>`, `DbSet<Site>`, `DbSet<Shift>`, `DbSet<RewardItem>`.

### Task 27: Create EmployeeConfiguration Class
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/EmployeeConfiguration.cs`
- **Instructions**: Create class `EmployeeConfiguration` implementing `IEntityTypeConfiguration<Employee>`. Configure required fields for Email and Names.

### Task 28: Create RoleConfiguration Class
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/RoleConfiguration.cs`
- **Instructions**: Create class `RoleConfiguration` implementing `IEntityTypeConfiguration<Role>`.

### Task 29: Create SiteConfiguration Class
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/SiteConfiguration.cs`
- **Instructions**: Create class `SiteConfiguration` implementing `IEntityTypeConfiguration<Site>`.

### Task 30: Create ShiftConfiguration Class
- **Difficulty**: Medium
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `Buy2.Infrastructure/Persistence/Configurations/ShiftConfiguration.cs`
- **Instructions**: Create class `ShiftConfiguration` implementing `IEntityTypeConfiguration<Shift>`.

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

---

## Phase 4: API Layer Controllers (Tasks 36-42)

RESTful API Controller stubs returning ActionResult responses.

### Task 36: Create AuthController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/AuthController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/auth")]`. Add `POST login` and `POST password/reset` endpoint stubs.

### Task 37: Create RolesController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/RolesController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/roles")]`. Add `POST` create role and `DELETE {id}` soft delete endpoints.

### Task 38: Create EmployeesController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/EmployeesController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/employees")]`. Add `POST` onboard and `PUT {id}/attendance-config` endpoints.

### Task 39: Create SitesController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/SitesController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/sites")]`. Add `POST` create site and `GET` all sites endpoints.

### Task 40: Create SchedulesController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/SchedulesController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/schedules")]`. Add `POST validate-draft` and `POST publish` endpoints.

### Task 41: Create ShiftMarketController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/ShiftMarketController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/shift-market")]`. Add `GET open-shifts` and `POST claims/{id}` endpoints.

### Task 42: Create RewardsController
- **Difficulty**: Easy
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `Buy2.Api/Controllers/RewardsController.cs`
- **Instructions**: Create `[ApiController]` at `[route("api/v1/rewards")]`. Add `POST` reward creation and `POST {id}/inventory/upload` endpoints.

---

## Phase 5: Angular Frontend Layer (Tasks 43-50)

> **Figma Design Reference**: [https://www.figma.com/design/JQ67DCkObzVjER8Safb5sw/BUY2-Junk-File?node-id=882-3040&p=f&t=GxfjGebSZZA9X7B0-0](https://www.figma.com/design/JQ67DCkObzVjER8Safb5sw/BUY2-Junk-File?node-id=882-3040&p=f&t=GxfjGebSZZA9X7B0-0)
> All UI components must implement layouts, colors, and responsive specs from the official Figma design link above.

### Task 43: Create TypeScript Data Models ✅ **[DONE]**
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
