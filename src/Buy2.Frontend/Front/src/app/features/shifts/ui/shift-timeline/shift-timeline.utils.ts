/** Timeline block model (ticket #328). Minutes since midnight; generic so later subfeatures reuse it. */
export interface TimelineBlock {
  id: string | number;
  start: number;
  end: number;
  label: string;
  assigned: boolean;
  assigneeName?: string | null;
}

export interface LanePlacement {
  id: string | number;
  lane: number;
}

export interface GapSpan {
  start: number;
  end: number;
}

/**
 * Greedy lane assignment: sort by start (ties by posted order), place each
 * block in the first lane free at its start, else open a new lane below.
 */
export function assignLanes(blocks: readonly TimelineBlock[]): LanePlacement[] {
  const ordered = blocks
    .map((b, index) => ({ b, index }))
    .sort((x, y) => x.b.start - y.b.start || x.index - y.index);
  const laneEnds: number[] = [];
  const out: LanePlacement[] = [];
  for (const { b } of ordered) {
    let lane = laneEnds.findIndex((end) => end <= b.start);
    if (lane === -1) {
      lane = laneEnds.length;
      laneEnds.push(b.end);
    } else {
      laneEnds[lane] = b.end;
    }
    out.push({ id: b.id, lane });
  }
  return out;
}

/** Percentage offset/width of a minute value within [rangeStart, rangeEnd]. */
export function toPercent(value: number, rangeStart: number, rangeEnd: number): number {
  if (rangeEnd <= rangeStart) return 0;
  return ((value - rangeStart) / (rangeEnd - rangeStart)) * 100;
}

/** Full-hour tick minutes within [rangeStart, rangeEnd]. */
export function hourTicks(rangeStart: number, rangeEnd: number): number[] {
  const ticks: number[] = [];
  const first = Math.ceil(rangeStart / 60) * 60;
  for (let t = first; t <= rangeEnd; t += 60) ticks.push(t);
  return ticks;
}

/** Spans inside the range covered by no block (clipped to the range). */
export function gapSpans(
  blocks: readonly TimelineBlock[],
  rangeStart: number,
  rangeEnd: number,
): GapSpan[] {
  if (rangeEnd <= rangeStart) return [];
  const covered = blocks
    .map((b) => ({
      start: Math.max(b.start, rangeStart),
      end: Math.min(b.end, rangeEnd),
    }))
    .filter((s) => s.end > s.start)
    .sort((x, y) => x.start - y.start);
  const gaps: GapSpan[] = [];
  let cursor = rangeStart;
  for (const span of covered) {
    if (span.start > cursor) gaps.push({ start: cursor, end: span.start });
    cursor = Math.max(cursor, span.end);
  }
  if (cursor < rangeEnd) gaps.push({ start: cursor, end: rangeEnd });
  return gaps;
}
