import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import type { Observable } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';
import type {
  EmployeeProfileDto,
  UpdatePersonalInfoRequestDto,
  UpdatePersonalInfoWrapperDto,
  UpdateJobDetailsRequestDto,
  UpdateJobDetailsWrapperDto,
} from '../models/view-employee/employee-profile';
import type {
  EmployeePayrollProfileDto,
  UpdatePayrollProfileDto,
} from '../models/view-employee/employee-payroll';
import type { PaginatedEmployeeListDto, EmployeeFilterDto } from '../models/employee-list/employee-list';
import { buildEmployeeQueryParams } from '../models/employee-list/employee-list';
import { Job } from '@app/core/models/job';
import { Role } from '@app/core/models/role.models';
import { Site } from '@app/core/models/site.models';
import { InsertEmployee } from '../models/insert-employee/insert-employee.models';

const API_BASE = environment.baseUrl;

@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private readonly http = inject(HttpClient);

  // Detail view store
  readonly detailEmployee = signal<EmployeeProfileDto | null>(null);
  readonly detailLoading = signal(false);
  readonly detailError = signal<string | null>(null);


  // Real API methods
  getEmployeeProfile(id: number): Observable<EmployeeProfileDto> {
    return this.http.get<EmployeeProfileDto>(`${API_BASE}/employees/${id}`);
  }

  getEmployeePayroll(id: number): Observable<EmployeePayrollProfileDto> {
    return this.http.get<EmployeePayrollProfileDto>(`${API_BASE}/employees/${id}/payroll`);
  }

  updatePersonalInfo(id: number, dto: UpdatePersonalInfoRequestDto): Observable<void> {
    const wrapper: UpdatePersonalInfoWrapperDto = { dto };
    return this.http.put<void>(`${API_BASE}/employees/${id}/personal`, wrapper);
  }

  updateJobDetails(id: number, dto: UpdateJobDetailsRequestDto): Observable<void> {
    const wrapper: UpdateJobDetailsWrapperDto = { dto };
    return this.http.put<void>(`${API_BASE}/employees/${id}/job`, wrapper);
  }

  updatePayrollProfile(id: number, dto: UpdatePayrollProfileDto): Observable<void> {
    return this.http.put<void>(`${API_BASE}/employees/${id}/payroll`, dto);
  }

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

  updateEmployee(id: number, input: InsertEmployee): Observable<EmployeeProfileDto> {
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
    return this.updatePersonalInfo(id, personalDto).pipe(
      switchMap(() => this.getEmployeeProfile(id))
    );
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

  // Detail view store methods
  loadDetailEmployee(id: number): void {
    if (this.detailLoading() || this.detailEmployee()?.id === id) {
      return;
    }

    this.detailLoading.set(true);
    this.detailError.set(null);

    this.getEmployeeProfile(id).subscribe({
      next: (data) => {
        this.detailEmployee.set(data);
        this.detailLoading.set(false);
      },
      error: () => {
        this.detailError.set('EMPLOYEE_MANAGEMENT.LIST_LOAD_FAILED');
        this.detailLoading.set(false);
      },
    });
  }

  clearDetailEmployee(): void {
    this.detailEmployee.set(null);
    this.detailLoading.set(false);
    this.detailError.set(null);
  }
  
  updateDetailEmployee(partial: Partial<EmployeeProfileDto>): void {
    this.detailEmployee.update((current) => (current ? { ...current, ...partial } : null));
  }
  
}