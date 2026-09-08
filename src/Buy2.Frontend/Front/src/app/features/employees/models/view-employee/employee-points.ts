export type EmployeePointsTransactionType = 'Add' | 'Deduct' | 'Earned' | 'Redeemed';

export interface EmployeePointsSummary {
  readonly currentBalance: number;
  readonly totalPointsRedeemed: number;
  readonly totalRewardsRedeemed: number;
  readonly totalRewardsCostPoints: number;
}

export interface EmployeePointsTransaction {
  readonly id: number;
  readonly date: string;
  readonly amount: number;
  readonly type: EmployeePointsTransactionType;
  readonly triggeredBy: string | null;
  readonly comments: string | null;
}

export interface PaginatedEmployeePointsTransactions {
  readonly items: readonly EmployeePointsTransaction[];
  readonly totalCount: number;
  readonly page: number;
  readonly pageSize: number;
  readonly totalPages: number;
}

export interface EmployeePointsTransactionFilters {
  readonly page: number;
  readonly pageSize: number;
  readonly type?: EmployeePointsTransactionType | null;
  readonly triggeredBy?: string | null;
  readonly dateFrom?: string | null;
  readonly dateTo?: string | null;
}
