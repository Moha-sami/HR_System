/**
 * Employee Payroll DTOs - matches GET /api/v1/employees/{id}/payroll response
 * Source: Buy2.Application.DTOs.Employees.PayrollDtos
 */

export interface EmployeePayrollProfileDto {
  readonly employeeId: number;
  readonly isConfigured: boolean;
  readonly salaryType: number; // 1 = Fixed, 2 = Hourly
  readonly payoutPeriod: string;
  readonly payoutDay: number;
  readonly workWeekStart: number; // DayOfWeek (0=Sunday...6=Saturday)
  readonly workWeekEnd: number; // DayOfWeek
  readonly paymentAmount: number;
  readonly overtimeThresholdHours: number;
  readonly overtimeHourlyRate: number;
  readonly attendanceType: string;
  readonly workSiteIds: readonly number[];
  readonly onlineWorkdays: readonly string[];
  readonly offlineWorkdays: readonly string[];
}

/** Request payload for PUT /api/v1/employees/{id}/payroll */
export interface UpdatePayrollProfileDto {
  readonly salaryType: number;
  readonly payoutPeriod: string;
  readonly payoutDay: number;
  readonly workWeekStart: number;
  readonly workWeekEnd: number;
  readonly paymentAmount: number;
  readonly overtimeThresholdHours: number;
  readonly overtimeHourlyRate: number;
  readonly attendanceType: string;
  readonly workSiteIds: readonly number[];
  readonly onlineWorkdays: readonly string[];
  readonly offlineWorkdays: readonly string[];
}
