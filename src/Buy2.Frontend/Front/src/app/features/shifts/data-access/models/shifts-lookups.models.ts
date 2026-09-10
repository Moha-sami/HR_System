/** Minimal lookup shapes owned by the shifts area (no cross-feature imports). */

export interface ShiftsSiteLookup {
  id: number;
  siteName: string;
}

export interface ShiftsJobRoleLookup {
  id: number;
  title: string;
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
