export interface RequestType {
  id: number;
  category: string;
  name: string;
  hint: string;
  leaveType?: 'Full' | 'Partial' | string | null;
  leavePay?: 'Paid' | 'Unpaid' | string | null;
  createdAt?: string;
  addedBy?: string;
  requiresDates?: boolean;
  requiresReason?: boolean;
  isActive?: boolean;
  isUtilized?: boolean;
}
