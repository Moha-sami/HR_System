# Buy2 HRMS - Database ERD Architecture Specification

Complete database entity relationship documentation containing multi-view visual diagrams ordered from high-level clean overview to detailed relationship diagrams.

---

## 1. High-Level Clean ERD Overview (Light Theme)

*Minimalist, non-intersecting relationship layout for fast high-level understanding.*

![1. High-Level Clean White ERD Overview](./clean_white_erd_diagram.jpg)

---

## 2. Explicitly Labeled Relationship Diagram (Dark Theme)

*Features explicit action labels on connecting arrows detailing entity interactions.*

![2. Explicitly Labeled Relationship ERD Diagram](./labeled_erd_diagram.jpg)

---

## Detailed Relationship Explanations

| Source Entity | Relationship Action (Verb) | Target Entity | Cardinality | Business Logic & Rules |
| :--- | :--- | :--- | :--- | :--- |
| **`Role`** | `assigned to` | **`Employee`** | **One to Many** | Role defines modular permission toggles assigned to multiple employees. |
| **`JobRole`** | `defines position for` | **`Employee`** | **One to Many** | Job position specifying required qualifications (POS, Management). |
| **`Site`** | `maps primary branch for` | **`Employee`** | **One to Many** | Mappings for geofence coordinates and MAC whitelists. |
| **`AttendanceProfile`** | `dictates clock-in rules for` | **`Employee`** | **One to Many** | Configures daily expected start/end times and break durations. |
| **`Site`** | `hosts` | **`Shift`** | **One to Many** | Physical store branch location where work shifts take place. |
| **`JobRole`** | `demands qualifications for` | **`Shift`** | **One to Many** | Shift requires employee to hold matching qualifications. |
| **`Employee`** | `assigned to` | **`Shift`** | **One to Many** | Work shift block assigned to an employee on the schedule board. |
| **`Shift`** | `listed in market as` | **`ShiftClaim`** | **One to Many** | Shift posted to the internal marketplace for claiming/swapping. |
| **`Employee`** | `claims` | **`ShiftClaim`** | **One to Many** | Teammate submitting a claim to cover a marketplace shift. |
| **`Employee`** | `owns compliance files` | **`EmployeeDocument`** | **One to Many** | Uploaded ID copies, medical leaves, and certificates. |
| **`Employee`** | `receives infraction` | **`DisciplinaryViolation`** | **One to Many** | Disciplinary violations logged by managers with attached evidence. |
| **`PointsRule`** | `triggers automation for` | **`PointsTransaction`** | **One to Many** | Rule logic evaluating clock-in delay to credit or debit wallet points. |
| **`Employee`** | `accumulates/spends` | **`PointsTransaction`** | **One to Many** | Ledger transactions modifying employee points balance. |
| **`RewardItem`** | `depletes stock for` | **`RewardRedemption`** | **One to Many** | Digital store voucher (Talabat, Noon) redeemed by employee points. |

---

## Interactive Mermaid Diagram

```mermaid
erDiagram

    Role ||--o{ Employee : "assigned to (One to Many)"
    JobRole ||--o{ Employee : "defines position for (One to Many)"
    Site ||--o{ Employee : "maps primary branch for (One to Many)"
    AttendanceProfile ||--o{ Employee : "dictates clock-in rules for (One to Many)"
    
    Site ||--o{ Shift : "hosts (One to Many)"
    JobRole ||--o{ Shift : "demands qualifications for (One to Many)"
    Employee ||--o{ Shift : "assigned to (One to Many)"
    
    Shift ||--o{ ShiftClaim : "listed in market as (One to Many)"
    Employee ||--o{ ShiftClaim : "claims (One to Many)"
    
    Employee ||--o{ EmployeeDocument : "owns compliance files (One to Many)"
    Employee ||--o{ DisciplinaryViolation : "receives infraction (One to Many)"
    
    Employee ||--o{ PointsTransaction : "accumulates/spends (One to Many)"
    PointsRule ||--o{ PointsTransaction : "triggers automation for (One to Many)"
    
    Employee ||--o{ RewardRedemption : "purchases (One to Many)"
    RewardItem ||--o{ RewardRedemption : "depletes stock for (One to Many)"

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
