import { FormControl } from '@angular/forms';
import { describe, expect, it } from 'vitest';
import { MAX_FILE_BYTES, publishTimestamp, requiredTrimmed, saveStatus, scheduledStatus, validateFile, validPoints } from './recognition.utils';

describe('Recognition validation and status transitions', () => {
  it('rejects whitespace-only required values', () => {
    expect(requiredTrimmed(new FormControl(' \n '))).toEqual({ required: true });
    expect(requiredTrimmed(new FormControl(' Award '))).toBeNull();
  });
  it('accepts absent and zero points but rejects negative/nonfinite values', () => {
    for (const value of [null, 0, 25.5]) expect(validPoints(new FormControl(value))).toBeNull();
    for (const value of [-1, NaN, Infinity]) expect(validPoints(new FormControl(value))).toEqual({ points: true });
  });
  it('keeps archived/draft/published records in their status on ordinary updates', () => {
    for (const status of ['archived', 'draft', 'published'] as const) {
      expect(saveStatus(status, '2099-01-01T10:00:00Z')).toBe(status);
      expect(saveStatus(status, null)).toBe(status);
    }
  });
  it('recalculates scheduled records and publishes when there is no schedule', () => {
    expect(saveStatus('scheduled', '2099-01-01T10:00:00Z')).toBe('scheduled');
    expect(saveStatus('scheduled', '2000-01-01T10:00:00Z')).toBe('published');
    expect(scheduledStatus(null)).toBe('published');
  });
  it('requires paired date/time and serializes local time to ISO', () => {
    expect(publishTimestamp('', '')).toBeNull();
    expect(() => publishTimestamp('2026-09-01', '')).toThrow();
    expect(() => publishTimestamp('', '10:00')).toThrow();
    expect(publishTimestamp('2026-09-01', '10:00')).toBe(new Date('2026-09-01T10:00:00').toISOString());
  });
  it('accepts exactly 5 MiB and rejects larger files and unsupported types', () => {
    expect(validateFile({ name: 'award.pdf', type: 'application/pdf', size: MAX_FILE_BYTES } as File)).toBeNull();
    expect(validateFile({ name: 'award.pdf', type: 'application/pdf', size: MAX_FILE_BYTES + 1 } as File)).toBe('RECOGNITIONS.FILE_SIZE_ERROR');
    expect(validateFile({ name: 'award.exe', type: '', size: 1 } as File)).toBe('RECOGNITIONS.FILE_TYPE_ERROR');
  });
});
