# Buy2 HRMS - New Jira Task Cards (SCRUM-56 to SCRUM-81)

This file contains the detailed specifications for adding new Jira Task Cards (`SCRUM-56` through `SCRUM-81`) to the Jira Sprint Board (**Space**: `Buy2 HRMS`, **Key**: `SCRUM`).

> **Import Option**: Use the generated CSV file [`docs/jira/JIRA_NEW_TASKS_IMPORT.csv`](file:///F:/C%23%20projects/HR_system/docs/jira/JIRA_NEW_TASKS_IMPORT.csv) in Jira -> Settings -> System -> External System Import -> CSV.

---

## Task Cards Reference List

### 🔹 Domain Layer (Entities)

#### 💳 Ticket: SCRUM-56 (Task 51)
- **Summary**: Create EmployeeDocument Entity
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `src/Buy2.Domain/Entities/EmployeeDocument.cs`
- **Branch**: `SCRUM-56-create-employee-document-entity`
- **PR Title**: `SCRUM-56: Create EmployeeDocument Entity`
- **Description**:
  Create class `EmployeeDocument` inheriting from `BaseEntity`. Add integer `EmployeeId`, string `Category`, and string `StorageUrl`.

#### 💳 Ticket: SCRUM-57 (Task 52)
- **Summary**: Create DisciplinaryViolation Entity
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `src/Buy2.Domain/Entities/DisciplinaryViolation.cs`
- **Branch**: `SCRUM-57-create-disciplinary-violation-entity`
- **PR Title**: `SCRUM-57: Create DisciplinaryViolation Entity`
- **Description**:
  Create class `DisciplinaryViolation` inheriting from `BaseEntity`. Add integer `EmployeeId`, string `Severity`, and string `Description`.

#### 💳 Ticket: SCRUM-58 (Task 53)
- **Summary**: Create PointsTransaction Entity
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `src/Buy2.Domain/Entities/PointsTransaction.cs`
- **Branch**: `SCRUM-58-create-points-transaction-entity`
- **PR Title**: `SCRUM-58: Create PointsTransaction Entity`
- **Description**:
  Create class `PointsTransaction` inheriting from `BaseEntity`. Add `EmployeeId` (`int`), `PointsRuleId` (`int?`), `Amount` (`int`), and `TransactionType` (`string`).

#### 💳 Ticket: SCRUM-59 (Task 54)
- **Summary**: Create RewardRedemption Entity
- **Labels**: `good first issue`, `layer:domain`, `entity`
- **Location**: `src/Buy2.Domain/Entities/RewardRedemption.cs`
- **Branch**: `SCRUM-59-create-reward-redemption-entity`
- **PR Title**: `SCRUM-59: Create RewardRedemption Entity`
- **Description**:
  Create class `RewardRedemption` inheriting from `BaseEntity`. Add `RewardItemId` (`int`), `EmployeeId` (`int`), `VoucherCode` (`string`), and `RedeemedAt` (`DateTimeOffset`).

---

### 🔹 Application Layer (DTOs)

#### 💳 Ticket: SCRUM-60 (Task 55)
- **Summary**: Create UploadEmployeeDocumentDto Record
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `src/Buy2.Application/Employees/DTOs/DocumentDtos.cs`
- **Branch**: `SCRUM-60-create-upload-employee-document-dto`
- **PR Title**: `SCRUM-60: Create UploadEmployeeDocumentDto Record`
- **Description**:
  Create record DTO `UploadEmployeeDocumentDto(int EmployeeId, string Category, string StorageUrl)`.

#### 💳 Ticket: SCRUM-61 (Task 56)
- **Summary**: Create LogDisciplinaryViolationDto Record
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `src/Buy2.Application/Employees/DTOs/ViolationDtos.cs`
- **Branch**: `SCRUM-61-create-log-disciplinary-violation-dto`
- **PR Title**: `SCRUM-61: Create LogDisciplinaryViolationDto Record`
- **Description**:
  Create record DTO `LogDisciplinaryViolationDto(int EmployeeId, string Severity, string Description)`.

#### 💳 Ticket: SCRUM-62 (Task 57)
- **Summary**: Create PointsTransactionDto Record
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `src/Buy2.Application/Points/DTOs/PointsTransactionDtos.cs`
- **Branch**: `SCRUM-62-create-points-transaction-dto`
- **PR Title**: `SCRUM-62: Create PointsTransactionDto Record`
- **Description**:
  Create record DTO `PointsTransactionDto(int EmployeeId, int? PointsRuleId, int Amount, string TransactionType)`.

#### 💳 Ticket: SCRUM-63 (Task 58)
- **Summary**: Create RedeemRewardDto Record
- **Labels**: `good first issue`, `layer:application`, `dto`
- **Location**: `src/Buy2.Application/Rewards/DTOs/RedemptionDtos.cs`
- **Branch**: `SCRUM-63-create-redeem-reward-dto`
- **PR Title**: `SCRUM-63: Create RedeemRewardDto Record`
- **Description**:
  Create record DTO `RedeemRewardDto(int RewardItemId, int EmployeeId)`.

---

### 🔹 Infrastructure Layer (Persistence & DI)

#### 💳 Ticket: SCRUM-64 (Task 59)
- **Summary**: Create EmployeeDocumentConfiguration Class
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `src/Buy2.Infrastructure/Persistence/Configurations/EmployeeDocumentConfiguration.cs`
- **Branch**: `SCRUM-64-create-employee-document-configuration`
- **PR Title**: `SCRUM-64: Create EmployeeDocumentConfiguration Class`
- **Description**:
  Create class `EmployeeDocumentConfiguration` implementing `IEntityTypeConfiguration<EmployeeDocument>`.

#### 💳 Ticket: SCRUM-65 (Task 60)
- **Summary**: Create DisciplinaryViolationConfiguration Class
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `src/Buy2.Infrastructure/Persistence/Configurations/DisciplinaryViolationConfiguration.cs`
- **Branch**: `SCRUM-65-create-disciplinary-violation-configuration`
- **PR Title**: `SCRUM-65: Create DisciplinaryViolationConfiguration Class`
- **Description**:
  Create class `DisciplinaryViolationConfiguration` implementing `IEntityTypeConfiguration<DisciplinaryViolation>`.

#### 💳 Ticket: SCRUM-66 (Task 61)
- **Summary**: Create PointsTransactionConfiguration Class
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `src/Buy2.Infrastructure/Persistence/Configurations/PointsTransactionConfiguration.cs`
- **Branch**: `SCRUM-66-create-points-transaction-configuration`
- **PR Title**: `SCRUM-66: Create PointsTransactionConfiguration Class`
- **Description**:
  Create class `PointsTransactionConfiguration` implementing `IEntityTypeConfiguration<PointsTransaction>`.

#### 💳 Ticket: SCRUM-67 (Task 62)
- **Summary**: Create RewardRedemptionConfiguration Class
- **Labels**: `layer:infrastructure`, `database`
- **Location**: `src/Buy2.Infrastructure/Persistence/Configurations/RewardRedemptionConfiguration.cs`
- **Branch**: `SCRUM-67-create-reward-redemption-configuration`
- **PR Title**: `SCRUM-67: Create RewardRedemptionConfiguration Class`
- **Description**:
  Create class `RewardRedemptionConfiguration` implementing `IEntityTypeConfiguration<RewardRedemption>`.

#### 💳 Ticket: SCRUM-68 (Task 63)
- **Summary**: Create Infrastructure DependencyInjection Setup
- **Labels**: `layer:infrastructure`, `service`
- **Location**: `src/Buy2.Infrastructure/DependencyInjection.cs`
- **Branch**: `SCRUM-68-create-infrastructure-dependency-injection`
- **PR Title**: `SCRUM-68: Create Infrastructure DependencyInjection Setup`
- **Description**:
  Create static class `DependencyInjection` with extension method `AddInfrastructureServices`.

---

### 🔹 API Layer Split Controllers

#### 💳 Ticket: SCRUM-69 (Task 64)
- **Summary**: Create AuthPasswordResetController
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `src/Buy2.Api/Controllers/AuthPasswordResetController.cs`
- **Branch**: `SCRUM-69-create-auth-password-reset-controller`
- **PR Title**: `SCRUM-69: Create AuthPasswordResetController`
- **Description**:
  Create `[ApiController]` at `[Route("api/v1/auth")]`. Add `POST password/reset` endpoint stub.

#### 💳 Ticket: SCRUM-70 (Task 65)
- **Summary**: Create DeleteRoleController
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `src/Buy2.Api/Controllers/DeleteRoleController.cs`
- **Branch**: `SCRUM-70-create-delete-role-controller`
- **PR Title**: `SCRUM-70: Create DeleteRoleController`
- **Description**:
  Create `[ApiController]` at `[Route("api/v1/roles")]`. Add `DELETE {id}` soft delete endpoint stub.

#### 💳 Ticket: SCRUM-71 (Task 66)
- **Summary**: Create EmployeeAttendanceConfigController
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `src/Buy2.Api/Controllers/EmployeeAttendanceConfigController.cs`
- **Branch**: `SCRUM-71-create-employee-attendance-config-controller`
- **PR Title**: `SCRUM-71: Create EmployeeAttendanceConfigController`
- **Description**:
  Create `[ApiController]` at `[Route("api/v1/employees")]`. Add `PUT {id}/attendance-config` endpoint stub.

#### 💳 Ticket: SCRUM-72 (Task 67)
- **Summary**: Create EmployeeDocumentsController
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `src/Buy2.Api/Controllers/EmployeeDocumentsController.cs`
- **Branch**: `SCRUM-72-create-employee-documents-controller`
- **PR Title**: `SCRUM-72: Create EmployeeDocumentsController`
- **Description**:
  Create `[ApiController]` at `[Route("api/v1/employees")]`. Add `POST {id}/documents` endpoint stub accepting `UploadEmployeeDocumentDto`.

#### 💳 Ticket: SCRUM-73 (Task 68)
- **Summary**: Create DisciplinaryViolationsController
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `src/Buy2.Api/Controllers/DisciplinaryViolationsController.cs`
- **Branch**: `SCRUM-73-create-disciplinary-violations-controller`
- **PR Title**: `SCRUM-73: Create DisciplinaryViolationsController`
- **Description**:
  Create `[ApiController]` at `[Route("api/v1/employees")]`. Add `POST {id}/violations` endpoint stub accepting `LogDisciplinaryViolationDto`.

#### 💳 Ticket: SCRUM-74 (Task 69)
- **Summary**: Create GetSitesController
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `src/Buy2.Api/Controllers/GetSitesController.cs`
- **Branch**: `SCRUM-74-create-get-sites-controller`
- **PR Title**: `SCRUM-74: Create GetSitesController`
- **Description**:
  Create `[ApiController]` at `[Route("api/v1/sites")]`. Add `GET` endpoint stub returning site list.

#### 💳 Ticket: SCRUM-75 (Task 70)
- **Summary**: Create SchedulePublishController
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `src/Buy2.Api/Controllers/SchedulePublishController.cs`
- **Branch**: `SCRUM-75-create-schedule-publish-controller`
- **PR Title**: `SCRUM-75: Create SchedulePublishController`
- **Description**:
  Create `[ApiController]` at `[Route("api/v1/schedules")]`. Add `POST publish` endpoint stub.

#### 💳 Ticket: SCRUM-76 (Task 71)
- **Summary**: Create ShiftClaimsController
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `src/Buy2.Api/Controllers/ShiftClaimsController.cs`
- **Branch**: `SCRUM-76-create-shift-claims-controller`
- **PR Title**: `SCRUM-76: Create ShiftClaimsController`
- **Description**:
  Create `[ApiController]` at `[Route("api/v1/shift-market")]`. Add `POST claims/{id}` endpoint stub accepting `ClaimShiftDto`.

#### 💳 Ticket: SCRUM-77 (Task 72)
- **Summary**: Create RewardInventoryController
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `src/Buy2.Api/Controllers/RewardInventoryController.cs`
- **Branch**: `SCRUM-77-create-reward-inventory-controller`
- **PR Title**: `SCRUM-77: Create RewardInventoryController`
- **Description**:
  Create `[ApiController]` at `[Route("api/v1/rewards")]`. Add `POST {id}/inventory/upload` endpoint stub.

#### 💳 Ticket: SCRUM-78 (Task 73)
- **Summary**: Create PointsRulesController
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `src/Buy2.Api/Controllers/PointsRulesController.cs`
- **Branch**: `SCRUM-78-create-points-rules-controller`
- **PR Title**: `SCRUM-78: Create PointsRulesController`
- **Description**:
  Create `[ApiController]` at `[Route("api/v1/points")]`. Add `POST rules` endpoint stub accepting `CreatePointsRuleDto`.

#### 💳 Ticket: SCRUM-79 (Task 74)
- **Summary**: Create RewardRedemptionController
- **Labels**: `good first issue`, `layer:api`, `controller`
- **Location**: `src/Buy2.Api/Controllers/RewardRedemptionController.cs`
- **Branch**: `SCRUM-79-create-reward-redemption-controller`
- **PR Title**: `SCRUM-79: Create RewardRedemptionController`
- **Description**:
  Create `[ApiController]` at `[Route("api/v1/rewards")]`. Add `POST {id}/redeem` endpoint stub accepting `RedeemRewardDto`.

---

### 🔹 Angular Frontend Layer

#### 💳 Ticket: SCRUM-80 (Task 75)
- **Summary**: Create EmployeeDocumentsComponent
- **Labels**: `good first issue`, `layer:frontend`, `component`
- **Location**: `src/Buy2.Frontend/src/app/features/employees/employee-documents.component.ts`
- **Branch**: `SCRUM-80-create-employee-documents-component`
- **PR Title**: `SCRUM-80: Create EmployeeDocumentsComponent`
- **Description**:
  Create standalone Angular component `EmployeeDocumentsComponent` for uploading and managing employee compliance files.

#### 💳 Ticket: SCRUM-81 (Task 76)
- **Summary**: Create PointsRulesAdminComponent
- **Labels**: `layer:frontend`, `component`
- **Location**: `src/Buy2.Frontend/src/app/features/points/points-rules-admin.component.ts`
- **Branch**: `SCRUM-81-create-points-rules-admin-component`
- **PR Title**: `SCRUM-81: Create PointsRulesAdminComponent`
- **Description**:
  Create standalone Angular component `PointsRulesAdminComponent` for managing points rules and penalties.
