/**
 * Job API Response
 * Source: GET /api/v1/jobs
 */
export interface Job {
  readonly id: number;
  readonly title: string;
  readonly departmentId: number | null;
  /** Legacy entity-shaped field; the paginated jobs API returns a qualification count instead. */
  readonly requiredQualificationsJson?: string;
  readonly departmentName: string | null;
  readonly seniorityLevel: string;
  readonly workModel: string;
  readonly assignedEmployeesCount: number;
  readonly requiredQualificationsCount: number;
  readonly experienceYearsMin: number;
  readonly isActive: boolean;
  readonly createdAt: string;
}

export interface JobPaginatedResponse {
  readonly items: readonly Job[];
  readonly totalCount: number;
  readonly pageNumber: number;
  readonly pageSize: number;
  readonly totalPages: number;
}

export interface JobDetail {
  readonly id: number;
  readonly title: string;
  readonly departmentId: number;
  readonly departmentName: string;
  readonly seniorityLevel: string;
  readonly description: string | null;
  readonly requiredQualifications: readonly string[];
  readonly experienceYearsMin: number;
  readonly workModel: string;
  readonly onlineWorkdays: readonly string[];
  readonly offlineWorkdays: readonly string[];
  readonly assignedEmployeesCount: number;
  readonly isActive: boolean;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface JobEmployee {
  readonly id: number;
  readonly employeeCode: string;
  readonly fullName: string;
  readonly email: string;
  readonly departmentName: string;
  readonly siteName: string;
  readonly joinDate: string;
  readonly profilePhotoUrl: string | null;
}

export interface JobEmployeePaginatedResponse {
  readonly items: readonly JobEmployee[];
  readonly totalCount: number;
  readonly pageNumber: number;
  readonly pageSize: number;
  readonly totalPages: number;
}
