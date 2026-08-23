/**
 * Job API Response
 * Source: GET /api/v1/job-roles
 */
export interface Job {
  readonly id: number;
  readonly title: string;
  readonly departmentId: number;
  readonly requiredQualificationsJson: string;
  readonly createdAt: string;
}
