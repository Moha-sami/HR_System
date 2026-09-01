/**
 * Role models matching backend API responses
 * Source: API_ENDPOINTS.md - GET /api/v1/roles and GET /api/v1/roles/{id}
 */

export interface RoleListItem {
  id: number;
  name: string;
  description: string | null;
  assignedEmployeesCount: number;
  isSystemRole: boolean;
  isActive: boolean;
  createdAt: string; // ISO 8601
  permissionsSummary: readonly string[];
}

export interface ModulePermissionDto {
  module: string;
  actions: readonly string[] | null;
  scope: PermissionScopeDto | null;
}

export interface PermissionScopeDto {
  scopeType: string;
  targetIds: readonly number[] | null;
}

export interface RoleDetails {
  id: number;
  name: string;
  description: string | null;
  isSystemRole: boolean;
  isActive: boolean;
  assignedEmployeesCount: number;
  createdAt: string;
  updatedAt: string | null;
  permissions: readonly ModulePermissionDto[];
}

export interface RolePaginatedResponse {
  items: readonly RoleListItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface RoleLookupItem {
  id: number;
  name: string;
}

/** Input for creating a role (matches CreateRoleDto) */
export interface CreateRoleInput {
  name: string;
  description?: string | null;
  isActive?: boolean;
  permissions: readonly ModulePermissionDto[];
}

/** Input for updating a role */
export interface UpdateRoleInput {
  name?: string;
  description?: string | null;
  permissions?: readonly ModulePermissionDto[];
}

/** @deprecated Use RoleListItem or RoleDetails instead */
export interface Role {
  id: number;
  roleName: string;
  description?: string | null;
  permissions: string[];
}

/** Deletion impact response from backend */
export interface DeletionImpact {
  roleId: number;
  roleName: string;
  isSystemRole: boolean;
  canDeleteDirectly: boolean;
  assignedEmployeesCount: number;
  affectedEmployees: readonly AffectedEmployee[];
}

export interface AffectedEmployee {
  employeeId: number;
  employeeCode: string;
  fullName: string;
  email: string;
  jobRoleTitle: string;
  siteName: string;
}

/** Legacy mock roles for backward compatibility with tests */
export const MOCK_ROLES: readonly RoleListItem[] = [
  {
    id: 1,
    name: 'Super Admin',
    description: 'System administrator with full permissions',
    assignedEmployeesCount: 3,
    isSystemRole: true,
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    permissionsSummary: [
      'EmployeeManagement',
      'JobManagement',
      'SiteManagement',
      'PointsManagement',
      'NotificationsManagement',
      'RewardManagement',
    ],
  },
  {
    id: 2,
    name: 'Facilitator',
    description: 'Facilitator role for site operations',
    assignedEmployeesCount: 12,
    isSystemRole: false,
    isActive: true,
    createdAt: '2026-02-15T10:30:00Z',
    permissionsSummary: ['SiteManagement', 'RewardManagement'],
  },
] as const;
