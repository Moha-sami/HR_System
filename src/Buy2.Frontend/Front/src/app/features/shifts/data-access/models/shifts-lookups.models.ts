/** Minimal lookup shapes owned by the shifts area (no cross-feature imports). */

export interface ShiftsSiteLookup {
  id: number;
  siteName: string;
}

export interface ShiftsJobRoleLookup {
  id: number;
  title: string;
}

/** Minimal subset of GET /jobs paginated response used for the role lookup. */
export interface ShiftsJobsPage {
  items: ShiftsJobRoleLookup[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface ShiftsSiteEmployee {
  employeeId: number;
  fullName: string;
  roleName: string;
}

export interface ShiftsEmployeesPage {
  items: ShiftsSiteEmployee[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/** One row of GET /api/v1/shifts/employees (ShiftCandidateEmployeeItemDto, camelCase). */
export interface ShiftCandidateEmployee {
  id: number;
  employeeCode: string;
  fullName: string;
  roleTitle: string;
  jobRoleId: number;
  weeklyCompletedHours: number;
  ratingScore: number;
  riskStatusToken: string;
  isPreferredForSite: boolean;
}

export interface ShiftsCandidateEmployeesPage {
  items: ShiftCandidateEmployee[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/** Filter for GET /api/v1/shifts/employees. Backend takes a single SiteId. */
export interface ShiftEmployeesFilter {
  siteIds: number[];
  page: number;
  pageSize: number;
  search?: string;
  roleIds?: number[];
  ratingTiers?: string[];
  isPreferredOnly?: boolean;
}
