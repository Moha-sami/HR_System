import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import type { Observable } from 'rxjs';
import { forkJoin, map, of, switchMap } from 'rxjs';
import { environment } from '../../../../environments/environment';
import type { PaginatedEmployeeListDto, EmployeeFilterDto } from '../models/employee-list/employee-list';
import { buildEmployeeQueryParams } from '../models/employee-list/employee-list';
import { Job } from '@app/core/models/job';
import { Role, RolePaginatedResponse } from '@app/core/models/role.models';
import { Site } from '@app/core/models/site.models';
import { InsertEmployee } from '../models/insert-employee/insert-employee.models';
import type {
  BulkOnboardRequest,
  BulkOnboardResult,
} from '../models/bulk-onboard/bulk-onboard.models';

const API_BASE = environment.baseUrl;

@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private readonly http = inject(HttpClient);

  // Dropdown options for create/edit forms
  getJobRoles(): Observable<readonly Job[]> {
    return this.http.get<readonly Job[]>(`${API_BASE}/job-roles`);
  }

  getRoles(): Observable<readonly Role[]> {
    return this.http.get<readonly Role[]>(`${API_BASE}/roles`);
  }

  getSites(): Observable<readonly Site[]> {
    return this.http.get<readonly Site[]>(`${API_BASE}/sites`);
  }

  /** SCRUM-276: load every active role from the real paginated roles endpoint. */
  getBulkOnboardingRoles(): Observable<readonly Role[]> {
    return this.getAllLookupPages<Role, RolePaginatedResponse>(`${API_BASE}/roles`);
  }

  /** SCRUM-276: adapt the requested roles endpoint to Job Title dropdown options. */
  getBulkOnboardingJobs(): Observable<readonly Job[]> {
    return this.getAllLookupPages<Role, RolePaginatedResponse>(`${API_BASE}/roles`).pipe(
      map((roles) =>
        roles.map((role) => ({
          id: role.id,
          title: role.name,
          departmentId: null,
          isActive: role.isActive,
          createdAt: role.createdAt,
        })),
      ),
    );
  }

  bulkOnboardEmployees(request: BulkOnboardRequest): Observable<BulkOnboardResult> {
    return this.http.post<BulkOnboardResult>(`${API_BASE}/employees/bulk-onboard`, request);
  }

  // Create/Update employee using real API endpoints
  createEmployee(input: InsertEmployee): Observable<{ id: number }> {
    const command = {
      firstName: input.firstName,
      lastName: input.lastName,
      email: input.email,
      phoneNumber: input.phoneNumber,
      jobRoleId: input.jobRoleId,
      roleId: input.roleId,
      siteId: input.siteId,
      // Defaults for optional fields
      gender: 'Male' as const,
      jobType: 'FullTime',
      attendanceType: 'OnSite',
      defaultPassword: 'Welcome@123',
    };
    return this.http.post<{ id: number }>(`${API_BASE}/employees/onboard`, command);
  }

  updateEmployee(id: number, input: InsertEmployee): Observable<{ id: number }> {
    // Backend has separate endpoints for personal, job, and payroll updates
    // Update personal info first, then fetch updated employee
    const personalDto = {
      firstName: input.firstName,
      lastName: input.lastName,
      phoneNumber: input.phoneNumber,
      dateOfBirth: input.createdAt, // Using createdAt as fallback for dateOfBirth
      address: '',
      emergencyContact: '',
      nationalId: '',
    };
    // Note: The actual personal/job updates should be done via EmployeeDetailService
    // This method is kept for compatibility with employee-create flow
    return this.http.put<{ id: number }>(`${API_BASE}/employees/${id}`, personalDto);
  }

  // Paginated list API
  getEmployeesPaginated(filter: EmployeeFilterDto): Observable<PaginatedEmployeeListDto> {
    const params = buildEmployeeQueryParams(filter);
    return this.http.get<PaginatedEmployeeListDto>(`${API_BASE}/employees?${params}`);
  }

  exportEmployees(filter: EmployeeFilterDto): Observable<Blob> {
    const params = buildEmployeeQueryParams(filter);
    return this.http.get(`${API_BASE}/employees/export?${params}`, { responseType: 'blob' });
  }

  deleteEmployee(id: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE}/employees/${id}`);
  }

  private getAllLookupPages<T, TResponse extends {
    readonly items: readonly T[];
    readonly totalPages: number;
  }>(url: string): Observable<readonly T[]> {
    const params = new HttpParams()
      .set('isActive', 'true')
      .set('pageNumber', '1')
      .set('pageSize', '100');

    return this.http.get<TResponse>(url, { params }).pipe(
      switchMap((firstPage) => {
        if (firstPage.totalPages <= 1) return of(firstPage.items);

        const remainingPages = Array.from(
          { length: firstPage.totalPages - 1 },
          (_, index) => index + 2,
        ).map((pageNumber) =>
          this.http.get<TResponse>(url, {
            params: params.set('pageNumber', pageNumber.toString()),
          }),
        );

        return forkJoin(remainingPages).pipe(
          map((pages) => [firstPage.items, ...pages.map((page) => page.items)].flat()),
        );
      }),
    );
  }
}
