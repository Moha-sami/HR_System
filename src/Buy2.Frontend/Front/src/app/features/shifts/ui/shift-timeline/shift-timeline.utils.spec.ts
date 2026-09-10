import { describe, expect, it } from 'vitest';
import {
  assignLanes,
  gapSpans,
  hourTicks,
  toPercent,
  type TimelineBlock,
} from './shift-timeline.utils';

const block = (id: number, start: number, end: number): TimelineBlock => ({
  id,
  start,
  end,
  label: `b${id}`,
  assigned: true,
});

describe('assignLanes', () => {
  it('puts non-overlapping blocks in one lane', () => {
    expect(assignLanes([block(1, 540, 600), block(2, 600, 660)])).toEqual([
      { id: 1, lane: 0 },
      { id: 2, lane: 0 },
    ]);
  });

  it('stacks the later-posted overlapping block in the lane below', () => {
    // 9:00-14:00 then 10:00-13:00.
    expect(assignLanes([block(1, 540, 840), block(2, 600, 780)])).toEqual([
      { id: 1, lane: 0 },
      { id: 2, lane: 1 },
    ]);
  });

  it('reuses a freed lane and breaks start ties by posted order', () => {
    const placements = assignLanes([block(1, 540, 600), block(2, 540, 600), block(3, 600, 660)]);
    expect(placements).toEqual([
      { id: 1, lane: 0 },
      { id: 2, lane: 1 },
      { id: 3, lane: 0 },
    ]);
  });
});

describe('toPercent', () => {
  it('maps range ends to 0 and 100', () => {
    expect(toPercent(540, 540, 1020)).toBe(0);
    expect(toPercent(1020, 540, 1020)).toBe(100);
    expect(toPercent(780, 540, 1020)).toBe(50);
  });
});

describe('hourTicks', () => {
  it('emits full hours inside the range', () => {
    expect(hourTicks(540, 1020)).toEqual([540, 600, 660, 720, 780, 840, 900, 960, 1020]);
  });
});

describe('gapSpans', () => {
  it('returns uncovered spans across all lanes', () => {
    const gaps = gapSpans([block(1, 540, 840), block(2, 600, 780)], 540, 1020);
    expect(gaps).toEqual([{ start: 840, end: 1020 }]);
  });

  it('returns the whole range when there are no blocks', () => {
    expect(gapSpans([], 540, 1020)).toEqual([{ start: 540, end: 1020 }]);
  });
});
