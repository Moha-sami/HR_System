export interface RangeLike {
  from: number;
  to: number;
}

/** Returns indices of ranges that overlap with at least one other range. */
export function findOverlappingRangeIndexes(ranges: readonly RangeLike[]): number[] {
  const overlapping = new Set<number>();

  for (let i = 0; i < ranges.length; i++) {
    const a = ranges[i];
    if (a.from > a.to) {
      overlapping.add(i);
      continue;
    }

    for (let j = i + 1; j < ranges.length; j++) {
      const b = ranges[j];
      if (a.from <= b.to && b.from <= a.to) {
        overlapping.add(i);
        overlapping.add(j);
      }
    }
  }

  return [...overlapping];
}

export function hasRangeOverlap(ranges: readonly RangeLike[]): boolean {
  return findOverlappingRangeIndexes(ranges).length > 0;
}
