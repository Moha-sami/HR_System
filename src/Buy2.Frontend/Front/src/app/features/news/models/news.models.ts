export type NewsPostStatus = 'draft' | 'scheduled' | 'published' | 'archived';

export const CURRENT_NEWS_AUTHOR = 'Mohamed Ahmed';

export interface NewsCategory {
  id: string;
  name: string;
}

export interface NewsPost {
  id: string;
  title: string;
  body: string;
  category: string;
  status: NewsPostStatus;
  publishAt: string | null;
  createdAt: string;
  updatedAt: string;
  createdBy: string;
  updatedBy: string;
  attachmentUrl: string;
  attachmentName: string;
  likesCount: number;
}

export type CreateNewsPostDto = Omit<NewsPost, 'id'>;
