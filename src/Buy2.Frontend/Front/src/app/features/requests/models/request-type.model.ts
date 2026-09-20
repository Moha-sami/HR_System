export interface RequestType {
  id: string;
  category: string;
  name: string;
  hint: string;
  leaveType?: 'Full' | 'Partial' | null;
  leavePay?: 'Paid' | 'Unpaid' | null;
  createdAt?: string;
  addedBy?: string;
}
