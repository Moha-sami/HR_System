/**
 * Violations Models - matches GET /api/v1/employees/{id}/violations response
 * Source: Buy2.Application.DTOs.Employees.ViolationDtos
 */

/** Single violation row from the API */
export interface ViolationDto {
  readonly id: number;
  readonly employeeId: number;
  readonly type: string;
  readonly severity: string;
  readonly description: string;
  readonly status: string;
  readonly reportedByName: string;
  readonly createdAt: string; // ISO 8601
  readonly actionType?: string | null;
  readonly actionDate?: string | null;
  readonly documentUrl?: string | null;
}

/** Query parameters for filtering */
export interface ViolationFilters {
  readonly type?: string | null;
  readonly severityLevel?: string | null;
  readonly dateFrom?: string | null;
  readonly dateTo?: string | null;
}

/** Valid sort fields matching backend */
export type ViolationSortField = 'createdAt' | 'type' | 'severity' | 'status';

/** Sort direction */
export type ViolationSortDirection = 'asc' | 'desc';
