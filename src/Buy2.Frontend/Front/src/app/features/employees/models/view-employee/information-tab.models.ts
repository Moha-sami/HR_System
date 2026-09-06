/**
 * Information Tab models — single source of truth for the tab.
 * Mirrors the current backend contracts (source of truth):
 * - GET /api/v1/employees/{id} -> Buy2.Application.DTOs.Employees.EmployeeProfileDtos
 * - PUT /api/v1/employees/{id}/personal -> UpdateEmployeePersonalInfoDto (bare, all optional)
 * - PUT /api/v1/employees/{id}/job -> UpdateJobDetailsDto (bare, all optional)
 * - GET /api/v1/employees/{id}/payroll -> EmployeePayrollProfileDto
 * - PUT /api/v1/employees/{id}/payroll -> UpdatePayrollProfileDto (bare, all optional)
 */

export type GenderCode = 1 | 2; // 1 = Male, 2 = Female
export type SalaryTypeCode = 1 | 2; // 1 = Fixed, 2 = Hourly
/** DayOfWeek as the backend serializes it: 0 = Sunday … 6 = Saturday. */
export type DayOfWeekCode = 0 | 1 | 2 | 3 | 4 | 5 | 6;

export interface EmployeeStatsDto {
  readonly totalPoints: number;
  readonly totalTasks: number;
  readonly totalGifts: number;
}

export interface EmployeePersonalInfoDto {
  readonly name: string;
  readonly birthdate: string | null;
  readonly email: string;
  readonly phoneNumber: string;
  readonly gender: number | null;
}

export interface EmployeeJobDetailsDto {
  readonly title: string;
  readonly department: string;
  readonly seniorityLevel: string;
  readonly experienceYears: number;
  readonly directManagerName: string | null;
  readonly jobType: string;
  readonly qualifications: readonly string[];
  readonly attendanceType: string;
  readonly onlineWorkdays: readonly string[];
  readonly offlineWorkdays: readonly string[];
  readonly jobRoleId: number | null;
  readonly directManagerId: number | null;
}

export interface EmployeePayrollSummaryDto {
  readonly salaryType: string;
  readonly paymentAmount: number;
  readonly payoutPeriod: string;
  readonly payoutDay: number | null;
  readonly workWeekStartDay: string | null;
  readonly workWeekEndDay: string | null;
  readonly overtimeEnabled: boolean;
  readonly overtimeThresholdHours: number | null;
  readonly overtimeRateMultiplier: number | null;
  readonly assignedWorkSiteIds: readonly number[];
}

export interface EmployeeProfileDto {
  readonly id: number;
  readonly employeeCode: string;
  readonly fullName: string;
  readonly phone: string;
  readonly email: string;
  readonly location: string;
  readonly profilePhotoUrl: string | null;
  readonly stats: EmployeeStatsDto;
  readonly personalInfo: EmployeePersonalInfoDto;
  readonly jobDetails: EmployeeJobDetailsDto;
  readonly payroll: EmployeePayrollSummaryDto | null;
}

/** Bare payload for PUT /api/v1/employees/{id}/personal (partial update). */
export interface UpdatePersonalInfoRequest {
  readonly firstName?: string;
  readonly lastName?: string;
  readonly phoneNumber?: string;
  /** ISO date-time string; backend accepts DateTimeOffset. */
  readonly birthdate?: string | null;
  readonly gender?: GenderCode | null;
  readonly email?: string;
}

/** Bare payload for PUT /api/v1/employees/{id}/job (partial update). */
export interface UpdateJobDetailsRequest {
  readonly jobRoleId?: number | null;
  readonly directManagerId?: number | null;
  readonly seniorityLevel?: string;
  readonly experienceYears?: number;
  readonly jobType?: string;
  readonly attendanceType?: string;
  readonly onlineWorkdays?: readonly string[];
  readonly offlineWorkdays?: readonly string[];
  /** Written to the shared job role's required qualifications. */
  readonly qualifications?: readonly string[];
}

/** Full payroll view from GET /api/v1/employees/{id}/payroll. */
export interface EmployeePayrollProfileDto {
  readonly employeeId: number;
  readonly isConfigured: boolean;
  readonly salaryType: number;
  readonly payoutPeriod: string;
  readonly payoutDay: number;
  readonly workWeekStartDay: number;
  readonly workWeekEndDay: number;
  readonly paymentAmount: number;
  readonly overtimeThresholdHours: number;
  readonly overtimeHourlyRate: number;
  readonly attendanceType: string;
  readonly workSiteIds: readonly number[];
  readonly onlineWorkdays: readonly string[];
  readonly offlineWorkdays: readonly string[];
}

/** Bare payload for PUT /api/v1/employees/{id}/payroll (partial update). */
export interface UpdatePayrollProfileRequest {
  readonly salaryType?: SalaryTypeCode;
  readonly payoutPeriod?: string;
  readonly payoutDay?: number;
  readonly workWeekStartDay?: DayOfWeekCode;
  readonly workWeekEndDay?: DayOfWeekCode;
  readonly paymentAmount?: number;
  readonly overtimeThresholdHours?: number;
  /** Mapped to the backend's overtime hourly rate. */
  readonly overtimeRateMultiplier?: number;
  readonly attendanceType?: string;
  readonly assignedWorkSiteIds?: readonly number[];
  readonly onlineWorkdays?: readonly string[];
  readonly offlineWorkdays?: readonly string[];
}

/** Minimal row shapes for the tab lookups (full entities live elsewhere). */
export interface JobLookupRow {
  readonly id: number;
  readonly title: string;
}

export interface JobLookupResponse {
  readonly items: readonly JobLookupRow[];
  readonly totalCount: number;
}

export interface ManagerLookupRow {
  readonly id: number;
  readonly employeeCode: string;
  readonly employeeName: string;
  readonly jobTitle: string;
  readonly email: string;
}

export interface ManagerLookupResponse {
  readonly items: readonly ManagerLookupRow[];
  readonly totalCount: number;
}

export interface SiteLookupRow {
  readonly id: number;
  readonly siteName: string;
}
