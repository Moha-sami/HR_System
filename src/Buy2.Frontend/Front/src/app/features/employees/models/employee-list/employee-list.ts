/**
 * Employee List Models - matches GET /api/v1/employees response
 * Source: Buy2.Application.DTOs.Employees.EmployeeProfileDtos
 */

/** Single row in the paginated employee list */
export interface EmployeeListRowDto {
  readonly id: number;
  readonly employeeCode: string;
  readonly employeeName: string;
  readonly joinDate: string; // ISO date string
  readonly jobTitle: string;
  readonly email: string;
  readonly adminAccess: boolean;
}

/** Paginated response from the API */
export interface PaginatedEmployeeListDto {
  readonly items: readonly EmployeeListRowDto[];
  readonly totalCount: number;
  readonly page: number;
  readonly pageSize: number;
}

/** Query parameters for filtering/sorting/pagination */
export interface EmployeeFilterDto {
  readonly page: number;
  readonly pageSize: number;
  readonly search?: string | null;
  readonly department?: string | null;
  readonly region?: string | null;
  readonly sort?: EmployeeSortField | null;
  readonly sortDir?: SortDirection | null;
}

/** Valid sort fields matching backend */
export type EmployeeSortField = 'name' | 'employeecode' | 'email' | 'jobtitle' | 'joindate';

/** Sort direction */
export type SortDirection = 'asc' | 'desc';

/** Default filter values */
export const DEFAULT_EMPLOYEE_FILTER: EmployeeFilterDto = {
  page: 1,
  pageSize: 20,
  search: null,
  department: null,
  region: null,
  sort: 'joindate',
  sortDir: 'desc',
};

/** Convert filter to query string for API */
export function buildEmployeeQueryParams(filter: EmployeeFilterDto): string {
  const params = new URLSearchParams();

  params.set('page', filter.page.toString());
  params.set('pageSize', filter.pageSize.toString());

  if (filter.search?.trim()) {
    params.set('search', filter.search.trim());
  }
  if (filter.department?.trim()) {
    params.set('department', filter.department.trim());
  }
  if (filter.region?.trim()) {
    params.set('region', filter.region.trim());
  }
  if (filter.sort) {
    params.set('sort', filter.sort);
  }
  if (filter.sortDir) {
    params.set('sortDir', filter.sortDir);
  }

  return params.toString();
}
