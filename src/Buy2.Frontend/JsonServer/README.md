# JSON Server - Mock API

A lightweight fake REST API for development and testing using [json-server](https://github.com/typicode/json-server).

## Quick Start

```bash
# Install dependencies
npm install

# Start the server (with file watching enabled)
npm start
```

The server runs at `http://localhost:3000` by default.

## Data Structure

Edit `data.json` to define your resources. Each top-level key becomes an API endpoint.

## Available Endpoints

| Endpoint | Items | Description |
| --- | --- | --- |
| `/books` | 4 | Test data (original) |
| `/departments` | 5 | Organizational departments |
| `/sites` | 3 | Physical branch locations with coordinates |
| `/roles` | 4 | System roles with permission toggles |
| `/jobRoles` | 6 | Job title definitions with required qualifications |
| `/employees` | 8 | Employee records with FK references |
| `/attendanceProfiles` | 4 | Shift timing profiles |
| `/shifts` | 6 | Scheduled shift assignments (published + draft) |
| `/shiftClaims` | 2 | Employee shift claim requests |
| `/pointsRules` | 6 | Gamification automation rules |
| `/pointsTransactions` | 8 | Points wallet transaction history |
| `/disciplinaryViolations` | 4 | Manual violation logs |
| `/rewardItems` | 6 | Reward catalog items |
| `/rewardRedemptions` | 3 | Voucher redemption records |

## REST Operations

Each endpoint supports standard CRUD:

| Method | Endpoint | Description |
| --- | --- | --- |
| GET | `/{resource}` | Get all items |
| GET | `/{resource}/1` | Get item by id |
| POST | `/{resource}` | Create a new item |
| PUT | `/{resource}/1` | Update item by id |
| PATCH | `/{resource}/1` | Partially update item by id |
| DELETE | `/{resource}/1` | Delete item by id |

## Query Parameters

| Parameter | Example | Description |
| --- | --- | --- |
| `_page` | `/employees?_page=1` | Pagination page number |
| `_limit` | `/employees?_limit=10` | Items per page |
| `_sort` | `/employees?_sort=firstName` | Sort by field |
| `_order` | `/employees?_sort=firstName&_order=desc` | Sort order (asc/desc) |
| `q` | `/employees?q=ahmed` | Full-text search |
| `fieldName` | `/employees?roleId=4` | Filter by field value |

## Data Schema

### employees

```json
{
  "id": 1,
  "firstName": "Ahmed",
  "lastName": "Ali",
  "email": "a.ali@buy2.com",
  "phoneNumber": "+966598432423",
  "jobRoleId": 2,
  "roleId": 1,
  "siteId": 1,
  "createdAt": "2026-01-15T08:00:00Z"
}
```

### jobRoles

```json
{
  "id": 1,
  "title": "UI/UX Designer",
  "departmentId": 2,
  "requiredQualificationsJson": "[\"figma\",\"management\"]",
  "createdAt": "2026-01-01T08:00:00Z"
}
```

### roles

```json
{
  "id": 1,
  "name": "SuperAdmin",
  "permissionsJson": "{\"employeeManagement\":[\"Add\",\"Edit\",\"View\",\"Delete\",\"Suspend\"],...}",
  "createdAt": "2026-01-01T08:00:00Z"
}
```

### sites

```json
{
  "id": 1,
  "siteName": "Downtown Campus",
  "latitude": 30.0444,
  "longitude": 31.2357,
  "macAddressWhitelistJson": "[\"00:1B:44:11:3A:B7\"]",
  "createdAt": "2026-01-01T08:00:00Z"
}
```

### shifts

```json
{
  "id": 1,
  "employeeId": 4,
  "siteId": 2,
  "jobRoleId": 3,
  "startTime": "2026-06-14T08:00:00Z",
  "endTime": "2026-06-14T16:00:00Z",
  "isPublished": true,
  "createdAt": "2026-06-01T08:00:00Z"
}
```

### pointsRules

```json
{
  "id": 1,
  "ruleKey": "LATENESS_DEDUCTION",
  "eventType": "ClockIn",
  "conditionExpression": "ActualClockIn > ExpectedClockIn + 15m",
  "actionType": "Debit",
  "pointValue": 500,
  "createdAt": "2026-01-01T08:00:00Z"
}
```

### pointsTransactions

```json
{
  "id": 1,
  "employeeId": 4,
  "pointsRuleId": 1,
  "amount": -500,
  "transactionType": "Debit",
  "createdAt": "2026-06-01T08:20:00Z"
}
```

### rewardItems

```json
{
  "id": 1,
  "rewardName": "Talabat Voucher",
  "costInPoints": 1000,
  "availableStock": 50,
  "createdAt": "2026-01-01T08:00:00Z"
}
```

## Example Requests (Angular HttpClient)

```typescript
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

const API_URL = 'http://localhost:3000';

// Get all employees
getEmployees(): Observable<Employee[]> {
  return this.http.get<Employee[]>(`${API_URL}/employees`);
}

// Filter employees by site
getEmployeesBySite(siteId: number): Observable<Employee[]> {
  return this.http.get<Employee[]>(`${API_URL}/employees?siteId=${siteId}`);
}

// Get employees with pagination
getEmployeesPaginated(page: number, limit: number): Observable<Employee[]> {
  const params = new HttpParams()
    .set('_page', page.toString())
    .set('_limit', limit.toString());
  return this.http.get<Employee[]>(`${API_URL}/employees`, { params });
}

// Get open (unassigned) shifts for a site
getOpenShifts(siteId: number): Observable<Shift[]> {
  return this.http.get<Shift[]>(`${API_URL}/shifts?siteId=${siteId}&isPublished=false`);
}

// Get points transactions for an employee
getPointsHistory(employeeId: number): Observable<PointsTransaction[]> {
  return this.http.get<PointsTransaction[]>(`${API_URL}/pointsTransactions?employeeId=${employeeId}`);
}

// Get available rewards
getAvailableRewards(): Observable<RewardItem[]> {
  return this.http.get<RewardItem[]>(`${API_URL}/rewardItems?availableStock_gte=1`);
}
```

## Notes

- The `-w` flag enables file watching — changes to `data.json` are reflected immediately without restarting.
- IDs are auto-generated for POST requests if not provided.
- JSON string fields (`*Json`) contain serialized arrays/objects — parse them in your components.
- The server is for **development only** — not suitable for production use.
