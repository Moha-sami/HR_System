# Buy2 HRMS - High-Definition Database ERD Specification

Visual high-definition database schema architecture and entity relationships for the Buy2 HR Management System.

![Buy2 HRMS Visual Database ERD Diagram](./database_erd_diagram.jpg)

---

## Interactive Mermaid Diagram

```mermaid
erDiagram

    Role ||--o{ Employee : "assigned to"
    JobRole ||--o{ Employee : "defines position for"
    Site ||--o{ Employee : "maps primary branch for"
    AttendanceProfile ||--o{ Employee : "dictates clock-in rules for"
    
    Site ||--o{ Shift : "hosts"
    JobRole ||--o{ Shift : "demands qualifications for"
    Employee ||--o{ Shift : "assigned to"
    
    Shift ||--o{ ShiftClaim : "listed in market as"
    Employee ||--o{ ShiftClaim : "claims"
    
    Employee ||--o{ EmployeeDocument : "owns compliance files"
    Employee ||--o{ DisciplinaryViolation : "receives policy infraction"
    
    Employee ||--o{ PointsTransaction : "accumulates/spends"
    PointsRule ||--o{ PointsTransaction : "triggers automation for"
    
    Employee ||--o{ RewardRedemption : "purchases"
    RewardItem ||--o{ RewardRedemption : "depletes voucher inventory"

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
