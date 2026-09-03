/**
 * Role API Response
 * Source: GET /api/v1/roles
 */
export interface Role {
  readonly id: number;
  readonly name: string;
  /** Legacy entity-shaped field; the paginated roles API returns permissionsSummary instead. */
  readonly permissionsJson?: string;
  readonly description?: string | null;
  readonly assignedEmployeesCount?: number;
  readonly isSystemRole?: boolean;
  readonly isActive?: boolean;
  readonly createdAt: string;
  readonly permissionsSummary?: readonly string[];
}

export interface RolePaginatedResponse {
  readonly items: readonly Role[];
  readonly totalCount: number;
  readonly pageNumber: number;
  readonly pageSize: number;
  readonly totalPages: number;
}
