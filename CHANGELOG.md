# Changelog

All notable changes to the Buy2 HR Management System (HRMS) project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added
- **Employee Directory CSV Export (`[EP-2]`)**:
  - Implemented `ExportEmployeesQuery` MediatR request and `ExportEmployeesQueryHandler` in [`ExportEmployeesQuery.cs`](file:///F:/C%23%20projects/HR_system/src/Buy2.Application/Features/Employees/ExportEmployees/ExportEmployeesQuery.cs).
  - Added `GET /api/v1/employees/export` endpoint to [`EmployeeDirectoryController.cs`](file:///F:/C%23%20projects/HR_system/src/Buy2.Api/Controllers/EmployeeDirectoryController.cs).
  - Protected with role-based authorization `[Authorize(Roles = "Admin,Manager")]`.
  - Supports comprehensive server-side filtering by:
    - Search text (`FirstName`, `LastName`, `Email`, `EmployeeCode`)
    - Department (`JobRole.DepartmentId` / `JobRole.Title`)
    - Region (`Site.SiteName`)
    - Sorting by name, employee code, email, job title, and join date (ascending/descending).
  - Generates RFC-4180-compliant CSV with UTF-8 BOM preamble for full Microsoft Excel compatibility.
  - Returns `text/csv` attachment file titled `employees.csv`.
