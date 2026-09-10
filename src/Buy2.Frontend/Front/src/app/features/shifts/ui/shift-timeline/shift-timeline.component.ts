import { Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { CdkDragDrop, CdkDropList } from '@angular/cdk/drag-drop';
import {
  assignLanes,
  gapSpans,
  hourTicks,
  toPercent,
  type TimelineBlock,
} from './shift-timeline.utils';

export type { TimelineBlock };

const LANE_HEIGHT = 88;
const BAR_HEIGHT = 78;

interface BarView {
  id: string | number;
  label: string;
  time: string;
  assigned: boolean;
  assigneeName: string | null;
  top: number;
  startPct: number;
  widthPct: number;
}

interface TickView {
  minute: number;
  label: string;
  leftPct: number;
}

interface GapView {
  startPct: number;
  widthPct: number;
}

/**
 * Ticket #328: visual coverage view. Posted blocks render as bars on a
 * same-day time axis with edit/assign/delete actions per bar; uncovered
 * spans are shaded. Each bar is a drop target for strip employee cards
 * (drag payload stays untyped here; the host interprets it).
 * Positioning uses logical inset-inline-start so the axis mirrors in RTL.
 */
@Component({
  selector: 'app-shift-timeline',
  standalone: true,
  imports: [TranslatePipe, CdkDropList],
  templateUrl: './shift-timeline.component.html',
})
export class ShiftTimelineComponent {
  readonly blocks = input<TimelineBlock[]>([]);
  readonly rangeStart = input<number | null>(null);
  readonly rangeEnd = input<number | null>(null);

  readonly editBlock = output<string | number>();
  readonly assignBlock = output<string | number>();
  readonly deleteBlock = output<string | number>();
  readonly dropOnBlock = output<{ id: string | number; data: unknown }>();

  readonly valid = computed(() => {
    const start = this.rangeStart();
    const end = this.rangeEnd();
    return start !== null && end !== null && end > start;
  });

  private readonly lanes = computed(() => assignLanes(this.blocks()));

  readonly laneCount = computed(() =>
    this.lanes().reduce((max, p) => Math.max(max, p.lane + 1), 1),
  );

  readonly canvasHeight = computed(() => this.laneCount() * LANE_HEIGHT);

  readonly bars = computed<BarView[]>(() => {
    const start = this.rangeStart();
    const end = this.rangeEnd();
    if (start === null || end === null || end <= start) return [];
    const byId = new Map(this.lanes().map((p) => [p.id, p.lane]));
    return this.blocks().map((b) => ({
      id: b.id,
      label: b.label,
      time: `${this.tickLabel(b.start)} - ${this.tickLabel(b.end)}`,
      assigned: b.assigned,
      assigneeName: b.assigneeName ?? null,
      top: (byId.get(b.id) ?? 0) * LANE_HEIGHT,
      startPct: toPercent(b.start, start, end),
      widthPct: Math.max(toPercent(b.end, start, end) - toPercent(b.start, start, end), 0),
    }));
  });

  readonly ticks = computed<TickView[]>(() => {
    const start = this.rangeStart();
    const end = this.rangeEnd();
    if (start === null || end === null || end <= start) return [];
    return hourTicks(start, end).map((minute) => ({
      minute,
      label: this.tickLabel(minute),
      leftPct: toPercent(minute, start, end),
    }));
  });

  readonly gaps = computed<GapView[]>(() => {
    const start = this.rangeStart();
    const end = this.rangeEnd();
    if (start === null || end === null || end <= start) return [];
    return gapSpans(this.blocks(), start, end).map((g) => ({
      startPct: toPercent(g.start, start, end),
      widthPct: toPercent(g.end, start, end) - toPercent(g.start, start, end),
    }));
  });

  tickLabel(minute: number): string {
    const h24 = Math.floor(minute / 60) % 24;
    const h12 = ((h24 + 11) % 12) + 1;
    return `${h12} ${h24 < 12 ? 'AM' : 'PM'}`;
  }

  onBarDrop(event: CdkDragDrop<string | number>): void {
    this.dropOnBlock.emit({ id: event.container.data, data: event.item.data });
  }

  protected readonly barHeight = BAR_HEIGHT;
}
