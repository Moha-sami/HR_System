import type { AbstractControl, ValidationErrors } from '@angular/forms';
import type { RecognitionStatus } from '../models/recognition.models';

export const MAX_FILE_MIB = 5;
export const MAX_FILE_BYTES = MAX_FILE_MIB * 1024 * 1024;
export const FILE_ACCEPT = '.pdf,.png,.jpg,.jpeg';
export const STATUSES: RecognitionStatus[] = ['published', 'draft', 'scheduled', 'archived'];

export function requiredTrimmed(control: AbstractControl): ValidationErrors | null {
  return typeof control.value === 'string' && control.value.trim() ? null : { required: true };
}
export function validPoints(control: AbstractControl): ValidationErrors | null {
  const value = control.value;
  return value === null || value === '' || (typeof value === 'number' && Number.isFinite(value) && value >= 0)
    ? null : { points: true };
}
export function validateFile(file: File): string | null {
  const extension = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();
  if (!['application/pdf', 'image/png', 'image/jpeg'].includes(file.type) && !FILE_ACCEPT.split(',').includes(extension)) {
    return 'RECOGNITIONS.FILE_TYPE_ERROR';
  }
  return file.size > MAX_FILE_BYTES ? 'RECOGNITIONS.FILE_SIZE_ERROR' : null;
}
export function publishTimestamp(date: string, time: string): string | null {
  if (!date && !time) return null;
  if (!date || !time) throw new Error('Incomplete date/time');
  const value = new Date(`${date}T${time}:00`);
  if (!Number.isFinite(value.getTime())) throw new Error('Invalid date/time');
  return value.toISOString();
}
export function scheduledStatus(publishAt: string | null): 'published' | 'scheduled' {
  return publishAt && new Date(publishAt).getTime() > Date.now() ? 'scheduled' : 'published';
}
export function saveStatus(current: RecognitionStatus, publishAt: string | null): RecognitionStatus {
  return current === 'scheduled' ? scheduledStatus(publishAt) : current;
}
export function localDateTime(iso: string | null): { date: string; time: string } {
  if (!iso) return { date: '', time: '' };
  const d = new Date(iso);
  const pad = (v: number) => String(v).padStart(2, '0');
  return { date: `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`, time: `${pad(d.getHours())}:${pad(d.getMinutes())}` };
}
export function displayDate(iso: string | null, language: string, time = false): string {
  if (!iso) return '—';
  return new Intl.DateTimeFormat(language, time ? { hour: 'numeric', minute: '2-digit' } : { year: 'numeric', month: 'short', day: 'numeric' }).format(new Date(iso));
}
