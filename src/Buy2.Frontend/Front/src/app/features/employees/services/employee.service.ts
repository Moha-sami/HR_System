import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import type { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import type {
  EmployeeProfileDto,
  UpdatePersonalInfoRequestDto,
  UpdatePersonalInfoWrapperDto,
  UpdateJobDetailsRequestDto,
  UpdateJobDetailsWrapperDto,
} from '../models/employee-profile';
import type {
  EmployeePayrollProfileDto,
  UpdatePayrollProfileDto,
} from '../models/employee-payroll';

const JSON_SERVER_API = `${environment.jsonServerUrl}/employees`;
const JOB_ROLES_API = `${environment.jsonServerUrl}/jobRoles`;
const ROLES_API = `${environment.jsonServerUrl}/roles`;
const SITES_API = `${environment.jsonServerUrl}/sites`;
const API_BASE = environment.baseUrl;

/** Employee resource as persisted by the JSON Server API. */
export interface EmployeeApiResponse {
  readonly id: number;
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly phoneNumber: string;
  readonly jobRoleId: number;
  readonly roleId: number;
  readonly siteId: number;
  readonly createdAt: string;
}

/** Data required to create an employee in the JSON Server employees collection. */
export interface CreateEmployeeRequest {
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly phoneNumber: string;
  readonly jobRoleId: number;
  readonly roleId: number;
  readonly siteId: number;
  readonly createdAt: string;
}

export type UpdateEmployeeRequest = CreateEmployeeRequest;

/** Job-role option returned by the JSON Server jobRoles resource. */
export interface JobRoleApiResponse {
  readonly id: number;
  readonly title: string;
  readonly departmentId: number;
  readonly requiredQualificationsJson: string;
  readonly createdAt: string;
}

/** System-role option returned by the JSON Server roles resource. */
export interface RoleApiResponse {
  readonly id: number;
  readonly name: string;
  readonly permissionsJson: string;
  readonly createdAt: string;
}

/** Site option returned by the JSON Server sites resource. */
export interface SiteApiResponse {
  readonly id: number;
  readonly siteName: string;
  readonly latitude: number;
  readonly longitude: number;
  readonly macAddressWhitelistJson: string;
  readonly createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private readonly http = inject(HttpClient);

  // Detail view store
  readonly detailEmployee = signal<EmployeeProfileDto | null>(null);
  readonly detailLoading = signal(false);
  readonly detailError = signal<string | null>(null);

  // JSON Server methods (existing)
  getEmployees(): Observable<readonly EmployeeApiResponse[]> {
    return this.http.get<readonly EmployeeApiResponse[]>(JSON_SERVER_API);
  }

  getJobRoles(): Observable<readonly JobRoleApiResponse[]> {
    return this.http.get<readonly JobRoleApiResponse[]>(JOB_ROLES_API);
  }

  getRoles(): Observable<readonly RoleApiResponse[]> {
    return this.http.get<readonly RoleApiResponse[]>(ROLES_API);
  }

  getSites(): Observable<readonly SiteApiResponse[]> {
    return this.http.get<readonly SiteApiResponse[]>(SITES_API);
  }

  createEmployee(input: CreateEmployeeRequest): Observable<EmployeeApiResponse> {
    return this.http.post<EmployeeApiResponse>(JSON_SERVER_API, input);
  }

  updateEmployee(id: number, input: UpdateEmployeeRequest): Observable<EmployeeApiResponse> {
    return this.http.patch<EmployeeApiResponse>(`${JSON_SERVER_API}/${id}`, input);
  }

  deleteEmployee(id: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE}/employees/${id}`);
  }

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

  // Store signals (readonly for consumers)
  readonly detailEmployeeSignal = this.detailEmployee.asReadonly();
  readonly detailLoadingSignal = this.detailLoading.asReadonly();
  readonly detailErrorSignal = this.detailError.asReadonly();
}
