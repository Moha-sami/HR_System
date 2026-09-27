export interface EmployeeDocumentDto {
  id: number;
  employeeId: number;
  category: string;
  storageUrl: string;
  createdAt: string;
}

export type DocumentCategory =
  | 'All'
  | 'Identification'
  | 'Contracts'
  | 'Certificates'
  | 'Medical'
  | 'Other';

export const DOCUMENT_CATEGORIES: readonly DocumentCategory[] = [
  'All',
  'Identification',
  'Contracts',
  'Certificates',
  'Medical',
  'Other',
] as const;

export interface UploadEmployeeDocumentRequest {
  employeeId: number;
  category: string;
  storageUrl: string;
}
