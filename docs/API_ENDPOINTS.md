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
  - `department` (string, optional) - Department ID or name filter
  - `region` (string, optional) - Site region name filter
  - `search` (string, optional) - Search text matching first/last name, email, or employee code
  - `sort` (string, optional) - Column to sort on (`name`, `employeecode`, `email`, `jobtitle`, `joindate`)
  - `sortDir` (string, optional) - Sort direction (`asc`, `desc`, default: `desc`)
- **Response**: `200 OK` with paginated list of employee records (`PaginatedList<EmployeeListItemDto>`).

### `GET /api/v1/employees/export`
- **Authorization**: `[Authorize(Roles = "Admin,Manager")]`
- **Query Parameters**:
  - `department` (string, optional) - Filter by department ID or title
  - `region` (string, optional) - Filter by site / region name
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
- **Description**: Retrieves full employee profile details for the Figma Information Tab, including personal information, job details, qualifications, attendance/workdays breakdown, live gamification/task stats, and optional payroll summary.
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
      - `department` (string) - Department name or identifier
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
- **Description**: Returns employee performance analytics and summary metrics over the specified time range. Computes weighted performance scores, descriptive rating labels (`Needs Improvement`, `Satisfactory`, `Good Performance`, `Excellent`), task tracking & deadline compliance percentage, awarded achievement badges, chronological daily score trend points, and detailed individual submission records. Soft-deleted or non-existent employees return `404 Not Found`.
- **Responses**:
  - `200 OK` with `PerformanceOverviewDto`:
    - `employeeId` (int)
    - `dateRangeResolved` (`DateRangeResolvedDto`: `from`, `to`, `period`)
    - `overallWeightedScore` (decimal)
    - `ratingLabel` (string: `Needs Improvement`, `Satisfactory`, `Good Performance`, `Excellent`)
    - `tasksSummary` (`TasksSummaryDto`: `totalTasks`, `todoCount`, `inProgressCount`, `completedCount`, `overdueCount`, `deadlineCompliancePercentage`)
    - `achievements` (array of `AchievementBadgeDto`: `id`, `title`, `description`, `earnedAt`, `badgeIcon`)
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
- **Description**: Returns a full monthly attendance calendar breakdown for the specified employee and month/year, including overall summary metrics (`AttendanceRate`, `PunctualityScore`, `AverageLatenessMinutes`, `RecordedHours`, `TargetHours`) and daily cell details (`Date`, `Status`, `LeaveType`, `HoursWorked`, `HoursLeft`, `OtHours`, `BreakTime`, `LatenessMinutes`). Non-existent or soft-deleted employees return `404 Not Found`.
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




