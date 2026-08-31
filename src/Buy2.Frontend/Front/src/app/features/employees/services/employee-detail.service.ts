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
} from '../models/view-employee/employee-profile';
import type {
  EmployeePayrollProfileDto,
  UpdatePayrollProfileDto,
} from '../models/view-employee/employee-payroll';
import type { AttendanceCalendarDto } from '../models/view-employee/employee-attendance';
import type {
  ViolationDto,
  ViolationFilters,
  ViolationDetailDto,
  ResolveViolationDto,
} from '../models/view-employee/employee-violations';

const API_BASE = environment.baseUrl;

@Injectable({ providedIn: 'root' })
export class EmployeeDetailService {
  private readonly http = inject(HttpClient);

  // Detail view store
  readonly detailEmployee = signal<EmployeeProfileDto | null>(null);
  readonly detailLoading = signal(false);
  readonly detailError = signal<string | null>(null);

  // Attendance calendar store
  readonly attendanceCalendar = signal<AttendanceCalendarDto | null>(null);
  readonly attendanceLoading = signal(false);
  readonly attendanceError = signal<string | null>(null);

  // Violations store
  readonly violations = signal<readonly ViolationDto[]>([]);
  readonly violationsLoading = signal(false);
  readonly violationsError = signal<string | null>(null);

  // Violation detail store
  readonly violationDetail = signal<ViolationDetailDto | null>(null);
  readonly violationDetailLoading = signal(false);
  readonly violationDetailError = signal<string | null>(null);

  // Profile API
  getEmployeeProfile(id: number): Observable<EmployeeProfileDto> {
    return this.http.get<EmployeeProfileDto>(`${API_BASE}/employees/${id}`);
  }

  getEmployeePayroll(id: number): Observable<EmployeePayrollProfileDto> {
    return this.http.get<EmployeePayrollProfileDto>(`${API_BASE}/employees/${id}/payroll`);
  }

  // Attendance Calendar API
  getAttendanceCalendar(
    id: number,
    month: number,
    year: number,
  ): Observable<AttendanceCalendarDto> {
    return this.http.get<AttendanceCalendarDto>(`${API_BASE}/employees/${id}/attendance/calendar`, {
      params: { month, year },
    });
  }

  // Violations API
  getViolations(id: number, filters: ViolationFilters = {}): Observable<readonly ViolationDto[]> {
    const params: Record<string, string> = {};
    if (filters.type) params['type'] = filters.type;
    if (filters.severityLevel) params['severityLevel'] = filters.severityLevel;
    if (filters.dateFrom) params['dateFrom'] = filters.dateFrom;
    if (filters.dateTo) params['dateTo'] = filters.dateTo;

    return this.http.get<readonly ViolationDto[]>(`${API_BASE}/employees/${id}/violations`, {
      params,
    });
  }

  getViolationDetail(employeeId: number, violationId: number): Observable<ViolationDetailDto> {
    return this.http.get<ViolationDetailDto>(
      `${API_BASE}/employees/${employeeId}/violations/${violationId}`,
    );
  }

  resolveViolation(
    employeeId: number,
    violationId: number,
    dto: ResolveViolationDto,
  ): Observable<void> {
    return this.http.patch<void>(
      `${API_BASE}/employees/${employeeId}/violations/${violationId}/resolve`,
      dto,
    );
  }

  exportViolations(id: number, filters: ViolationFilters = {}): Observable<Blob> {
    const params: Record<string, string> = {};
    if (filters.type) params['type'] = filters.type;
    if (filters.severityLevel) params['severityLevel'] = filters.severityLevel;
    if (filters.dateFrom) params['dateFrom'] = filters.dateFrom;
    if (filters.dateTo) params['dateTo'] = filters.dateTo;

    return this.http.get(`${API_BASE}/employees/${id}/violations/export`, {
      params,
      responseType: 'blob',
    });
  }

  // Update API methods
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
        this.detailError.set('EMPLOYEE_DETAIL.LOAD_FAILED');
        this.detailLoading.set(false);
      },
    });
  }

  loadAttendanceCalendar(id: number, month: number, year: number): void {
    this.attendanceLoading.set(true);
    this.attendanceError.set(null);

    this.getAttendanceCalendar(id, month, year).subscribe({
      next: (data) => {
        this.attendanceCalendar.set(data);
        this.attendanceLoading.set(false);
      },
      error: () => {
        this.attendanceError.set('EMPLOYEE_DETAIL.ATTENDANCE_LOAD_FAILED');
        this.attendanceLoading.set(false);
      },
    });
  }

  loadViolations(id: number, filters: ViolationFilters = {}): void {
    this.violationsLoading.set(true);
    this.violationsError.set(null);

    this.getViolations(id, filters).subscribe({
      next: (data) => {
        this.violations.set(data);
        this.violationsLoading.set(false);
      },
      error: () => {
        this.violationsError.set('EMPLOYEE_DETAIL.VIOLATIONS_LOAD_FAILED');
        this.violationsLoading.set(false);
      },
    });
  }

  loadViolationDetail(employeeId: number, violationId: number): void {
    this.violationDetailLoading.set(true);
    this.violationDetailError.set(null);

    this.getViolationDetail(employeeId, violationId).subscribe({
      next: (data) => {
        this.violationDetail.set(data);
        this.violationDetailLoading.set(false);
      },
      error: () => {
        this.violationDetailError.set('EMPLOYEE_DETAIL.VIOLATIONS.DETAIL.LOAD_FAILED');
        this.violationDetailLoading.set(false);
      },
    });
  }

  resolveViolationAction(
    employeeId: number,
    violationId: number,
    dto: ResolveViolationDto,
  ): Observable<void> {
    return this.resolveViolation(employeeId, violationId, dto);
  }

  clearDetailEmployee(): void {
    this.detailEmployee.set(null);
    this.detailLoading.set(false);
    this.detailError.set(null);
  }

  clearAttendanceCalendar(): void {
    this.attendanceCalendar.set(null);
    this.attendanceLoading.set(false);
    this.attendanceError.set(null);
  }

  clearViolations(): void {
    this.violations.set([]);
    this.violationsLoading.set(false);
    this.violationsError.set(null);
  }

  clearViolationDetail(): void {
    this.violationDetail.set(null);
    this.violationDetailLoading.set(false);
    this.violationDetailError.set(null);
  }

  updateDetailEmployee(partial: Partial<EmployeeProfileDto>): void {
    this.detailEmployee.update((current) => (current ? { ...current, ...partial } : null));
  }
}
