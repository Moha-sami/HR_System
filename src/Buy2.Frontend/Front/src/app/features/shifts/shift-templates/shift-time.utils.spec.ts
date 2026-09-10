import {
  describe,
  it,
  expect,
} from 'vitest';
import {
  formatBackendTime,
  parseBackendTime,
  parseTimeInput,
  rangesOverlap,
  splitTime,
} from './shift-time.utils';

/** Ticket #327: time helpers mirror the backend 'hh:mm tt' contract. */
describe('shift-time.utils', () => {
  it('should parse hh:mm inputs with meridiem into minutes since midnight', () => {
    expect(parseTimeInput('09:00', 'AM')).toBe(540);
    expect(parseTimeInput('05:00', 'PM')).toBe(1020);
    expect(parseTimeInput('12:00', 'AM')).toBe(0);
    expect(parseTimeInput('12:00', 'PM')).toBe(720);
    expect(parseTimeInput('9:00', 'AM')).toBe(540);
  });

  it('should reject malformed time inputs', () => {
    expect(parseTimeInput('25:00', 'AM')).toBeNull();
    expect(parseTimeInput('09:60', 'AM')).toBeNull();
    expect(parseTimeInput('9', 'AM')).toBeNull();
    expect(parseTimeInput('', 'PM')).toBeNull();
  });

  it('should round-trip backend hh:mm tt strings', () => {
    expect(parseBackendTime('09:00 AM')).toBe(540);
    expect(parseBackendTime('05:00 PM')).toBe(1020);
    expect(parseBackendTime('9:00am')).toBe(540);
    expect(parseBackendTime('not a time')).toBeNull();
    expect(formatBackendTime(540)).toBe('09:00 AM');
    expect(formatBackendTime(1020)).toBe('05:00 PM');
    expect(formatBackendTime(0)).toBe('12:00 AM');
  });

  it('should split minutes into editor input parts', () => {
    expect(splitTime(540)).toEqual({ hour: '09', minute: '00', meridiem: 'AM' });
    expect(splitTime(1020)).toEqual({ hour: '05', minute: '00', meridiem: 'PM' });
  });

  it('should detect half-open range overlaps', () => {
    expect(rangesOverlap(540, 660, 600, 720)).toBe(true);
    expect(rangesOverlap(540, 660, 660, 720)).toBe(false);
    expect(rangesOverlap(660, 720, 540, 660)).toBe(false);
  });
});
