export type PointsTransactionType = 'Add' | 'Deduct' | 'Earned' | 'Redeemed';
export type ManualPointsTransactionType = 'Add' | 'Deduct';
export type PointsSortDirection = 'Asc' | 'Desc';
export type PointsSortBy =
  | 'CreatedAt'
  | 'Date'
  | 'TransactionType'
  | 'Points'
  | 'Amount'
  | 'EmployeeName';

export interface PointsEmployee {
  readonly id: number;
  readonly employeeName: string;
}

export interface CreatePointsTransactionInput {
  readonly employeeId: number;
  readonly transactionType: ManualPointsTransactionType;
  readonly pointsValue: number;
  readonly comments: string;
}

export interface CreateManualPointsTransactionResult {
  readonly isSuccess: boolean;
  readonly transactionId?: number | null;
  readonly errorMessage?: string | null;
  readonly isNotFound?: boolean;
}

export interface PointsTransactionFilter {
  readonly pageNumber: number;
  readonly pageSize: number;
  readonly searchTerm?: string | null;
  readonly triggeredBy?: string | null;
  readonly transactionType?: PointsTransactionType | null;
  readonly sortBy?: PointsSortBy | null;
  readonly sortDir?: PointsSortDirection | null;
  readonly month?: number | null;
  readonly year?: number | null;
}

export interface PointsTransactionListItem {
  readonly id: number;
  readonly employeeId: number;
  readonly employeeName: string;
  readonly employeeCode: string;
  readonly departmentName: string;
  readonly siteName: string;
  readonly avatarUrl: string | null;
  readonly date: string;
  readonly time: string;
  readonly transactionType: string;
  readonly points: number;
  readonly triggeredBy: string;
  readonly comments: string | null;
  readonly createdAt: string;
}

export interface PaginatedPointsTransactions {
  readonly items: readonly PointsTransactionListItem[];
  readonly totalCount: number;
  readonly pageNumber: number;
  readonly pageSize: number;
  readonly totalPages: number;
}

export interface PointTableRow {
  readonly id: number;
  readonly employeeName: string;
  readonly date: string;
  readonly time: string;
  readonly transactionType: string;
  readonly points: number;
  readonly triggeredBy: string;
  readonly comments: string;
}
