/**
 * Employee Profile DTOs - matches GET /api/v1/employees/{id} response
 * Source: Buy2.Application.DTOs.Employees.EmployeeProfileDtos
 */

export interface EmployeeStatsDto {
  readonly totalPoints: number;
  readonly totalTasks: number;
  readonly totalGifts: number;
}

export interface EmployeePersonalInfoDto {
  readonly birthdate: string | null;
  readonly gender: number | null; // 1 = Male, 2 = Female
}

export interface EmployeeJobDetailsDto {
  readonly title: string;
  readonly department: string;
  readonly seniorityLevel: string;
  readonly experienceYears: number;
  readonly directManagerName: string | null;
  readonly jobType: string;
  readonly attendanceType?: string; // Not in GET response, only in PUT
  readonly qualifications: readonly string[];
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
}

/** Request payload for PUT /api/v1/employees/{id}/personal - API expects { dto: {...} } */
export interface UpdatePersonalInfoRequestDto {
  readonly firstName: string;
  readonly lastName: string;
  readonly phoneNumber: string;
  readonly dateOfBirth: string; // yyyy-MM-dd format
  readonly address: string;
  readonly emergencyContact: string;
  readonly nationalId: string;
}

export interface UpdatePersonalInfoWrapperDto {
  readonly dto: UpdatePersonalInfoRequestDto;
}

/** Request payload for PUT /api/v1/employees/{id}/job - API expects { dto: {...} } */
export interface UpdateJobDetailsRequestDto {
  readonly jobRoleId: number;
  readonly roleId: number;
  readonly siteId: number;
  readonly directManagerId: number;
  readonly seniorityLevel: string;
  readonly experienceYears: number;
  readonly jobType: string;
  readonly attendanceType: string;
  readonly joinDate: string;
}

export interface UpdateJobDetailsWrapperDto {
  readonly dto: UpdateJobDetailsRequestDto;
}
