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
- **Authorization**: Authenticated users (`[Authorize]`)
- **Path Parameters**:
  - `id` (int, required) - Unique ID of the employee
- **Description**: Retrieves full employee profile details including personal info, job role details, qualifications, and live calculated stats.
- **Computed Header Stats**:
  - `TotalPoints` - Sum of points from the points wallet ledger
  - `TotalTasks` - Total number of assigned tasks
  - `TotalGifts` - Total number of redeemed rewards/gifts
- **Response**:
  - `200 OK` with `EmployeeProfileDto`:
    - `Id`, `FirstName`, `LastName`, `EmployeeCode`, `Email`, `Phone`, `BirthDate`, `Gender`, `JobTitle`, `Department`, `SeniorityLevel`, `ExperienceYears`, `DirectManagerName`, `JobType`, `SiteName`, `RoleName`, `Qualifications` (array of strings), `TotalPoints`, `TotalTasks`, `TotalGifts`
  - `404 Not Found` if employee with specified `id` does not exist.

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



