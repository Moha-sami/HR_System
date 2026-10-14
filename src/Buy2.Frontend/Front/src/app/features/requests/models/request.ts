export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface SubmittedRequest {
  id: number;
  employeeId: number;
  employeeName: string;
  employeeCode: string | null;
  requestType: string;
  category: string;
  submittedAt: string;
  startDate: string;
  endDate: string;
  managerName: string;
  managerStatus: string;
  hrStatus: string;
  status: string;
  reason: string;
  attachmentsCount: number;
}

export interface RequestHistoryItem {
  id: number;
  employeeId: number;
  employeeName: string;
  employeeCode: string | null;
  requestType: string;
  category: string;
  submittedAt: string;
  startDate: string;
  endDate: string;
  managerName: string;
  managerStatus: string;
  managerComment: string | null;
  hrStatus: string;
  hrComment: string | null;
  status: string;
  rejectionReason: string | null;
  resolvedAt: string | null;
  attachmentsCount: number;
}

export interface RequestDetails {
  id: number;
  employeeId: number;
  employeeName: string;
  employeeCode: string | null;
  departmentName: string;
  requestType: string;
  category: string;
  submittedAt: string;
  startDate: string;
  endDate: string;
  reason: string;
  categoryValuesJson: string;
  overallStatus: string;
  rejectionReason: string | null;
  managerReview: any;
  hrReview: any;
  attachments: any[];
  previousRequests: any[];
}

export interface RequestDecisionDto {
  tier: string;
  decision: string;
  comment: string;
  rejectionReason?: string;
  reviewerId?: number;
}
