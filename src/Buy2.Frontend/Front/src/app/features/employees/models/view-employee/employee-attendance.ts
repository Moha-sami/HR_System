/**
 * Attendance Calendar DTOs - matches GET /api/v1/employees/{id}/attendance/calendar response
 * Source: Buy2.Application.DTOs.Employees.AttendanceCalendarDtos
 */

/** Attendance Day Status enum - matches backend AttendanceDayStatus */
export enum AttendanceDayStatus {
  OnTime = 1,
  Late = 2,
  ApprovedLeave = 3,
  UnapprovedLeave = 4,
  PartialLeave = 5,
  NoAttendance = 6,
  PublicHoliday = 7,
  AttendanceNotRequired = 8,
}

/** Attendance Calendar Summary - matches AttendanceSummaryDto */
export interface AttendanceSummaryDto {
  readonly attendanceRate: number;
  readonly punctualityScore: number;
  readonly averageLatenessMinutes: number;
  readonly recordedHours: number;
  readonly targetHours: number;
}

/** Single day attendance data - matches AttendanceDayDto */
export interface AttendanceDayDto {
  readonly date: string; // ISO 8601
  readonly status: AttendanceDayStatus;
  readonly leaveType?: string | null;
  readonly hoursWorked: number;
  readonly hoursLeft: number;
  readonly otHours: number;
  readonly breakTime: number; // minutes
  readonly latenessMinutes?: number | null;
}

/** Full attendance calendar response - matches AttendanceCalendarDto */
export interface AttendanceCalendarDto {
  readonly summary: AttendanceSummaryDto;
  readonly days: readonly AttendanceDayDto[];
}
