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

  updateDetailEmployee(partial: Partial<EmployeeProfileDto>): void {
    this.detailEmployee.update((current) => (current ? { ...current, ...partial } : null));
  }
}
