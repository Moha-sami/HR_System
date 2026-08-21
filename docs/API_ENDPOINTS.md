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



