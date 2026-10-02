import { AfterViewInit, Component, OnInit, TemplateRef, ViewChild, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import {
  CellContext,
  ColumnDef,
  TableComponent,
} from '@app/shared/components/table/table.component';
import { Pagination } from '@app/shared/components/pagination/pagination';
import type { RewardAnalyticsDto, RewardAnalyticsFilter } from '../../models/reward.models';
import { RewardDetailsContext } from '../../services/reward-details.context';
import { RewardService } from '../../services/reward.service';

type PeriodKey = '7d' | '30d' | 'month' | 'year' | 'custom';

interface ChartBar {
  label: string;
  value: number;
}

@Component({
  selector: 'app-reward-information',
  standalone: true,
  imports: [FormsModule, TranslatePipe, TableComponent, Pagination],
  templateUrl: './reward-information.component.html',
  styleUrl: './reward-information.component.css',
})
export class RewardInformationComponent implements OnInit, AfterViewInit {
  readonly ctx = inject(RewardDetailsContext);
  private readonly rewardService = inject(RewardService);
  private readonly translate = inject(TranslateService);

  @ViewChild('codeTemplate') codeTemplate!: TemplateRef<CellContext>;

  readonly period = signal<PeriodKey>('30d');
  readonly customFrom = signal('');
  readonly customTo = signal('');
  readonly periodOpen = signal(false);
  readonly page = signal(1);
  readonly pageSize = 10;
  readonly analytics = signal<RewardAnalyticsDto | null>(null);
  readonly loading = signal(false);
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

  readonly totalInRange = computed(() => this.analytics()?.totalCount ?? 0);

  readonly totalPages = computed(() => Math.max(1, this.analytics()?.totalPages ?? 1));

  readonly chartBars = computed<ChartBar[]>(() =>
    (this.analytics()?.timeline ?? []).map((point) => ({
      label: point.periodLabel,
      value: point.redemptionCount,
    })),
  );

  readonly maxBar = computed(() => Math.max(1, ...this.chartBars().map((bar) => bar.value)));

  readonly transactionRows = computed(() =>
    (this.analytics()?.transactions ?? []).map((item) => ({
      id: item.id,
      employeeName: item.employeeName || item.employeeCode || '—',
      date: formatDisplayDate(new Date(item.redeemedAt)),
      time: formatDisplayTime(new Date(item.redeemedAt)),
      voucherCode: item.voucherCode,
    })),
  );

  ngOnInit(): void {
    this.loadAnalytics();
  }

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
    this.page.set(1);
    if (key !== 'custom') {
      this.periodOpen.set(false);
      this.loadAnalytics();
    }
  }

  onCustomRangeChange(): void {
    if (this.period() !== 'custom') {
      return;
    }
    this.page.set(1);
    this.loadAnalytics();
  }

  onPageChanged(page: number): void {
    this.page.set(page);
    this.loadAnalytics();
  }

  private loadAnalytics(): void {
    const rewardId = this.ctx.rewardId();
    if (!rewardId) {
      return;
    }
    this.loading.set(true);
    this.rewardService.getAnalytics(rewardId, this.currentFilter()).subscribe({
      next: (analytics) => {
        this.analytics.set(analytics);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  private currentFilter(): RewardAnalyticsFilter {
    const { start, end, timelinePeriod } = resolveRange(
      this.period(),
      this.customFrom(),
      this.customTo(),
    );
    return {
      dateFrom: start.toISOString(),
      dateTo: end.toISOString(),
      timelinePeriod,
      pageNumber: this.page(),
      pageSize: this.pageSize,
    };
  }
}

function resolveRange(
  period: PeriodKey,
  customFrom: string,
  customTo: string,
): { start: Date; end: Date; timelinePeriod: 'Weekly' | 'Monthly' } {
  const now = new Date();
  const end = endOfDay(now);
  switch (period) {
    case '7d':
      return { start: addDays(now, -6), end, timelinePeriod: 'Weekly' };
    case '30d':
      return { start: addDays(now, -29), end, timelinePeriod: 'Weekly' };
    case 'month':
      return { start: new Date(now.getFullYear(), now.getMonth(), 1), end, timelinePeriod: 'Monthly' };
    case 'year':
      return { start: new Date(now.getFullYear(), 0, 1), end, timelinePeriod: 'Monthly' };
    case 'custom': {
      const from = customFrom ? new Date(customFrom) : addDays(now, -29);
      const to = customTo ? endOfDay(new Date(customTo)) : end;
      return { start: from, end: to, timelinePeriod: 'Monthly' };
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

function formatDisplayDate(date: Date): string {
  if (Number.isNaN(date.getTime())) {
    return '—';
  }
  const dd = String(date.getDate()).padStart(2, '0');
  const mm = String(date.getMonth() + 1).padStart(2, '0');
  const yy = String(date.getFullYear()).slice(-2);
  return `${dd}-${mm}-${yy}`;
}

function formatDisplayTime(date: Date): string {
  if (Number.isNaN(date.getTime())) {
    return '—';
  }
  let hours = date.getHours();
  const minutes = String(date.getMinutes()).padStart(2, '0');
  const suffix = hours >= 12 ? 'PM' : 'AM';
  hours = hours % 12 || 12;
  return `${String(hours).padStart(2, '0')}:${minutes} ${suffix}`;
}
