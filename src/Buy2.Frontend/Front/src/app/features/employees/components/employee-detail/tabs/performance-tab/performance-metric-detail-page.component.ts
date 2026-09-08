import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { EmployeeDetailService } from '../../../../services/employee-detail.service';
import type { PerformanceFilters } from '../../../../models/view-employee/employee-performance';
import { LanguageService } from '../../../../../../core/services/language.service';

type CalendarPeriod = 'thisMonth' | 'thisWeek' | 'thisYear' | 'today';
type PeriodOption = CalendarPeriod | '30days' | '90days';

@Component({
  selector: 'app-performance-metric-detail-page',
  standalone: true,
  imports: [TranslatePipe, RouterLink],
  templateUrl: './performance-metric-detail-page.component.html',
})
export class PerformanceMetricDetailPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly params = toSignal(this.route.paramMap, {
    initialValue: this.route.snapshot.paramMap,
  });

  private readonly service = inject(EmployeeDetailService);
  readonly employee = this.service.detailEmployee;
  readonly employeeId = computed(() => {
    const id = this.employee()?.id;
    return id !== undefined && Number.isSafeInteger(id) && id > 0 ? id : null;
  });
  readonly metricId = computed<number | null>(() => {
    const raw = this.params().get('metricId');
    if (!raw || !/^\d+$/.test(raw)) return null;
    const id = Number(raw);
    // The backend route accepts a positive Int32 metric ID.
    return Number.isSafeInteger(id) && id > 0 && id <= 2147483647 ? id : null;
  });
  readonly invalidMetricId = computed(() => this.metricId() === null);
  readonly detail = this.service.performanceMetricDetail;
  private readonly language = inject(LanguageService);
  formatSubmissionDate(value: string): string {
    const date = new Date(value);
    if (!Number.isFinite(date.getTime())) return '—';
    return date.toLocaleDateString(this.language.currentLanguage() === 'ar' ? 'ar-EG' : 'en-US', {
      year: 'numeric', month: 'short', day: 'numeric', timeZone: 'UTC',
    });
  }
  readonly trendPoints = computed(() => {
    const locale = this.language.currentLanguage() === 'ar' ? 'ar-EG' : 'en-US';
    // All-time API order is authoritative, including duplicates and calendar gaps.
    return (this.detail()?.monthlyTrends ?? []).map((point, index) => {
      const date = new Date(0);
      date.setUTCFullYear(point.year, point.month - 1, 1);
      const monthLabel = date.toLocaleDateString(locale, { month: 'short', year: 'numeric', timeZone: 'UTC' });
      const visualScore = Number.isFinite(point.averageScore) ? Math.min(100, Math.max(0, point.averageScore)) : 0;
      return { ...point, monthLabel, x: 50 + index * 100, y: 220 - visualScore * 2 };
    });
  });
  readonly trendWidth = computed(() => Math.max(100, this.trendPoints().length * 100));
  readonly trendLine = computed(() => this.trendPoints().map(point => `${point.x},${point.y}`).join(' '));
  showTrendLabel(index: number): boolean {
    const count = this.trendPoints().length;
    return index === count - 1 || index % Math.max(1, Math.ceil(count / 8)) === 0;
  }
  readonly scoreProgress = computed(() => {
    const score = this.detail()?.periodAverageScore ?? 0;
    return Number.isFinite(score) ? Math.min(100, Math.max(0, score)) : 0;
  });
  readonly loading = this.service.performanceMetricDetailLoading;
  readonly error = this.service.performanceMetricDetailError;
  readonly selectedPeriod = signal<PeriodOption>('thisMonth');
  readonly filters = signal<PerformanceFilters>({ period: 'thisMonth' });
  readonly canNavigate = computed(() => !['30days', '90days'].includes(this.selectedPeriod()));
  readonly rangeLabel = computed(() => {
    const { from, to } = this.filters();
    return from && to ? `${from.slice(0, 10)} – ${to.slice(0, 10)} (UTC)` : null;
  });
  readonly periods: readonly { value: PeriodOption; label: string }[] = [
    { value: 'thisMonth', label: 'THIS_MONTH' },
    { value: 'thisWeek', label: 'THIS_WEEK' },
    { value: 'thisYear', label: 'THIS_YEAR' },
    { value: 'today', label: 'TODAY' },
    { value: '30days', label: 'LAST_30_DAYS' },
    { value: '90days', label: 'LAST_90_DAYS' },
  ];
  readonly ratingKey = computed(() => {
    switch (this.detail()?.periodRatingLabel) {
      case 'Needs Improvement': return 'EMPLOYEE_DETAIL.PERFORMANCE.NEEDS_IMPROVEMENT';
      case 'Satisfactory': return 'EMPLOYEE_DETAIL.PERFORMANCE.SATISFACTORY';
      case 'Good Performance': return 'EMPLOYEE_DETAIL.PERFORMANCE.GOOD_PERFORMANCE';
      case 'Excellent': return 'EMPLOYEE_DETAIL.PERFORMANCE.EXCELLENT';
      default: return 'EMPLOYEE_DETAIL.NOT_AVAILABLE';
    }
  });

  constructor() {
    effect(() => {
      const employeeId = this.employeeId();
      const metricId = this.metricId();
      const filters = this.filters();
      untracked(() => {
        if (employeeId !== null && metricId !== null) {
          this.service.loadPerformanceMetricDetail(employeeId, metricId, filters);
        } else {
          this.service.clearPerformanceMetricDetail();
        }
      });
    });
  }

  retry(): void {
    const employeeId = this.employeeId();
    const metricId = this.metricId();
    if (employeeId !== null && metricId !== null) {
      this.service.loadPerformanceMetricDetail(employeeId, metricId, this.filters());
    }
  }

  selectPeriod(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    const option = this.periods.find((period) => period.value === value);
    if (!option) return;
    this.selectedPeriod.set(option.value);
    this.filters.set(option.value === '30days' ? { days: 30 }
      : option.value === '90days' ? { days: 90 } : { period: option.value });
  }

  navigatePeriod(direction: -1 | 1): void {
    if (!this.canNavigate()) return;
    const period = this.selectedPeriod() as CalendarPeriod;
    const from = this.filters().from;
    const date = from ? new Date(from) : new Date();
    const start = new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate()));
    if (period === 'thisMonth') start.setUTCDate(1);
    if (period === 'thisYear') start.setUTCMonth(0, 1);
    if (period === 'thisWeek') start.setUTCDate(start.getUTCDate() - (start.getUTCDay() + 6) % 7);
    const nextStart = this.shiftPeriod(start, period, direction);
    const end = new Date(this.shiftPeriod(nextStart, period, 1).getTime() - 1);
    this.filters.set({ from: nextStart.toISOString(), to: end.toISOString() });
  }

  private shiftPeriod(start: Date, period: CalendarPeriod, direction: number): Date {
    const result = new Date(start);
    if (period === 'thisMonth') result.setUTCMonth(result.getUTCMonth() + direction);
    else if (period === 'thisYear') result.setUTCFullYear(result.getUTCFullYear() + direction);
    else result.setUTCDate(result.getUTCDate() + direction * (period === 'thisWeek' ? 7 : 1));
    return result;
  }
}
