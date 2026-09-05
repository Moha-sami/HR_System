import type { NewsPost, NewsPostStatus } from '../models/news.models';

const MAX_FILE_BYTES = 5 * 1024 * 1024;
const ALLOWED_TYPES = new Set(['application/pdf', 'image/png', 'image/jpeg']);
const ALLOWED_EXTENSIONS = ['.pdf', '.png', '.jpg', '.jpeg'];

export function formatNewsDateTime(iso: string | null | undefined): { date: string; time: string } {
  if (!iso) {
    return { date: '', time: '' };
  }
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) {
    return { date: '', time: '' };
  }
  const date = d.toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' });
  const time = d.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
  return { date, time };
}

export function formatLikeCount(count: number): string {
  if (count >= 1_000_000) {
    return `${trimDecimal(count / 1_000_000)}M`;
  }
  if (count >= 1_000) {
    return `${trimDecimal(count / 1_000)}K`;
  }
  return String(count);
}

function trimDecimal(value: number): string {
  return value.toFixed(1).replace(/\.0$/, '');
}

export function isImageAttachment(url: string, fileName: string): boolean {
  const lower = `${url} ${fileName}`.toLowerCase();
  return (
    lower.includes('image/') ||
    lower.includes('.png') ||
    lower.includes('.jpg') ||
    lower.includes('.jpeg') ||
    url.startsWith('data:image/')
  );
}

export function validateNewsFile(file: File): string | null {
  const ext = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();
  const typeOk = ALLOWED_TYPES.has(file.type) || ALLOWED_EXTENSIONS.includes(ext);
  if (!typeOk) {
    return 'NEWS.FILE_TYPE_INVALID';
  }
  if (file.size > MAX_FILE_BYTES) {
    return 'NEWS.FILE_TOO_LARGE';
  }
  return null;
}

export function toDateInputValue(iso: string | null): string {
  if (!iso) {
    return '';
  }
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) {
    return '';
  }
  const month = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${d.getFullYear()}-${month}-${day}`;
}

export function toTimeInputValue(iso: string | null): string {
  if (!iso) {
    return '';
  }
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) {
    return '';
  }
  return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
}

export function resolvePublishStatus(
  date: string,
  time: string,
): { status: Extract<NewsPostStatus, 'published' | 'scheduled'>; publishAt: string } {
  if (!date && !time) {
    return { status: 'published', publishAt: new Date().toISOString() };
  }
  const isoLocal = `${date || toDateInputValue(new Date().toISOString())}T${time || '00:00'}:00`;
  const dt = new Date(isoLocal);
  if (dt.getTime() > Date.now()) {
    return { status: 'scheduled', publishAt: dt.toISOString() };
  }
  return { status: 'published', publishAt: dt.toISOString() };
}

export function formatRelativeTime(iso: string): string {
  const diffMs = Date.now() - new Date(iso).getTime();
  const minutes = Math.max(0, Math.floor(diffMs / 60_000));
  if (minutes < 1) {
    return 'now';
  }
  if (minutes < 60) {
    return `${minutes}m ago`;
  }
  const hours = Math.floor(minutes / 60);
  if (hours < 24) {
    return `${hours}h ago`;
  }
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
}

export function effectiveStatus(post: NewsPost): NewsPostStatus {
  if (post.status === 'scheduled' && post.publishAt && new Date(post.publishAt).getTime() <= Date.now()) {
    return 'published';
  }
  return post.status;
}
