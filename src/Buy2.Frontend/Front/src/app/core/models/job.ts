/**
 * Job API Response
 * Source: GET /api/v1/job-roles
 */
export interface Job {
  readonly id: number;
  readonly title: string;
  readonly departmentId: number | null;
  /** Legacy entity-shaped field; the paginated jobs API returns a qualification count instead. */
  readonly requiredQualificationsJson?: string;
  readonly departmentName?: string | null;
  readonly seniorityLevel?: string;
  readonly workModel?: string;
  readonly assignedEmployeesCount?: number;
  readonly requiredQualificationsCount?: number;
  readonly experienceYearsMin?: number;
  readonly isActive?: boolean;
  readonly createdAt: string;
}

export interface JobPaginatedResponse {
  readonly items: readonly Job[];
  readonly totalCount: number;
  readonly pageNumber: number;
  readonly pageSize: number;
  readonly totalPages: number;
}
