import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import type { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

const EMPLOYEES_API = `${environment.jsonServerUrl}/employees`;
const JOB_ROLES_API = `${environment.jsonServerUrl}/jobRoles`;
const ROLES_API = `${environment.jsonServerUrl}/roles`;
const SITES_API = `${environment.jsonServerUrl}/sites`;

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

  getEmployees(): Observable<readonly EmployeeApiResponse[]> {
    return this.http.get<readonly EmployeeApiResponse[]>(EMPLOYEES_API);
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
    return this.http.post<EmployeeApiResponse>(EMPLOYEES_API, input);
  }

  updateEmployee(id: number, input: UpdateEmployeeRequest): Observable<EmployeeApiResponse> {
    return this.http.patch<EmployeeApiResponse>(`${EMPLOYEES_API}/${id}`, input);
  }

  deleteEmployee(id: number): Observable<void> {
    return this.http.delete<void>(`${EMPLOYEES_API}/${id}`);
  }
}
