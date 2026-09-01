import { AfterViewInit, Component, TemplateRef, ViewChild, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import {
  CellContext,
  ColumnDef,
  TableComponent,
} from '@app/shared/components/table/table.component';
import { RewardDetailsContext } from '../../services/reward-details.context';

type PeriodKey = '7d' | '30d' | 'month' | 'year' | 'custom';

interface ChartBar {
  label: string;
  value: number;
}

@Component({
  selector: 'app-reward-information',
  standalone: true,
  imports: [FormsModule, TranslatePipe, TableComponent],
  templateUrl: './reward-information.component.html',
  styleUrl: './reward-information.component.css',
})
export class RewardInformationComponent implements AfterViewInit {
  readonly ctx = inject(RewardDetailsContext);
  private readonly translate = inject(TranslateService);

  @ViewChild('codeTemplate') codeTemplate!: TemplateRef<CellContext>;

  readonly period = signal<PeriodKey>('30d');
  readonly customFrom = signal('');
  readonly customTo = signal('');
  readonly periodOpen = signal(false);
  readonly cellTemplates = signal<Map<string, TemplateRef<CellContext>>>(new Map());

  readonly columns = computed<ColumnDef[]>(() => [
    { key: 'id', label: this.translate.instant('REWARD_MANAGEMENT.TX_ID') },
    { key: 'employeeName', label: this.translate.instant('REWARD_MANAGEMENT.TX_EMPLOYEE') },
    { key: 'date', label: this.translate.instant('REWARD_MANAGEMENT.TX_DATE') },
    { key: 'time', label: this.translate.instant('REWARD_MANAGEMENT.TX_TIME'), sortable: true },
    {
      key: 'voucherCode',
      label: this.translate.instant('REWARD_MANAGEMENT.TX_CODE'),
      template: 'codeTemplate',
    },
  ]);

  readonly range = computed(() => {
    const now = new Date();
    const end = endOfDay(now);
    switch (this.period()) {
      case '7d':
        return { start: addDays(now, -6), end };
      case '30d':
        return { start: addDays(now, -29), end };
      case 'month':
        return { start: new Date(now.getFullYear(), now.getMonth(), 1), end };
      case 'year':
        return { start: new Date(now.getFullYear(), 0, 1), end };
      case 'custom': {
        const from = this.customFrom() ? new Date(this.customFrom()) : addDays(now, -29);
        const to = this.customTo() ? endOfDay(new Date(this.customTo())) : end;
        return { start: from, end: to };
      }
    }
  });

  readonly redemptionDatesInRange = computed(() => {
    const { start, end } = this.range();
    const dates: Date[] = [];
    for (const item of this.ctx.inventory()) {
      if (item.status !== 'Redeemed') {
        continue;
      }
      const date = new Date(item.redeemedAt || item.createdAt);
      if (date >= start && date <= end) {
        dates.push(date);
      }
    }
    for (const item of this.ctx.redemptions()) {
      const date = new Date(item.redeemedAt);
      if (date >= start && date <= end) {
        dates.push(date);
      }
    }
    return dates;
  });

  readonly totalInRange = computed(() => this.redemptionDatesInRange().length);

  readonly chartBars = computed<ChartBar[]>(() => {
    const dates = this.redemptionDatesInRange();
    const { start, end } = this.range();
    const buckets = buildBuckets(start, end);
    for (const date of dates) {
      const key = closestBucket(date, buckets);
      if (key) {
        const bucket = buckets.find((item) => item.key === key);
        if (bucket) {
          bucket.value += 1;
        }
      }
    }
    return buckets.map((item) => ({ label: item.label, value: item.value }));
  });

  readonly maxBar = computed(() => Math.max(1, ...this.chartBars().map((bar) => bar.value)));

  readonly transactionRows = computed(() =>
    this.ctx.redemptions().map((item) => {
      const date = new Date(item.redeemedAt);
      return {
        id: item.id,
        employeeName: this.ctx.employeeName(item.employeeId),
        date: formatDisplayDate(date),
        time: formatDisplayTime(date),
        voucherCode: item.voucherCode,
      };
    }),
  );

  ngAfterViewInit(): void {
    this.cellTemplates.set(new Map([['codeTemplate', this.codeTemplate]]));
  }

  periodLabel(): string {
    const key = this.period();
    const map: Record<PeriodKey, string> = {
      '7d': 'REWARD_MANAGEMENT.PERIOD_7D',
      '30d': 'REWARD_MANAGEMENT.PERIOD_30D',
      month: 'REWARD_MANAGEMENT.PERIOD_MONTH',
      year: 'REWARD_MANAGEMENT.PERIOD_YEAR',
      custom: 'REWARD_MANAGEMENT.PERIOD_CUSTOM',
    };
    return this.translate.instant(map[key]);
  }

  selectPeriod(key: PeriodKey): void {
    this.period.set(key);
    if (key !== 'custom') {
      this.periodOpen.set(false);
    }
  }
}

function addDays(date: Date, days: number): Date {
  const next = new Date(date);
  next.setDate(next.getDate() + days);
  next.setHours(0, 0, 0, 0);
  return next;
}

function endOfDay(date: Date): Date {
  const next = new Date(date);
  next.setHours(23, 59, 59, 999);
  return next;
}

function buildBuckets(start: Date, end: Date): { key: string; label: string; value: number }[] {
  const spanDays = Math.max(1, Math.round((end.getTime() - start.getTime()) / 86400000));
  const count = Math.min(6, Math.max(4, Math.ceil(spanDays / 7)));
  const buckets: { key: string; label: string; value: number }[] = [];
  for (let i = 0; i < count; i++) {
    const t = start.getTime() + ((end.getTime() - start.getTime()) * i) / Math.max(count - 1, 1);
    const d = new Date(t);
    buckets.push({
      key: d.toISOString().slice(0, 10),
      label: d.toLocaleDateString(undefined, { day: 'numeric', month: 'short' }),
      value: 0,
    });
  }
  return buckets;
}

function closestBucket(date: Date, buckets: { key: string }[]): string | null {
  if (!buckets.length) {
    return null;
  }
  let best = buckets[0].key;
  let bestDiff = Infinity;
  for (const bucket of buckets) {
    const diff = Math.abs(new Date(bucket.key).getTime() - date.getTime());
    if (diff < bestDiff) {
      bestDiff = diff;
      best = bucket.key;
    }
  }
  return best;
}

function formatDisplayDate(date: Date): string {
  const dd = String(date.getDate()).padStart(2, '0');
  const mm = String(date.getMonth() + 1).padStart(2, '0');
  const yy = String(date.getFullYear()).slice(-2);
  return `${dd}-${mm}-${yy}`;
}

function formatDisplayTime(date: Date): string {
  let hours = date.getHours();
  const minutes = String(date.getMinutes()).padStart(2, '0');
  const suffix = hours >= 12 ? 'PM' : 'AM';
  hours = hours % 12 || 12;
  return `${String(hours).padStart(2, '0')}:${minutes} ${suffix}`;
}
