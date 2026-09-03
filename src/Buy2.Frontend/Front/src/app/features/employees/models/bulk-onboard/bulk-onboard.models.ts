export interface BulkOnboardEmployeeItem {
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly jobRoleId: number;
  readonly roleId: number;
}

export interface BulkOnboardRequest {
  readonly employees: readonly BulkOnboardEmployeeItem[];
}

export interface BulkOnboardRowError {
  readonly rowIndex: number;
  readonly email: string | null;
  readonly errorMessage: string;
}

export interface BulkOnboardResult {
  readonly totalCount: number;
  readonly createdCount: number;
  readonly failedCount: number;
  readonly failedRows: readonly BulkOnboardRowError[];
}
