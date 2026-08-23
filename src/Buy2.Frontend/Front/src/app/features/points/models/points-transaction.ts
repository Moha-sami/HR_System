export type PointsTransactionKind = 'Credit' | 'Debit' | 'ManualAdjustment';

export interface PointsEmployee {
  id: string;
  firstName: string;
  lastName: string;
}

export interface CreatePointsTransactionInput {
  employeeId: string | number;
  pointsValue: number;
  type: 'Add' | 'Deduct';
  comments: string;
}

export interface PointsTransactionResponse {
  id: string | number;
  employeeId: string | number;
  pointsRuleId: string | number | null;
  amount: number;
  transactionType: PointsTransactionKind | string;
  createdAt: string;
  comments?: string;
}

export interface PointTableRow {
  id: string;
  name: string;
  date: string;
  time: string;
  month: string;
  createdAt: string;
  type: string;
  points: number;
  triggeredBy: string;
  transactionType: string;
  comments: string;
}
