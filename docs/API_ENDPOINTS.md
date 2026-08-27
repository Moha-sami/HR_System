# Buy2 HRMS - API Endpoints Documentation

This document outlines the REST API endpoints provided by the Buy2 HRMS backend (`Buy2.Api`).

---

## 1. Authentication (`/api/v1/auth`)

| Method | Route | Authorization | Description |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/auth/login` | AllowAnonymous | Authenticates user with credentials and returns JWT bearer token and employee profile. |
| `POST` | `/api/v1/auth/password/reset` | AllowAnonymous | Resets user password by email. |

---

## 2. Employee Directory (`/api/v1/employees`)

### `GET /api/v1/employees`
- **Authorization**: Authenticated users (`[Authorize]`)
- **Query Parameters**:
  - `page` (int, default: 1)
  - `pageSize` (int, default: 20)
  - `department` (string, optional) - Department ID or name filter (eager loads `JobRole.Department` and filters by integer ID or case-insensitive name substring)
  - `region` (string, optional) - Region ID or name filter (eager loads `Site.Region` and filters by integer ID or case-insensitive name substring)
  - `search` (string, optional) - Search text matching first/last name, email, or employee code
  - `sort` (string, optional) - Column to sort on (`name`, `employeecode`, `email`, `jobtitle`, `joindate`)
  - `sortDir` (string, optional) - Sort direction (`asc`, `desc`, default: `desc`)
- **Response**: `200 OK` with paginated list of employee records (`PaginatedList<EmployeeListItemDto>`).

### `GET /api/v1/employees/export`
- **Authorization**: `[Authorize(Roles = "Admin,Manager")]`
- **Query Parameters**:
  - `department` (string, optional) - Filter by department ID or title (eager loads `JobRole.Department`)
  - `region` (string, optional) - Filter by site / region name (eager loads `Site.Region`)
  - `search` (string, optional) - Search text matching first/last name, email, or employee code
  - `sort` (string, optional) - Column to sort by (`name`, `employeecode`, `email`, `jobtitle`, `joindate`)
  - `sortDir` (string, optional) - Sort direction (`asc`, `desc`, default: `desc`)
- **Response**: `200 OK` with `text/csv` binary file download (`employees.csv`), encoded with UTF-8 BOM for Microsoft Excel compatibility.
- **CSV Columns**:
  - `Employee Code`
  - `First Name`
  - `Last Name`
  - `Email`
  - `Phone`
  - `Job Title`
  - `Site`
  - `Join Date` (`yyyy-MM-dd`)
  - `Admin Access` (`true`/`false`)

### `GET /api/v1/employees/{id}`
- **Authorization**: `[Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]`
- **Path Parameters**:
  - `id` (int, required) - Unique ID of the employee
- **Description**: Retrieves full employee profile details for the Figma Information Tab, including personal information, job details (with eager-loaded `JobRole.Department`), qualifications, attendance/workdays breakdown, live gamification/task stats, and optional payroll summary.
- **Computed Header Stats**:
  - `totalPoints` - Sum of points from the points wallet ledger
  - `totalTasks` - Total number of assigned tasks
  - `totalGifts` - Total number of redeemed rewards/gifts
- **Response**:
  - `200 OK` with `EmployeeProfileDto`:
    - `id` (int) - Employee ID
    - `employeeCode` (string) - Unique employee code (e.g., `EMP-0001`)
    - `fullName` (string) - Formatted full name
    - `phone` (string) - Contact phone number
    - `email` (string) - Email address
    - `location` (string) - Primary work site name
    - `profilePhotoUrl` (string, nullable) - Profile avatar URL
    - `stats` (`EmployeeStatsDto`):
      - `totalPoints` (int) - Total reward points earned
      - `totalTasks` (int) - Total tasks assigned
      - `totalGifts` (int) - Total gifts/rewards redeemed
    - `personalInfo` (`EmployeePersonalInfoDto`):
      - `name` (string) - Full name
      - `birthdate` (DateTime/ISO 8601, nullable) - Date of birth
      - `email` (string) - Email address
      - `phoneNumber` (string) - Contact phone number
      - `gender` (`Gender` enum: `Male` = 1, `Female` = 2)
    - `jobDetails` (`EmployeeJobDetailsDto`):
      - `title` (string) - Job role title
      - `department` (string) - Department name mapped from `JobRole.Department.Name` (defaults to `"N/A"` if unassigned)
      - `seniorityLevel` (string) - Seniority level designation
      - `experienceYears` (int) - Years of professional experience
      - `directManagerName` (string, nullable) - Direct manager full name
      - `jobType` (string) - Employment type (e.g. `FullTime`, `PartTime`)
      - `qualifications` (array of strings) - Required and verified qualifications
      - `attendanceType` (string) - Work model (e.g. `OnSite`, `Remote`, `Hybrid`)
      - `onlineWorkdays` (array of strings) - Days assigned for remote work
      - `offlineWorkdays` (array of strings) - Days assigned for on-site work
    - `payroll` (`EmployeePayrollSummaryDto`, nullable):
      - `salaryType` (string) - Compensation structure (`Fixed`, `Hourly`)
      - `paymentAmount` (decimal) - Base salary or hourly rate
      - `payoutPeriod` (string) - Payout frequency (e.g. `Monthly`, `Bi-Weekly`)
      - `payoutDay` (int, nullable) - Scheduled payout day of month
      - `workWeekStartDay` (string, nullable) - Work week start day
      - `workWeekEndDay` (string, nullable) - Work week end day
      - `overtimeEnabled` (bool) - Whether overtime compensation is enabled
      - `overtimeThresholdHours` (decimal, nullable) - Overtime threshold in hours
      - `overtimeRateMultiplier` (decimal, nullable) - Overtime rate multiplier / hourly rate
      - `assignedWorkSiteIds` (array of int) - Work site IDs assigned to employee
  - `404 Not Found` if employee with specified `id` does not exist or has been soft-deleted.
  - `401 Unauthorized` if the request is unauthenticated.
  - `403 Forbidden` if authenticated user lacks required administrative role (`Admin`, `Manager`, `HR`, `SuperAdmin`).

**Example Response**:
```json
{
  "id": 1,
  "employeeCode": "EMP-0001",
  "fullName": "Sarah Jenkins",
  "phone": "+1-555-0199",
  "email": "sarah.jenkins@buy2.com",
  "location": "Main Campus",
  "profilePhotoUrl": "https://storage.buy2.com/photos/emp-0001.jpg",
  "stats": {
    "totalPoints": 350,
    "totalTasks": 12,
    "totalGifts": 3
  },
  "personalInfo": {
    "name": "Sarah Jenkins",
    "birthdate": "1992-05-14T00:00:00Z",
    "email": "sarah.jenkins@buy2.com",
    "phoneNumber": "+1-555-0199",
    "gender": 1
  },
  "jobDetails": {
    "title": "Senior Store Associate",
    "department": "Department #2",
    "seniorityLevel": "Senior",
    "experienceYears": 5,
    "directManagerName": "Michael Scott",
    "jobType": "FullTime",
    "qualifications": [
      "Customer Service Excellence",
      "Inventory Management"
    ],
    "attendanceType": "Hybrid",
    "onlineWorkdays": ["Monday", "Wednesday"],
    "offlineWorkdays": ["Tuesday", "Thursday", "Friday"]
  },
  "payroll": {
    "salaryType": "Fixed",
    "paymentAmount": 4500.00,
    "payoutPeriod": "Monthly",
    "payoutDay": 25,
    "workWeekStartDay": "Sunday",
    "workWeekEndDay": "Thursday",
    "overtimeEnabled": true,
    "overtimeThresholdHours": 40.0,
    "overtimeRateMultiplier": 1.5,
    "assignedWorkSiteIds": [1, 2]
  }
}
```

### `GET /api/v1/employees/{id}/payroll`
- **Authorization**: `[Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]`
- **Path Parameters**:
  - `id` (int, required) - Unique ID of the employee
- **Description**: Retrieves employee payroll profile, compensation model, payment amount, overtime rates, work week schedules, assigned work site IDs, attendance type, and parsed online/offline workdays. If an employee does not yet have a configured payroll record, default unconfigured values (`IsConfigured = false`) are returned.
- **Response**:
  - `200 OK` with `EmployeePayrollProfileDto`:
    - `employeeId` (int)
    - `isConfigured` (bool)
    - `salaryType` (SalaryType enum / string: `Fixed`, `Hourly`)
    - `payoutPeriod` (string)
    - `payoutDay` (int)
    - `workWeekStart` (DayOfWeek enum: `Sunday`, `Monday`, etc.)
    - `workWeekEnd` (DayOfWeek enum: `Thursday`, `Friday`, etc.)
    - `paymentAmount` (decimal)
    - `overtimeThresholdHours` (decimal)
    - `overtimeHourlyRate` (decimal)
    - `attendanceType` (string)
    - `workSiteIds` (array of int)
    - `onlineWorkdays` (array of strings)
    - `offlineWorkdays` (array of strings)
    - `payrollRecords` (array of `PayrollRecordDto` objects with fields: `id`, `employeeId`, `baseSalary`, `overtimePay`, `bonusPay`, `deductions`, `netPay`, `periodStartDate`, `periodEndDate`, `paymentStatus`, `paidAt`)
  - `404 Not Found` if employee with specified `id` does not exist or has been soft-deleted.
  - `401 Unauthorized` if unauthenticated.
  - `403 Forbidden` if authenticated user lacks required administrative role (`Admin`, `Manager`, `HR`, `SuperAdmin`).

### `PUT /api/v1/employees/{id}/payroll`
- **Authorization**: `[Authorize(Roles = "Admin,Manager")]`
- **Path Parameters**:
  - `id` (int, required) - Unique ID of the employee
- **Request Body** (`application/json`, `UpdatePayrollProfileDto`):
  - `salaryType` (SalaryType enum / string: `Fixed`, `Hourly`, optional) - Compensation model
  - `payoutPeriod` (string, optional) - Payout schedule period (e.g., `Monthly`, `Bi-Weekly`)
  - `payoutDay` (int, optional) - Payout day of the month/cycle
  - `workWeekStart` (DayOfWeek enum: `Sunday`, `Monday`, etc., optional) - Starting day of work week
  - `workWeekEnd` (DayOfWeek enum: `Thursday`, `Friday`, etc., optional) - Ending day of work week
  - `paymentAmount` (decimal, optional) - Base salary or hourly wage amount
  - `overtimeThresholdHours` (decimal, optional) - Overtime threshold in hours
  - `overtimeHourlyRate` (decimal, optional) - Overtime hourly compensation rate
  - `attendanceType` (string, optional) - Attendance arrangement (e.g., `On-Site`, `Remote`, `Hybrid`)
  - `workSiteIds` (array of int, optional) - List of assigned work site IDs (synchronizes `EmployeeSite` join table)
  - `onlineWorkdays` (array of strings, optional) - Days designated for remote / online work
  - `offlineWorkdays` (array of strings, optional) - Days designated for on-site / offline work
- **Description**: Updates or upserts the employee's payroll profile and work schedule settings. Synchronizes the employee's assigned work sites in the `EmployeeSite` join table and validates that all specified site IDs exist. Serializes online and offline workdays to JSON across both `Employee` and `PayrollProfile` records. All changes are committed atomically via `IUnitOfWork`.
- **Responses**:
  - `204 No Content` on successful payroll profile update/upsert.
  - `400 Bad Request` if one or more specified `workSiteIds` do not exist.
  - `404 Not Found` if employee with specified `id` does not exist, is inactive, or has been soft-deleted.
  - `401 Unauthorized` if the request is unauthenticated.
  - `403 Forbidden` if authenticated user does not have `Admin` or `Manager` role.


### `DELETE /api/v1/employees/{id}`
- **Authorization**: `[Authorize(Roles = "Admin,SuperAdmin")]`
- **Path Parameters**:
  - `id` (int, required) - Unique ID of the employee
- **Description**: Soft deletes / deactivates an employee by setting `IsDeleted = true`, `DeletedAt = DateTimeOffset.UtcNow`, and `IsActive = false`. Preserves related historical records (payroll, points, tasks, and disciplinary actions). Soft-deleted records are automatically filtered out from standard queries via EF Core global query filters.
- **Responses**:
  - `204 No Content` on successful soft deletion / deactivation.
  - `404 Not Found` if employee with specified `id` does not exist or has already been soft-deleted.
  - `401 Unauthorized` if the request is unauthenticated.
  - `403 Forbidden` if authenticated user does not have `Admin` or `SuperAdmin` role.

### `PUT /api/v1/employees/{id}/personal`
- **Authorization**: `[Authorize(Roles = "Admin,Manager")]`
- **Path Parameters**:
  - `id` (int, required) - Unique ID of the employee
- **Request Body** (`application/json`, `UpdateEmployeePersonalInfoDto`):
  - `firstName` (string, optional) - Employee first name
  - `lastName` (string, optional) - Employee last name
  - `phoneNumber` (string, optional) - Contact phone number
  - `dateOfBirth` (string/DateTimeOffset, optional) - Date of birth (ISO 8601)
  - `address` (string, optional) - Residential address (max 250 chars)
  - `emergencyContact` (string, optional) - Emergency contact info (max 100 chars)
  - `nationalId` (string, optional) - National identification number (max 50 chars)
- **Description**: Performs a partial update on the employee's personal info. Only non-null fields provided in the request payload are updated on the target record.
- **Responses**:
  - `204 No Content` on successful personal info update.
  - `404 Not Found` if employee with specified `id` does not exist or has been soft-deleted.
  - `401 Unauthorized` if the request is unauthenticated.
  - `403 Forbidden` if authenticated user does not have `Admin` or `Manager` role.

### `PUT /api/v1/employees/{id}/job`
- **Authorization**: `[Authorize(Roles = "Admin,Manager")]`
- **Path Parameters**:
  - `id` (int, required) - Unique ID of the employee
- **Request Body** (`application/json`, `UpdateJobDetailsDto`):
  - `jobRoleId` (int, optional) - Job role ID
  - `roleId` (int, optional) - System security role ID
  - `siteId` (int, optional) - Work site / branch ID
  - `directManagerId` (int, optional) - Direct manager employee ID (cannot be the employee themselves)
  - `seniorityLevel` (string, optional) - Seniority level designation (e.g., Junior, Mid, Senior, Lead)
  - `experienceYears` (int, optional) - Years of experience
  - `jobType` (string, optional) - Employment type (e.g., Full-Time, Part-Time, Contract)
  - `attendanceType` (string, optional) - Work arrangement / attendance model (e.g., On-Site, Remote, Hybrid)
  - `joinDate` (string/DateTimeOffset, optional) - Date employee joined (ISO 8601)
- **Description**: Performs a partial update on the employee's job and role details. Validates referenced foreign keys (`JobRole`, `Role`, `Site`, `DirectManager`) and prevents self-manager assignment. Only non-null fields provided in the request payload are updated.
- **Responses**:
  - `204 No Content` on successful job details update.
  - `400 Bad Request` if any referenced foreign entity ID (`JobRole`, `Role`, `Site`, `DirectManager`) does not exist or if `directManagerId` matches `id`.
  - `404 Not Found` if employee with specified `id` does not exist or has been soft-deleted.
  - `401 Unauthorized` if the request is unauthenticated.
  - `403 Forbidden` if authenticated user does not have `Admin` or `Manager` role.

### `GET /api/v1/employees/{id}/performance/overview`
- **Authorization**: Authenticated users (`[Authorize]`)
- **Path Parameters**:
  - `id` (int, required) - Unique ID of the employee
- **Query Parameters**:
  - `period` (string, optional) - Predefined time period filter (`today`, `thisWeek`, `thisMonth`, `thisYear`)
  - `days` (int, optional) - Number of trailing days for rolling window calculation
  - `from` (DateTimeOffset/ISO 8601, optional) - Custom range start date
  - `to` (DateTimeOffset/ISO 8601, optional) - Custom range end date
- **Description**: Returns employee performance analytics and summary metrics over the specified time range. Computes weighted performance scores, descriptive rating labels (`Needs Improvement`, `Satisfactory`, `Good Performance`, `Excellent`), task tracking & deadline compliance percentage, awarded achievement badges with rich metadata (eager loading `Badge` entity with legacy `BadgeType` string record fallback), chronological daily score trend points, and detailed individual submission records. Uses `.AsNoTracking()` for EF queries. Soft-deleted or non-existent employees return `404 Not Found`.
- **Responses**:
  - `200 OK` with `PerformanceOverviewDto`:
    - `employeeId` (int)
    - `dateRangeResolved` (`DateRangeResolvedDto`: `from`, `to`, `period`)
    - `overallWeightedScore` (decimal)
    - `ratingLabel` (string: `Needs Improvement`, `Satisfactory`, `Good Performance`, `Excellent`)
    - `tasksSummary` (`TasksSummaryDto`: `totalTasks`, `todoCount`, `inProgressCount`, `completedCount`, `overdueCount`, `deadlineCompliancePercentage`)
    - `achievements` (array of `AchievementBadgeDto`: `id`, `title`, `description`, `iconUrl`, `pointsAwarded`, `earnedAt`)
    - `chartTrendPoints` (array of `ChartPointDto`: `date`, `score`)
    - `submissionsDetail` (array of `SubmissionDetailDto`: `id`, `metricName`, `score`, `weight`, `submittedAt`, `notes`)
  - `400 BadRequest` if `period` is invalid, `days` is outside valid bounds (1 to 3650), or `from`/`to` span exceeds 3650 days.
  - `404 Not Found` if employee with specified `id` does not exist or has been soft-deleted.
  - `401 Unauthorized` if the request is unauthenticated.

### `GET /api/v1/employees/{id}/performance/metrics/{metricId}`
- **Authorization**: Authenticated users (`[Authorize]`)
- **Path Parameters**:
  - `id` (int, required) - Unique ID of the employee
  - `metricId` (int, required) - Unique ID of the performance metric
- **Query Parameters**:
  - `period` (string, optional) - Predefined time period filter (`today`, `thisWeek`, `thisMonth`, `thisYear`)
  - `days` (int, optional) - Number of trailing days for rolling window calculation (1 to 3650)
  - `from` (DateTimeOffset/ISO 8601, optional) - Custom range start date
  - `to` (DateTimeOffset/ISO 8601, optional) - Custom range end date
- **Description**: Returns detailed performance metric evaluation, all-time score, period-filtered score and rating, chronological monthly trend aggregation, and historical submission items with notes and evaluator name (direct manager).
- **Responses**:
  - `200 OK` with `MetricDetailDto`:
    - `employeeId` (int)
    - `metricId` (int)
    - `metricName` (string)
    - `metricDescription` (string)
    - `weight` (decimal)
    - `targetScore` (decimal)
    - `unit` (string, e.g. `%`)
    - `allTimeAverageScore` (decimal)
    - `periodAverageScore` (decimal)
    - `periodRatingLabel` (string: `Needs Improvement`, `Satisfactory`, `Good Performance`, `Excellent`)
    - `dateRangeResolved` (`DateRangeResolvedDto`: `from`, `to`, `period`)
    - `monthlyTrends` (array of `MonthlyTrendPointDto`: `year`, `month`, `yearMonthLabel`, `averageScore`, `submissionCount`)
    - `submissions` (array of `MetricSubmissionItemDto`: `id`, `score`, `submittedAt`, `notes`, `evaluatorName`)
  - `400 Bad Request` if `period` is invalid, `days` is outside valid bounds (1 to 3650), or `from`/`to` span exceeds 3650 days.
  - `404 Not Found` if employee with specified `id` does not exist or has been soft-deleted, or if metric with specified `metricId` does not exist.
  - `401 Unauthorized` if the request is unauthenticated.

### `GET /api/v1/employees/{id}/tasks`
- **Authorization**: Authenticated users (`[Authorize]`)
- **Path Parameters**:
  - `id` (int, required) - Unique ID of the employee
- **Query Parameters**:
  - `status` (string, optional) - Filter by task status (`Todo`, `InProgress`, `InReview`, `Completed`, `Overdue`)
- **Description**: Returns all tasks assigned to the specified employee, ordered by due date (ascending) and creation timestamp (descending). Supports optional filtering by task status.
- **Responses**:
  - `200 OK` with array of `EmployeeTaskDto`:
    - `id` (int)
    - `employeeId` (int)
    - `title` (string)
    - `description` (string)
    - `status` (string)
    - `priority` (string, null)
    - `dueDate` (string/DateTimeOffset, null)
    - `completedAt` (string/DateTimeOffset, null)
    - `createdAt` (string/DateTimeOffset)
  - `404 Not Found` if employee with specified `id` does not exist or has been soft-deleted.
  - `401 Unauthorized` if the request is unauthenticated.

### `GET /api/v1/employees/{id}/attendance/calendar`
- **Authorization**: Authenticated users (`[Authorize]`)
- **Path Parameters**:
  - `id` (int, required) - Unique ID of the employee
- **Query Parameters**:
  - `month` (int, optional) - Calendar month (1 to 12, defaults to current UTC month)
  - `year` (int, optional) - Calendar year (1900 to 2100, defaults to current UTC year)
- **Description**: Returns a full monthly attendance calendar breakdown for the specified employee and month/year, including overall summary metrics (`AttendanceRate`, `PunctualityScore`, `AverageLatenessMinutes`, `RecordedHours`, `TargetHours`) and daily cell details (`Date`, `Status`, `LeaveType`, `HoursWorked`, `HoursLeft`, `OtHours`, `BreakTime`, `LatenessMinutes`). Eagerly loads `ScheduledShift` on `AttendanceRecord` and queries `ShiftEntity` to dynamically compute scheduled shift hours and target break times (e.g. 60m break for >=8h shifts, 30m for >=4h shifts). Queries approved `Request` / `RequestType` records for the employee during the requested month and maps approved leave requests (e.g. Sick Leave, Approved Leave, Remote Work) directly onto calendar days where attendance records are absent or marked as leave. Uses `.AsNoTracking()` for EF queries. Non-existent or soft-deleted employees return `404 Not Found`.
- **Responses**:
  - `200 OK` with `AttendanceCalendarDto`:
    - `summary` (`AttendanceSummaryDto`):
      - `attendanceRate` (decimal) - Percentage of attended workdays
      - `punctualityScore` (decimal) - Percentage of on-time days among attended days
      - `averageLatenessMinutes` (decimal) - Average minutes late for late days
      - `recordedHours` (decimal) - Total recorded hours worked in the month
      - `targetHours` (decimal) - Total expected hours based on standard workdays (8h/day)
    - `days` (array of `AttendanceDayDto`):
      - `date` (DateTime/ISO 8601) - UTC date of the day
      - `status` (`AttendanceDayStatus` enum: `OnTime` = 1, `Late` = 2, `ApprovedLeave` = 3, `UnapprovedLeave` = 4, `PartialLeave` = 5, `NoAttendance` = 6, `PublicHoliday` = 7, `AttendanceNotRequired` = 8)
      - `leaveType` (string, optional/nullable)
      - `hoursWorked` (decimal) - Hours worked on the day
      - `hoursLeft` (decimal) - Remaining hours to meet daily target (8h)
      - `otHours` (decimal) - Overtime hours worked
      - `breakTime` (decimal) - Break time in minutes
      - `latenessMinutes` (int, optional/nullable) - Minutes late relative to scheduled start
  - `404 Not Found` if employee with specified `id` does not exist or has been soft-deleted.
  - `401 Unauthorized` if the request is unauthenticated.

### `GET /api/v1/employees/{id}/points/summary`
- **Authorization**: Authenticated users (`[Authorize]`)
- **Path Parameters**:
  - `id` (int, required) - Unique ID of the employee
- **Description**: Returns employee gamification points summary including current point balance, total points redeemed, total rewards redeemed count, and total rewards cost in points. Non-existent or soft-deleted employees return `404 Not Found`.
- **Responses**:
  - `200 OK` with `PointsSummaryDto`:
    - `currentBalance` (int) - Current balance of points (earned positive, spent/deducted negative)
    - `totalPointsRedeemed` (int) - Total points redeemed across all redemption transactions
    - `totalRewardsRedeemed` (int) - Total count of rewards redeemed
    - `totalRewardsCostPoints` (int) - Total cost in points for all redeemed rewards
  - `404 Not Found` if employee with specified `id` does not exist or has been soft-deleted.
  - `401 Unauthorized` if the request is unauthenticated.

### `GET /api/v1/employees/{id}/points/transactions`
- **Authorization**: Authenticated users (`[Authorize]`)
- **Path Parameters**:
  - `id` (int, required) - Unique ID of the employee
- **Query Parameters**:
  - `page` (int, default: 1) - Page number (minimum 1)
  - `pageSize` (int, default: 10) - Items per page (clamped between 1 and 100)
  - `type` (string, optional) - Filter by transaction type (`Earned`, `Redeemed`, or exact transaction type string)
  - `triggeredBy` (string, optional) - Filter text matching rule key, event type, or transaction type
  - `dateFrom` (DateTimeOffset/ISO 8601, optional) - Filter transactions created on or after this timestamp
  - `dateTo` (DateTimeOffset/ISO 8601, optional) - Filter transactions created on or before this timestamp
- **Description**: Returns paginated points ledger transaction history for the specified employee, ordered by creation date descending. Supports filtering by transaction type, triggering reason/rule, and date bounds. Non-existent or soft-deleted employees return `404 Not Found`.
- **Responses**:
  - `200 OK` with `PaginatedPointsTransactionsDto`:
    - `items` (array of `PointsTransactionDto`):
      - `id` (int) - Transaction ID
      - `date` (DateTimeOffset/ISO 8601) - Timestamp when transaction was recorded (UTC)
      - `amount` (int) - Points amount (positive for earned/awarded, negative for spent/deducted)
      - `type` (string) - Transaction category (`Earned`, `Redeemed`, or specific type)
      - `triggeredBy` (string, nullable) - Rule key or triggering event description
      - `comments` (string, nullable) - Additional event context or comments
    - `totalCount` (int) - Total number of matching transactions
    - `page` (int) - Current page number
    - `pageSize` (int) - Page size
    - `totalPages` (int) - Total calculated page count
  - `404 Not Found` if employee with specified `id` does not exist or has been soft-deleted.
  - `401 Unauthorized` if the request is unauthenticated.

### `GET /api/v1/employees/{id}/violations`
- **Authorization**: Authenticated users (`[Authorize]`)
- **Path Parameters**:
  - `id` (int, required) - Unique ID of the employee
- **Query Parameters**:
  - `type` (string, optional) - Filter by violation type (e.g., `Attendance`, `Behavioral`, `Performance`, `PolicyViolation`, etc.)
  - `severityLevel` (string, optional) - Filter by severity level (e.g., `Low`, `Medium`, `High`, `Critical`)
  - `dateFrom` (DateTimeOffset/ISO 8601, optional) - Filter violations created on or after this timestamp
  - `dateTo` (DateTimeOffset/ISO 8601, optional) - Filter violations created on or before this timestamp
- **Description**: Returns disciplinary violation history records for the specified employee, ordered by creation date descending. Supports optional filtering by violation type enum, severity level string, and date range bounds. Eagerly loads reporting supervisor details (`ReportedBy`) with fallback formatting. Non-existent or soft-deleted employees return `404 Not Found`.
- **Responses**:
  - `200 OK` with array of `ViolationDto`:
    - `id` (int) - Disciplinary violation ID
    - `employeeId` (int) - Target employee ID
    - `type` (string) - Violation type name
    - `severity` (string) - Severity level description/string
    - `description` (string) - Violation incident details / explanation
    - `status` (string) - Status name (e.g., `Pending`, `Approved`, `Rejected`, `Resolved`)
    - `reportedByName` (string) - Full name of the reporter or `"System"` fallback
    - `createdAt` (DateTimeOffset/ISO 8601) - Timestamp when violation was recorded (UTC)
    - `actionType` (string, nullable) - Disciplinary or corrective action applied
    - `actionDate` (DateTime/ISO 8601, nullable) - Date when corrective action took effect
    - `documentUrl` (string, nullable) - URL/path to supporting documentation/attachment
  - `404 Not Found` if employee with specified `id` does not exist or has been soft-deleted.
  - `401 Unauthorized` if the request is unauthenticated.

### `GET /api/v1/employees/{id}/violations/{violationId}`
- **Authorization**: Authenticated users (`[Authorize]`)
- **Path Parameters**:
  - `id` (int, required) - Unique ID of the employee
  - `violationId` (int, required) - Unique ID of the disciplinary violation
- **Description**: Retrieves detailed disciplinary violation record for the specified employee and violation ID. Eagerly loads reporter (`ReportedBy`) and action taker (`ActionTakenBy`) navigations. Safely parses `WitnessesJson` into a string list and conditionally provides `ActionDetail` (null if status is `Pending` or no action recorded). Returns `404 Not Found` if employee or violation does not exist, if employee is soft-deleted, or if violation belongs to a different employee.
- **Responses**:
  - `200 OK` with `ViolationDetailDto`:
    - `id` (int) - Violation ID
    - `employeeId` (int) - Employee ID
    - `violationType` (string) - Type of violation
    - `severity` (string) - Severity level description
    - `description` (string) - Incident description
    - `status` (string) - Violation status (`Pending`, `UnderInvestigation`, `Resolved`)
    - `reportedByName` (string) - Reporter full name or `"System"` fallback
    - `witnesses` (array of strings) - Witness names/descriptions parsed from JSON
    - `documentUrl` (string, nullable) - Supporting document URL
    - `createdAt` (DateTimeOffset/ISO 8601) - Timestamp when violation was recorded (UTC)
    - `actionDetail` (`ViolationActionDetailDto`, nullable):
      - `actionType` (string, nullable) - Corrective or disciplinary action type
      - `actionDate` (DateTime/ISO 8601, nullable) - Date action was taken
      - `actionTakenByName` (string, nullable) - Full name of supervisor/manager who took action
      - `actionDescription` (string, nullable) - Description of the action taken
  - `404 Not Found` if employee or violation is not found, employee is soft-deleted, or violation does not belong to the employee.
  - `401 Unauthorized` if the request is unauthenticated.

### `PATCH /api/v1/employees/{id}/violations/{violationId}/resolve`
- **Authorization**: `[Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]`
- **Path Parameters**:
  - `id` (int, required) - Unique ID of the employee
  - `violationId` (int, required) - Unique ID of the disciplinary violation
- **Request Body** (`application/json`, `ResolveViolationDto`):
  - `actionType` (string, required) - Type of disciplinary/corrective action taken
  - `actionDescription` (string, required) - Description of the corrective action taken
  - `actionDate` (DateTime/ISO 8601, optional) - Date and time when the action was taken (defaults to UTC now if omitted)
  - `actionTakenById` (int, optional) - Employee ID of the supervisor or manager who took the action
- **Description**: Resolves an employee disciplinary violation by updating its status to `Resolved` and recording action metadata (`ActionType`, `ActionDescription`, `ActionDate`, `ActionTakenById`). Returns `400 Bad Request` if the violation is already resolved. Returns `404 Not Found` if the employee does not exist, is soft-deleted, or if the violation is not found for the employee.
- **Responses**:
  - `204 No Content` on successful resolution of the violation.
  - `400 Bad Request` if the violation is already marked as resolved.
  - `404 Not Found` if employee or violation is not found, or employee is soft-deleted.
  - `401 Unauthorized` if the request is unauthenticated.
  - `403 Forbidden` if authenticated user lacks required administrative role (`Admin`, `Manager`, `HR`, `SuperAdmin`).

### `GET /api/v1/employees/{id}/violations/export`
- **Authorization**: `[Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]`
- **Path Parameters**:
  - `id` (int, required) - Unique ID of the employee
- **Query Parameters**:
  - `type` (string, optional) - Filter by violation type (e.g., `Attendance`, `Behavioral`, `Performance`, `PolicyViolation`, etc.)
  - `severityLevel` (string, optional) - Filter by severity level (e.g., `Low`, `Medium`, `High`, `Critical`)
  - `dateFrom` (DateTimeOffset/ISO 8601, optional) - Filter violations created on or after this timestamp
  - `dateTo` (DateTimeOffset/ISO 8601, optional) - Filter violations created on or before this timestamp
- **Description**: Generates and downloads an RFC-4180-compliant CSV export (`employee_{id}_violations.csv`) containing disciplinary violation records for the specified employee with UTF-8 BOM preamble for Microsoft Excel compatibility. Supports filtering by violation type, severity level, and date range bounds. Eagerly loads reporter (`ReportedBy`) and action taker (`ActionTakenBy`) details. Non-existent or soft-deleted employees return `404 Not Found`.
- **Responses**:
  - `200 OK` with `text/csv` binary file download (`employee_{id}_violations.csv`).
  - `404 Not Found` if employee with specified `id` does not exist or has been soft-deleted.
  - `401 Unauthorized` if the request is unauthenticated.
  - `403 Forbidden` if authenticated user lacks required administrative role (`Admin`, `Manager`, `HR`, `SuperAdmin`).
- **CSV Columns**:
  - `Violation ID`
  - `Violation Type`
  - `Severity`
  - `Status`
  - `Description`
  - `Reported By`
  - `Action Type`
  - `Action Date`
  - `Action Taken By`
  - `Action Description`
  - `Created At`

---

## 3. Role Management (`/api/v1/roles`)

### `GET /api/v1/roles`
- **Authorization**: `[Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]`
- **Controller**: `RolesController`
- **Query Parameters**:
  - `searchTerm` (string, optional) - Filters roles by `Name` or `Description` (case-insensitive substring match)
  - `isActive` (bool, optional) - Filters roles by active status (`true` / `false`)
  - `pageNumber` (int, default: `1`, min: `1`) - Page number for pagination
  - `pageSize` (int, default: `10`, range: `1..100`) - Items per page
- **Description**: Retrieves a paginated list of system roles with assigned active employee count metrics and summary list of permission modules. Results are ordered by system roles first (`IsSystemRole` descending), then alphabetically by name (`Name` ascending). Ignores EF Core global query filters to retrieve complete role definitions.
- **Responses**:
  - `200 OK` with `RolePaginatedResponseDto`:
    - `items` (array of `RoleListItemDto`):
      - `id` (int) - Unique role ID
      - `name` (string) - Role display name
      - `description` (string, nullable) - Role description
      - `assignedEmployeesCount` (int) - Count of active, non-deleted assigned employees (`!IsDeleted`)
      - `isSystemRole` (bool) - Flag indicating whether the role is a built-in system role
      - `isActive` (bool) - Role active status flag
      - `createdAt` (DateTimeOffset/ISO 8601) - Role creation timestamp (UTC)
      - `permissionsSummary` (array of strings) - Summary list of permission modules/actions parsed from JSON
    - `totalCount` (int) - Total count of matching roles across all pages
    - `pageNumber` (int) - Current page number
    - `pageSize` (int) - Page size
    - `totalPages` (int) - Total calculated page count
  - `401 Unauthorized` if the request is unauthenticated.
  - `403 Forbidden` if authenticated user lacks required administrative role (`Admin`, `Manager`, `HR`, `SuperAdmin`).

**Example Response**:
```json
{
  "items": [
    {
      "id": 1,
      "name": "SuperAdmin",
      "description": "System administrator with full permissions",
      "assignedEmployeesCount": 3,
      "isSystemRole": true,
      "isActive": true,
      "createdAt": "2026-01-01T00:00:00Z",
      "permissionsSummary": [
        "EmployeeManagement",
        "JobManagement",
        "SiteManagement",
        "PointsManagement",
        "NotificationsManagement",
        "RewardManagement"
      ]
    },
    {
      "id": 2,
      "name": "HR Manager",
      "description": "Human resources management role",
      "assignedEmployeesCount": 12,
      "isSystemRole": false,
      "isActive": true,
      "createdAt": "2026-02-15T10:30:00Z",
      "permissionsSummary": [
        "EmployeeManagement",
        "JobManagement"
      ]
    }
  ],
  "totalCount": 2,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

### `GET /api/v1/roles/lookup`
- **Authorization**: `[Authorize]` (authenticated users)
- **Controller**: `RolesController`
- **Query Parameters**:
  - `excludeRoleId` (int, optional) - Optional role ID to exclude from the returned list (useful for populating replacement role dropdowns)
- **Description**: Retrieves a lightweight list of active roles (`id`, `name`) for UI selection dropdowns and replacement role selectors. Filters active roles (`IsActive == true`), orders them alphabetically by name, and supports excluding a specified role ID.
- **Responses**:
  - `200 OK` with `List<RoleLookupItemDto>`:
    - `id` (int) - Unique role ID
    - `name` (string) - Role display name
  - `401 Unauthorized` if the request is unauthenticated.

**Example Response**:
```json
[
  {
    "id": 1,
    "name": "SuperAdmin"
  },
  {
    "id": 2,
    "name": "HR Manager"
  },
  {
    "id": 3,
    "name": "Employee"
  }
]
```

### `GET /api/v1/roles/{id}`
- **Authorization**: `[Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]`
- **Controller**: `RolesController`
- **Path Parameters**:
  - `id` (int, required) - Unique ID of the role
- **Description**: Retrieves full role details by ID, including system role flag (`isSystemRole`), active status (`isActive`), count of active assigned employees (`assignedEmployeesCount`, filtering soft-deleted records `!e.IsDeleted`), creation and update timestamps (`createdAt`, `updatedAt`), and detailed list of module permissions with granted actions and access scopes (`permissions`). Queries the repository with `.IgnoreQueryFilters().AsNoTracking().Include(r => r.Employees)` and utilizes resilient multi-strategy permissions JSON parsing.
- **Responses**:
  - `200 OK` with `RoleDetailsDto`:
    - `id` (int) - Unique role ID
    - `name` (string) - Role display name
    - `description` (string, nullable) - Role description
    - `isSystemRole` (bool) - Flag indicating whether the role is a built-in system role
    - `isActive` (bool) - Role active status flag
    - `assignedEmployeesCount` (int) - Count of active, non-deleted assigned employees (`!e.IsDeleted`)
    - `createdAt` (DateTimeOffset/ISO 8601) - Role creation timestamp (UTC)
    - `updatedAt` (DateTimeOffset/ISO 8601, nullable) - Role last update timestamp (UTC)
    - `permissions` (array of `ModulePermissionDto`):
      - `module` (string) - Permission module name (e.g. `EmployeeManagement`)
      - `actions` (array of strings, nullable) - Granted actions list (e.g. `["Create", "Read", "Update", "Delete"]`)
      - `scope` (`PermissionScopeDto`, nullable):
        - `scopeType` (string) - Permission access scope type (e.g. `All`, `Department`, `Site`, `Self`)
        - `targetIds` (array of int, nullable) - Target entity IDs when scope is entity-restricted
  - `404 Not Found` if role with specified `id` does not exist.
  - `401 Unauthorized` if the request is unauthenticated.
  - `403 Forbidden` if authenticated user lacks required administrative role (`Admin`, `Manager`, `HR`, `SuperAdmin`).

**Example Response**:
```json
{
  "id": 2,
  "name": "HR Manager",
  "description": "Human resources management role",
  "isSystemRole": false,
  "isActive": true,
  "assignedEmployeesCount": 12,
  "createdAt": "2026-02-15T10:30:00Z",
  "updatedAt": "2026-08-20T14:20:00Z",
  "permissions": [
    {
      "module": "EmployeeManagement",
      "actions": [
        "Create",
        "Read",
        "Update",
        "Delete"
      ],
      "scope": {
        "scopeType": "All",
        "targetIds": []
      }
    },
    {
      "module": "JobManagement",
      "actions": [
        "Read",
        "Update"
      ],
      "scope": {
        "scopeType": "Department",
        "targetIds": [
          1,
          2
        ]
      }
    }
  ]
}
```

### `POST /api/v1/roles`
- **Authorization**: `[Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]`
- **Controller**: `RolesController`
- **Request Body** (`application/json`, `CreateRoleDto`):
  - `name` (string, required) - Role display name (trimmed, 2-100 characters, case-insensitive uniqueness validation)
  - `description` (string, optional) - Role description (max 500 characters)
  - `permissions` (array of `ModulePermissionDto`, required) - Granted permission modules list
    - `module` (string, required) - Permission module name (`EmployeeManagement`, `JobManagement`, `SiteManagement`, `PointsManagement`, `NotificationsManagement`, `RewardManagement`)
    - `actions` (array of strings, optional) - Granted action names list
    - `scope` (`PermissionScopeDto`, optional) - Access scope details (`scopeType`, `targetIds`)
- **Description**: Creates a new custom non-system role with case-insensitive uniqueness validation against existing role names (ignoring EF Core query filters). Forces `isSystemRole = false` and `isActive = true`. Trims name and description strings, serializes permissions to JSON, and maps the created role entity to `RoleDetailsDto`.
- **Responses**:
  - `201 Created` with `RoleDetailsDto` (and `Location` header pointing to `/api/v1/roles/{id}`):
    - `id` (int) - Assigned unique role ID
    - `name` (string) - Role display name
    - `description` (string, nullable) - Role description
    - `isSystemRole` (bool) - Always `false` for custom created roles
    - `isActive` (bool) - Always `true` for newly created roles
    - `assignedEmployeesCount` (int) - Initial assigned employee count (always `0`)
    - `createdAt` (DateTimeOffset/ISO 8601) - Role creation timestamp (UTC)
    - `updatedAt` (DateTimeOffset/ISO 8601, nullable) - Role last update timestamp (UTC)
    - `permissions` (array of `ModulePermissionDto`) - Granted permissions list
  - `400 Bad Request` on payload validation error (e.g. invalid name length, empty module list, unsupported action or scope).
  - `409 Conflict` if a role with the specified name already exists (case-insensitive check ignoring query filters).
  - `401 Unauthorized` if the request is unauthenticated.
  - `403 Forbidden` if authenticated user lacks required administrative role (`Admin`, `Manager`, `HR`, `SuperAdmin`).

**Example Request**:
```json
{
  "name": "Payroll Specialist",
  "description": "Custom role for managing payroll profiles and employee compensation records",
  "permissions": [
    {
      "module": "EmployeeManagement",
      "actions": [
        "Read",
        "Update"
      ],
      "scope": {
        "scopeType": "All",
        "targetIds": []
      }
    }
  ]
}
```

**Example Response**:
```json
{
  "id": 15,
  "name": "Payroll Specialist",
  "description": "Custom role for managing payroll profiles and employee compensation records",
  "isSystemRole": false,
  "isActive": true,
  "assignedEmployeesCount": 0,
  "createdAt": "2026-08-23T19:41:00Z",
  "updatedAt": null,
  "permissions": [
    {
      "module": "EmployeeManagement",
      "actions": [
        "Read",
        "Update"
      ],
      "scope": {
        "scopeType": "All",
        "targetIds": []
      }
    }
  ]
}
```

### `POST /api/v1/roles/{id}/reassign-and-delete`
- **Authorization**: `[Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]`
- **Controller**: `RolesController`
- **Path Parameters**:
  - `id` (int, required) - Unique ID of the role to delete/decommission
- **Request Body** (`application/json`, `ReassignUsersAndDeleteRoleDto`):
  - `defaultNewRoleId` (int, optional/nullable) - Default replacement role ID to assign to all unmapped active employees
  - `reassignments` (array of `EmployeeReassignmentDto`, optional/nullable):
    - `employeeId` (int, required) - ID of the assigned employee
    - `newRoleId` (int, required) - Target replacement role ID for this employee
- **Description**: Atomically reassigns all active non-deleted employees assigned to role `id` to their respective replacement roles (`reassignments` map or fallback `defaultNewRoleId`) and soft-deletes/decommissions the target role (`IsActive = false`, `UpdatedAt = UtcNow`) within an explicit database transaction (`BeginTransactionAsync`). Rejects deletion if the target role is a built-in system role (`IsSystemRole == true`), if unmapped employees exist, or if any replacement role ID is invalid, inactive, or equal to the target role `id`.
- **Responses**:
  - `200 OK` with `RoleDeletionResultDto`:
    - `success` (bool) - `true` upon successful atomic reassignment and role deletion
    - `deletedRoleId` (int) - ID of the deleted role
    - `reassignedEmployeesCount` (int) - Total count of employees reassigned to replacement roles
    - `message` (string) - Operation confirmation message
  - `400 Bad Request` if payload validation fails (e.g. unmapped employees remaining, invalid/inactive replacement role ID, or self-reassignment to target role).
  - `401 Unauthorized` if the request is unauthenticated.
  - `403 Forbidden` if authenticated user lacks required administrative role or attempts to delete a protected system role (`IsSystemRole == true`).
  - `404 Not Found` if role with specified `id` does not exist.

**Example Request**:
```json
{
  "defaultNewRoleId": 2,
  "reassignments": [
    {
      "employeeId": 101,
      "newRoleId": 3
    }
  ]
}
```

**Example Response**:
```json
{
  "success": true,
  "deletedRoleId": 15,
  "reassignedEmployeesCount": 5,
  "message": "Role deleted and users reassigned successfully."
}
```

---

## 8. Job Role Management (`/api/v1/jobs`) [SCRUM-277]

### `GET /api/v1/jobs`
- **Authorization**: `[Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]`
- **Controller**: `JobsController`
- **Query Parameters**:
  - `searchTerm` (string, optional) - Filters by title or department name matching substring.
  - `departmentId` (int, optional) - Filters job roles belonging to specific department ID.
  - `attendanceType` (string, optional) - Filters by work model (e.g. `Hybrid`, `OnSite`, `Remote`).
  - `isActive` (bool, optional) - Filters active or inactive job roles.
  - `pageNumber` (int, default: 1) - Page index.
  - `pageSize` (int, default: 10) - Page size capacity.
- **Description**: Returns a paginated list of job roles matching specified filter criteria. Computes live allocated employee count per job role.
- **Response**: `200 OK` with `JobPaginatedResponseDto<JobListItemDto>`.

### `GET /api/v1/jobs/{id}`
- **Authorization**: `[Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]`
- **Controller**: `JobsController`
- **Path Parameters**:
  - `id` (int, required) - Unique ID of the job role.
- **Description**: Returns detailed job role information including department details, seniority level, experience years, allocated employee count, and parsed JSON lists for online workdays, offline workdays, and required qualifications.
- **Responses**:
  - `200 OK` with `JobDetailsDto`.
  - `404 Not Found` if job role with specified `id` does not exist.

### `GET /api/v1/jobs/{id}/employees` [SCRUM-282]
- **Authorization**: `[Authorize(Roles = "Admin,Manager,HR,SuperAdmin")]`
- **Controller**: `JobsController`
- **Route**: `GET /api/v1/jobs/{id}/employees`
- **Path Parameters**:
  - `id` (int, required) - Unique ID of the job role.
- **Query Parameters**:
  - `searchTerm` (string, optional) - Filters employees by case-insensitive substring search matching `firstName`, `lastName`, `email`, or `employeeCode`.
  - `pageNumber` (int, optional, default: 1, min: 1) - Page number for pagination.
  - `pageSize` (int, optional, default: 10, range: 1..100) - Items per page.
- **Description**: Returns a paginated roster of active, non-deleted employees assigned to the specified job role. Eager loads employee `Site` and `JobRole.Department` navigation properties. Validates job role existence, returning HTTP 404 if the job role does not exist. Orders records alphabetically by `FirstName` ascending, then `LastName` ascending.
- **Business Rules & Processing**:
  1. Validates that the target `JobRole` exists in the database (`AnyAsync(j => j.Id == id)`). If missing, returns `404 Not Found`.
  2. Queries active employees (`!e.IsDeleted && e.JobRoleId == id`) with `.AsNoTracking()`.
  3. Includes `Site` and `JobRole.Department` for site and department resolution.
  4. If `searchTerm` is provided, applies case-insensitive substring matching against `FirstName`, `LastName`, `Email`, and `EmployeeCode`.
  5. Computes total count and paginates using `Skip((pageNumber - 1) * pageSize).Take(pageSize)`.
  6. Maps each employee entity into `JobAssignedEmployeeListItemDto` with fallback `"N/A"` for null or empty values.
- **Responses**:
  - `200 OK` with `JobPaginatedResponseDto<JobAssignedEmployeeListItemDto>`:
    - `items` (array of `JobAssignedEmployeeListItemDto`):
      - `id` (int) - Employee unique identifier
      - `employeeCode` (string) - Employee identification code (e.g., `"EMP-0012"`)
      - `fullName` (string) - Formatted full name (`FirstName LastName`)
      - `email` (string) - Corporate email address
      - `departmentName` (string) - Department name mapped from `JobRole.Department.Name` (`"N/A"` if unassigned)
      - `siteName` (string) - Assigned primary work site name (`Site.SiteName` or `"N/A"`)
      - `joinDate` (DateTime/ISO 8601, nullable) - Date employee joined the company
      - `profilePhotoUrl` (string, nullable) - Profile avatar URL
    - `totalCount` (int) - Total count of matching assigned employees
    - `pageNumber` (int) - Current page index
    - `pageSize` (int) - Items per page capacity
    - `totalPages` (int) - Calculated total page count
  - `404 Not Found` if job role with specified `id` does not exist.
  - `401 Unauthorized` if the request is unauthenticated.
  - `403 Forbidden` if authenticated user lacks required administrative role (`Admin`, `Manager`, `HR`, `SuperAdmin`).

**Example Request**:
```http
GET /api/v1/jobs/5/employees?searchTerm=sarah&pageNumber=1&pageSize=10 HTTP/1.1
Host: localhost:5000
Authorization: Bearer <jwt-token>
```

**Example Response (`200 OK`)**:
```json
{
  "items": [
    {
      "id": 12,
      "employeeCode": "EMP-0012",
      "fullName": "Sarah Jenkins",
      "email": "sarah.jenkins@buy2.com",
      "departmentName": "Engineering",
      "siteName": "Headquarters - Cairo",
      "joinDate": "2024-03-15T00:00:00Z",
      "profilePhotoUrl": "https://storage.buy2.com/photos/emp-0012.jpg"
    }
  ],
  "totalCount": 1,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

**Example Response (`404 Not Found`)**:
```json
{
  "message": "Job role with ID 999 was not found."
}
```

---

## 9. Site Management (`/api/v1/sites`) [SCRUM-229, SCRUM-230, SCRUM-232, SCRUM-233]

### `GET /api/v1/sites`
- **Authorization**: `[Authorize]`
- **Controller**: `GetSitesController` / `SitesController`
- **Description**: Returns list of all configured work sites.
- **Response**: `200 OK` with `List<SiteDto>`.

### `POST /api/v1/sites`
- **Authorization**: `[Authorize(Roles = "Admin")]`
- **Controller**: `GetSitesController` / `SitesController`
- **Request Body** (`CreateUpdateSiteDto`):
  - `siteName` (string, required) - Unique site name
  - `regionId` (int, required) - Existing region ID
  - `latitude` (double) & `longitude` (double) - GPS coordinates
  - `macWhitelist` (array of strings) - Whitelisted MAC addresses
  - `macAddress` (string), `address` (string), `mapUrl` (string), `phoneNumber` (string), `instructions` (string)
  - `maxCapacity` (int) - Max employee capacity
  - `preferredEmployeeIds` (array of int) - Attached preferred staff
  - `operationalHours` (array of `SiteOperationalHourDto`: `day`, `isOpen`, `from`, `to`)
- **Description**: Creates a new site with GPS coordinates, instructions, operational hours (Sun-Sat), and preferred employees in an atomic transaction.
- **Responses**:
  - `201 Created` with new `siteId` and `Location` header.
  - `400 Bad Request` if site name is duplicate, region does not exist, or duplicate operational days are provided.

### `PUT /api/v1/sites/{id}`
- **Authorization**: `[Authorize(Roles = "Admin,Manager")]`
- **Controller**: `GetSitesController` / `SitesController`
- **Path Parameters**:
  - `id` (int, required) - Site ID to update
- **Request Body** (`CreateUpdateSiteDto`): Updated site fields, schedule, and preferred employees.
- **Description**: Updates site basic info, operational hours schedule, and preferred employee list.
- **Responses**:
  - `200 OK` with updated `siteId`.
  - `400 Bad Request` if site name conflicts with another site, region does not exist, or duplicate days exist.
  - `404 Not Found` if site does not exist.

### `GET /api/v1/sites/{id}/deletion-check`
- **Authorization**: `[Authorize(Roles = "Admin")]`
- **Controller**: `GetSitesController` / `SitesController`
- **Path Parameters**:
  - `id` (int, required) - Site ID to check
- **Description**: Performs a deletion impact pre-check to verify if the site has allocated employees or future scheduled shifts. Populates deletion confirmation / reallocation warning modal.
- **Responses**:
  - `200 OK` with `DeletionCheckDto`:
    - `canDelete` (bool) - True if 0 allocated employees and 0 future shifts.
    - `allocatedEmployeesCount` (int) - Total count of assigned employees.
    - `allocatedEmployees` (array of `AllocatedEmployeeDto`: `employeeId`, `fullName`).
  - `404 Not Found` if site does not exist.

### `DELETE /api/v1/sites/{id}`
- **Authorization**: `[Authorize(Roles = "Admin")]`
- **Controller**: `GetSitesController` / `SitesController`
- **Path Parameters**:
  - `id` (int, required) - Site ID to delete
- **Request Body** (`ReallocateAndDeleteSiteDto`, optional if site has 0 employees):
  - `employeeSiteReassignments` (array of `EmployeeSiteReassignmentDto`: `employeeId`, `newSiteId`)
- **Description**: Deletes a company work site. If employees are currently assigned, requires a replacement target site for each allocated employee and reassigns them atomically before deleting the site entity. Blocks deletion if future scheduled shifts exist. Cascades removal of site operational hours, documents, and preferred staff links.
- **Responses**:
  - `204 No Content` on successful deletion and reallocation.
  - `400 Bad Request` if future scheduled shifts exist, if any assigned employee is missing a replacement site, if multiple replacement sites are specified for the same employee, if target sites do not exist, or if an employee is reallocated to the site being deleted.
  - `404 Not Found` if site does not exist.

### `GET /api/v1/sites/regions`
- **Authorization**: `[Authorize]`
- **Description**: Returns all active regions ordered alphabetically by name for dropdown selection.
- **Response**: `200 OK` with `List<RegionListItemDto>`.

### `POST /api/v1/sites/regions`
- **Authorization**: `[Authorize(Roles = "Admin,Manager")]`
- **Request Body** (`CreateRegionDto`: `name`): Region name to create.
- **Description**: Creates a new region inline with case-insensitive name uniqueness check.
- **Responses**:
  - `200 OK` with newly generated `regionId`.
  - `400 Bad Request` if region name already exists.



