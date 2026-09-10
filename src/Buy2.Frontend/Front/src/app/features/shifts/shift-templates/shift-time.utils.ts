/**
 * Ticket #327: 12-hour time helpers for the template editor.
 * Backend contract: 'hh:mm tt' strings (e.g. '09:00 AM'), same-day ranges.
 */

export type Meridiem = 'AM' | 'PM';

export interface TimeParts {
  /** 12-hour clock hour 1-12, zero-padded to 2 digits. */
  hour: string;
  /** Minutes 00-59. */
  minute: string;
  meridiem: Meridiem;
}

const TIME_INPUT = /^(0?[1-9]|1[0-2]):([0-5]\d)$/;
const TIME_WITH_MERIDIEM = /^(0?[1-9]|1[0-2]):([0-5]\d)\s*([AP])\.?M\.?$/i;

/** Parse an 'hh:mm' input plus meridiem select into minutes since midnight. Null when invalid. */
export function parseTimeInput(hourMinute: string, meridiem: Meridiem): number | null {
  const match = TIME_INPUT.exec(hourMinute.trim());
  if (!match) return null;
  let hour = Number(match[1]);
  const minute = Number(match[2]);
  if (meridiem === 'AM') {
    if (hour === 12) hour = 0;
  } else if (hour !== 12) {
    hour += 12;
  }
  return hour * 60 + minute;
}

/** Parse a backend 'hh:mm tt' string into minutes since midnight. Null when invalid. */
export function parseBackendTime(value: string): number | null {
  const match = TIME_WITH_MERIDIEM.exec(value.trim());
  if (!match) return null;
  return parseTimeInput(`${match[1]}:${match[2]}`, match[3].toUpperCase() === 'A' ? 'AM' : 'PM');
}

/** Format minutes since midnight as backend 'hh:mm tt'. */
export function formatBackendTime(totalMinutes: number): string {
  const normalized = ((totalMinutes % 1440) + 1440) % 1440;
  const hour24 = Math.floor(normalized / 60);
  const minute = normalized % 60;
  const meridiem: Meridiem = hour24 < 12 ? 'AM' : 'PM';
  const hour12 = hour24 % 12 === 0 ? 12 : hour24 % 12;
  return `${String(hour12).padStart(2, '0')}:${String(minute).padStart(2, '0')} ${meridiem}`;
}

/** Split minutes since midnight into editor input parts. */
export function splitTime(totalMinutes: number): TimeParts {
  const normalized = ((totalMinutes % 1440) + 1440) % 1440;
  const hour24 = Math.floor(normalized / 60);
  const hour12 = hour24 % 12 === 0 ? 12 : hour24 % 12;
  return {
    hour: String(hour12).padStart(2, '0'),
    minute: String(normalized % 60).padStart(2, '0'),
    meridiem: hour24 < 12 ? 'AM' : 'PM',
  };
}

/** Half-open overlap check: [startA, endA) vs [startB, endB). */
export function rangesOverlap(startA: number, endA: number, startB: number, endB: number): boolean {
  return startA < endB && startB < endA;
}
