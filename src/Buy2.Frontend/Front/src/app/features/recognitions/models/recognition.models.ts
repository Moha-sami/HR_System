export type RecognitionStatus = 'draft' | 'published' | 'scheduled' | 'archived';

export interface Recognition {
  id: string;
  employeeId: string;
  title: string;
  description: string;
  points: number | null;
  status: RecognitionStatus;
  publishAt: string | null;
  attachmentUrl?: string;
  attachmentName?: string;
  createdAt: string;
  updatedAt: string;
  createdBy: string;
  updatedBy: string;
}

export type RecognitionInput = Omit<Recognition, 'id'>;
export interface RecognitionEmployee {
  id: string;
  firstName: string;
  lastName: string;
}
