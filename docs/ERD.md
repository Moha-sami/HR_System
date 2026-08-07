# Buy2 HRMS - Database ERD Architecture Specification

Complete database entity relationship documentation containing multi-view visual diagrams ordered from high-level clean overview to detailed architectural schematics.

---

## 1. High-Level Clean ERD Overview (Light Theme)

*Minimalist, non-intersecting relationship layout for fast high-level understanding.*

![1. High-Level Clean White ERD Overview](./clean_white_erd_diagram.jpg)

---

## 2. Explicitly Labeled Relationship Diagram (Dark Theme)

*Features explicit action labels on connecting arrows detailing entity interactions.*

![2. Explicitly Labeled Relationship ERD Diagram](./labeled_erd_diagram.jpg)

---

## 3. Ultra-Detailed Schema Architecture Chart

*Full database schema layout with field data types, primary keys, and foreign keys.*

![3. Detailed Schema Architecture Chart](./database_erd_diagram.jpg)

---

## Detailed Relationship Explanations

| Source Entity | Relationship Action (Verb) | Target Entity | Business Logic & Rules |
| :--- | :--- | :--- | :--- |
| **`Role`** | `assigned to (1:N)` | **`Employee`** | Role defines modular permission toggles assigned to multiple employees. |
| **`JobRole`** | `defines position for (1:N)` | **`Employee`** | Job position specifying required qualifications (POS, Management). |
| **`Site`** | `maps primary branch for (1:N)` | **`Employee`** | Mappings for geofence coordinates and MAC whitelists. |
| **`AttendanceProfile`** | `dictates clock-in rules for (1:N)` | **`Employee`** | Configures daily expected start/end times and break durations. |
| **`Site`** | `hosts (1:N)` | **`Shift`** | Physical store branch location where work shifts take place. |
| **`JobRole`** | `demands qualifications for (1:N)` | **`Shift`** | Shift requires employee to hold matching qualifications. |
| **`Employee`** | `assigned to (1:N)` | **`Shift`** | Work shift block assigned to an employee on the schedule board. |
| **`Shift`** | `listed in market as (1:N)` | **`ShiftClaim`** | Shift posted to the internal marketplace for claiming/swapping. |
| **`Employee`** | `claims (1:N)` | **`ShiftClaim`** | Teammate submitting a claim to cover a marketplace shift. |
| **`Employee`** | `owns compliance files (1:N)` | **`EmployeeDocument`** | Uploaded ID copies, medical leaves, and certificates. |
| **`Employee`** | `receives infraction (1:N)` | **`DisciplinaryViolation`** | Disciplinary violations logged by managers with attached evidence. |
| **`PointsRule`** | `triggers automation for (1:N)` | **`PointsTransaction`** | Rule logic evaluating clock-in delay to credit or debit wallet points. |
| **`Employee`** | `accumulates/spends (1:N)` | **`PointsTransaction`** | Ledger transactions modifying employee points balance. |
| **`RewardItem`** | `depletes stock for (1:N)` | **`RewardRedemption`** | Digital store voucher (Talabat, Noon) redeemed by employee points. |

---

## Interactive Mermaid Diagram

```mermaid
erDiagram

    Role ||--o{ Employee : "assigned to (1:N)"
    JobRole ||--o{ Employee : "defines position for (1:N)"
    Site ||--o{ Employee : "maps primary branch for (1:N)"
    AttendanceProfile ||--o{ Employee : "dictates clock-in rules for (1:N)"
    
    Site ||--o{ Shift : "hosts (1:N)"
    JobRole ||--o{ Shift : "demands qualifications for (1:N)"
    Employee ||--o{ Shift : "assigned to (1:N)"
    
    Shift ||--o{ ShiftClaim : "listed in market as (1:N)"
    Employee ||--o{ ShiftClaim : "claims (1:N)"
    
    Employee ||--o{ EmployeeDocument : "owns compliance files (1:N)"
    Employee ||--o{ DisciplinaryViolation : "receives infraction (1:N)"
    
    Employee ||--o{ PointsTransaction : "accumulates/spends (1:N)"
    PointsRule ||--o{ PointsTransaction : "triggers automation for (1:N)"
    
    Employee ||--o{ RewardRedemption : "purchases (1:N)"
    RewardItem ||--o{ RewardRedemption : "depletes stock for (1:N)"

    Role {
        int Id PK
        string RoleName
        string PermissionsJson
        DateTimeOffset CreatedAt
    }

    JobRole {
        int Id PK
        string Title
        int DepartmentId
        string RequiredQualificationsJson
        DateTimeOffset CreatedAt
    }

    Site {
        int Id PK
        string SiteName
        double Latitude
        double Longitude
        string MacAddressWhitelistJson
        DateTimeOffset CreatedAt
    }

    AttendanceProfile {
        int Id PK
        string ProfileName
        TimeSpan ExpectedClockIn
        TimeSpan ExpectedClockOut
        double RequiredWorkHours
        DateTimeOffset CreatedAt
    }

    Employee {
        int Id PK
        string FirstName
        string LastName
        string Email
        string PhoneNumber
        int JobRoleId FK
        int RoleId FK
        int SiteId FK
        int AttendanceProfileId FK
        DateTimeOffset CreatedAt
    }

    Shift {
        int Id PK
        int EmployeeId FK
        int SiteId FK
        int JobRoleId FK
        DateTimeOffset StartTime
        DateTimeOffset EndTime
        bool IsPublished
        DateTimeOffset CreatedAt
    }

    ShiftClaim {
        int Id PK
        int ShiftId FK
        int EmployeeId FK
        string Status
        string OvertimeJustification
        DateTimeOffset CreatedAt
    }

    EmployeeDocument {
        int Id PK
        int EmployeeId FK
        string Category
        string StorageUrl
        DateTimeOffset CreatedAt
    }

    DisciplinaryViolation {
        int Id PK
        int EmployeeId FK
        string Severity
        string Description
        DateTimeOffset CreatedAt
    }

    PointsRule {
        int Id PK
        string RuleKey
        string EventType
        string ConditionExpression
        string ActionType
        int PointValue
        DateTimeOffset CreatedAt
    }

    PointsTransaction {
        int Id PK
        int EmployeeId FK
        int PointsRuleId FK
        int Amount
        string TransactionType
        DateTimeOffset CreatedAt
    }

    RewardItem {
        int Id PK
        string RewardName
        int CostInPoints
        int AvailableStock
        DateTimeOffset CreatedAt
    }

    RewardRedemption {
        int Id PK
        int RewardItemId FK
        int EmployeeId FK
        string VoucherCode
        DateTimeOffset RedeemedAt
    }
```
