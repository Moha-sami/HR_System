import { inject, Injectable, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
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
import type {
  EmployeePointsSummary,
  EmployeePointsTransactionFilters,
  PaginatedEmployeePointsTransactions,
} from '../models/view-employee/employee-points';

const API_BASE = environment.baseUrl;

@Injectable({ providedIn: 'root' })
export class EmployeeDetailService {
  private readonly http = inject(HttpClient);
  private detailRequestId = 0;
  private detailRequestedEmployeeId: number | null = null;
  private pointsSummaryRequestId = 0;
  private pointsTransactionsRequestId = 0;

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

  // Points & Rewards store
  readonly pointsSummary = signal<EmployeePointsSummary | null>(null);
  readonly pointsSummaryLoading = signal(false);
  readonly pointsSummaryError = signal<string | null>(null);
  readonly pointsTransactions = signal<PaginatedEmployeePointsTransactions | null>(null);
  readonly pointsTransactionsLoading = signal(false);
  readonly pointsTransactionsError = signal<string | null>(null);

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

  getEmployeePointsSummary(id: number): Observable<EmployeePointsSummary> {
    return this.http.get<EmployeePointsSummary>(`${API_BASE}/employees/${id}/points/summary`);
  }

  getEmployeePointsTransactions(
    id: number,
    filters: EmployeePointsTransactionFilters,
  ): Observable<PaginatedEmployeePointsTransactions> {
    let params = new HttpParams()
      .set('page', filters.page.toString())
      .set('pageSize', filters.pageSize.toString());

    if (filters.type) params = params.set('type', filters.type);
    if (filters.triggeredBy?.trim()) params = params.set('triggeredBy', filters.triggeredBy.trim());
    if (filters.dateFrom) params = params.set('dateFrom', filters.dateFrom);
    if (filters.dateTo) params = params.set('dateTo', filters.dateTo);

    return this.http.get<PaginatedEmployeePointsTransactions>(
      `${API_BASE}/employees/${id}/points/transactions`,
      { params },
    );
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
    if (
      (!this.detailLoading() && this.detailEmployee()?.id === id) ||
      (this.detailLoading() && this.detailRequestedEmployeeId === id)
    ) {
      return;
    }

    const requestId = ++this.detailRequestId;
    this.detailRequestedEmployeeId = id;
    this.detailLoading.set(true);
    this.detailError.set(null);

    this.getEmployeeProfile(id).subscribe({
      next: (data) => {
        if (requestId !== this.detailRequestId || this.detailRequestedEmployeeId !== id) return;
        this.detailEmployee.set(data);
        this.detailLoading.set(false);
      },
      error: () => {
        if (requestId !== this.detailRequestId || this.detailRequestedEmployeeId !== id) return;
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

  loadEmployeePointsSummary(id: number): void {
    const requestId = ++this.pointsSummaryRequestId;
    this.pointsSummary.set(null);
    this.pointsSummaryLoading.set(true);
    this.pointsSummaryError.set(null);

    this.getEmployeePointsSummary(id).subscribe({
      next: (data) => {
        if (this.detailEmployee()?.id !== id || requestId !== this.pointsSummaryRequestId) return;
        this.pointsSummary.set(data);
        this.pointsSummaryLoading.set(false);
      },
      error: () => {
        if (this.detailEmployee()?.id !== id || requestId !== this.pointsSummaryRequestId) return;
        this.pointsSummaryError.set('EMPLOYEE_DETAIL.POINTS_REWARDS.SUMMARY_ERROR');
        this.pointsSummaryLoading.set(false);
      },
    });
  }

  loadEmployeePointsTransactions(id: number, filters: EmployeePointsTransactionFilters): void {
    const requestId = ++this.pointsTransactionsRequestId;
    this.pointsTransactions.set(null);
    this.pointsTransactionsLoading.set(true);
    this.pointsTransactionsError.set(null);

    this.getEmployeePointsTransactions(id, filters).subscribe({
      next: (data) => {
        if (
          this.detailEmployee()?.id !== id ||
          requestId !== this.pointsTransactionsRequestId
        ) return;
        this.pointsTransactions.set(data);
        this.pointsTransactionsLoading.set(false);
      },
      error: () => {
        if (
          this.detailEmployee()?.id !== id ||
          requestId !== this.pointsTransactionsRequestId
        ) return;
        this.pointsTransactionsError.set('EMPLOYEE_DETAIL.POINTS_REWARDS.TRANSACTIONS_ERROR');
        this.pointsTransactionsLoading.set(false);
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
    this.detailRequestId++;
    this.detailRequestedEmployeeId = null;
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

  clearEmployeePoints(): void {
    this.pointsSummaryRequestId++;
    this.pointsTransactionsRequestId++;
    this.pointsSummary.set(null);
    this.pointsSummaryLoading.set(false);
    this.pointsSummaryError.set(null);
    this.pointsTransactions.set(null);
    this.pointsTransactionsLoading.set(false);
    this.pointsTransactionsError.set(null);
  }

  updateDetailEmployee(partial: Partial<EmployeeProfileDto>): void {
    this.detailEmployee.update((current) => (current ? { ...current, ...partial } : null));
  }
}
