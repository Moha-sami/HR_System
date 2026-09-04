export type NewsPostStatus = 'draft' | 'scheduled' | 'published' | 'archived';

export const CURRENT_NEWS_AUTHOR = 'Mohamed Ahmed';
export const CURRENT_NEWS_AVATAR = '/human-avatar.avif';

export type NewsReaction = 'like' | 'dislike' | 'laugh' | 'wow' | 'heart' | 'angry';

export type NewsReactionCounts = Record<NewsReaction, number>;

export function emptyReactionCounts(): NewsReactionCounts {
  return { like: 0, dislike: 0, laugh: 0, wow: 0, heart: 0, angry: 0 };
}

export function toggleReaction(
  counts: NewsReactionCounts | undefined,
  myReaction: NewsReaction | null | undefined,
  key: NewsReaction,
): { reactionCounts: NewsReactionCounts; myReaction: NewsReaction | null } {
  const nextCounts = { ...emptyReactionCounts(), ...counts };
  const previous = myReaction ?? null;
  if (previous === key) {
    nextCounts[key] = Math.max(0, nextCounts[key] - 1);
    return { reactionCounts: nextCounts, myReaction: null };
  }
  if (previous) {
    nextCounts[previous] = Math.max(0, nextCounts[previous] - 1);
  }
  nextCounts[key] += 1;
  return { reactionCounts: nextCounts, myReaction: key };
}

export function normalizeReactionFields<
  T extends {
    reactionCounts?: NewsReactionCounts;
    myReaction?: NewsReaction | null;
    likesCount?: number;
    reaction?: NewsReaction | null;
  },
>(item: T): { reactionCounts: NewsReactionCounts; myReaction: NewsReaction | null } {
  const myReaction = item.myReaction ?? item.reaction ?? null;
  const counts = { ...emptyReactionCounts(), ...item.reactionCounts };
  if (!item.reactionCounts && item.likesCount) {
    counts[myReaction ?? 'like'] = item.likesCount;
  }
  return { reactionCounts: counts, myReaction };
}

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
  reactionCounts: NewsReactionCounts;
  myReaction: NewsReaction | null;
  commentsCount: number;
}

export type CreateNewsPostDto = Omit<NewsPost, 'id'>;

export interface NewsComment {
  id: string;
  postId: string;
  parentId: string;
  authorName: string;
  authorAvatar: string;
  content: string;
  createdAt: string;
  reactionCounts: NewsReactionCounts;
  myReaction: NewsReaction | null;
  removedByAdmin: boolean;
}

export type CreateNewsCommentDto = Omit<NewsComment, 'id'>;

export const NEWS_REACTIONS: { key: NewsReaction; emoji: string }[] = [
  { key: 'like', emoji: '👍' },
  { key: 'dislike', emoji: '👎' },
  { key: 'laugh', emoji: '😄' },
  { key: 'wow', emoji: '😮' },
  { key: 'heart', emoji: '❤️' },
  { key: 'angry', emoji: '😡' },
];
