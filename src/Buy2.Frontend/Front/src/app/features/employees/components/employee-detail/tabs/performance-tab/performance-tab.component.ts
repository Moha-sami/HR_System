import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { EmployeeDetailService } from '../../../../services/employee-detail.service';
import type { PerformanceFilters } from '../../../../models/view-employee/employee-performance';
import { LanguageService } from '../../../../../../core/services/language.service';
import type { EmployeePerformanceTaskStatus } from '../../../../models/view-employee/employee-performance-task';

type CalendarPeriod = 'thisMonth' | 'thisWeek' | 'thisYear' | 'today';
type PeriodOption = CalendarPeriod | '30days' | '90days';

@Component({
  selector: 'app-performance-tab',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './performance-tab.component.html',
})
export class PerformanceTabComponent {
  private readonly service = inject(EmployeeDetailService);
  private readonly language = inject(LanguageService);
  private readonly failedAchievementIcons = signal<ReadonlySet<string>>(new Set());

  achievementIcon(url: string | null): string | null {
    const value = url?.trim();
    if (!value || this.failedAchievementIcons().has(value)) return null;
    // Accept web images and local relative assets; reject other URL schemes.
    if (/^https?:\/\//i.test(value) || (!/^[a-z][a-z\d+.-]*:/i.test(value) && !value.startsWith('//'))) {
      return value;
    }
    return null;
  }

  onAchievementIconError(url: string): void {
    this.failedAchievementIcons.update((urls) => new Set([...urls, url.trim()]));
  }

  formatPerformanceDate(value: string): string {
    const date = new Date(value);
    if (!Number.isFinite(date.getTime())) return '—';
    return date.toLocaleDateString(this.language.currentLanguage() === 'ar' ? 'ar-EG' : 'en-US', {
      year: 'numeric', month: 'short', day: 'numeric', timeZone: 'UTC',
    });
  }

  showChartDate(index: number, count: number): boolean {
    return index === count - 1 || index % Math.max(1, Math.ceil(count / 6)) === 0;
  }
  readonly employee = this.service.detailEmployee;
  readonly overview = this.service.performanceOverview;
  readonly loading = this.service.performanceOverviewLoading;
  readonly error = this.service.performanceOverviewError;
  readonly tasks = this.service.performanceTasks;
  readonly tasksLoading = this.service.performanceTasksLoading;
  readonly tasksError = this.service.performanceTasksError;
  private readonly taskEmployeeId = computed(() => this.employee()?.id ?? null);
  readonly taskColumns = computed(() => {
    const statuses: readonly { status: EmployeePerformanceTaskStatus; label: string }[] = [
      { status: 'Todo', label: 'TODO' },
      { status: 'InProgress', label: 'IN_PROGRESS' },
      { status: 'InReview', label: 'IN_REVIEW' },
      { status: 'Completed', label: 'COMPLETED' },
      { status: 'Overdue', label: 'OVERDUE' },
    ];
    return statuses.map((column) => ({
      ...column,
      tasks: (this.tasks() ?? []).filter((task) => task.status === column.status),
    }));
  });
  readonly scoreProgress = computed(() => this.clampProgress(this.overview()?.overallWeightedScore ?? 0));
  readonly completionRate = computed(() => {
    const tasks = this.overview()?.tasksSummary;
    const rate = tasks && tasks.totalTasks > 0 ? tasks.completedCount / tasks.totalTasks * 100 : 0;
    return Number.isFinite(rate) ? rate : 0;
  });
  readonly ratingKey = computed(() => {
    switch (this.overview()?.ratingLabel) {
      case 'Needs Improvement': return 'EMPLOYEE_DETAIL.PERFORMANCE.NEEDS_IMPROVEMENT';
      case 'Satisfactory': return 'EMPLOYEE_DETAIL.PERFORMANCE.SATISFACTORY';
      case 'Good Performance': return 'EMPLOYEE_DETAIL.PERFORMANCE.GOOD_PERFORMANCE';
      case 'Excellent': return 'EMPLOYEE_DETAIL.PERFORMANCE.EXCELLENT';
      default: return 'EMPLOYEE_DETAIL.NOT_AVAILABLE';
    }
  });

  clampProgress(value: number): number {
    return Number.isFinite(value) ? Math.min(100, Math.max(0, value)) : 0;
  }

  displayPercentage(value: number): number {
    return Number.isFinite(value) ? Number(value.toFixed(2)) : 0;
  }
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

  constructor() {
    // Only employee identity drives task loading; overview filters are independent.
    effect(() => {
      const employeeId = this.taskEmployeeId();
      untracked(() => {
        if (employeeId !== null && Number.isSafeInteger(employeeId) && employeeId > 0) {
          this.service.loadEmployeePerformanceTasks(employeeId);
        } else {
          this.service.clearEmployeePerformanceTasks();
        }
      });
    });
    effect(() => {
      const employeeId = this.employee()?.id;
      const filters = this.filters();
      untracked(() => {
        if (employeeId !== undefined && Number.isSafeInteger(employeeId) && employeeId > 0) {
          this.service.loadPerformanceOverview(employeeId, filters);
        } else {
          this.service.clearPerformanceOverview();
        }
      });
    });
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
    const start = this.periodStart(from ? new Date(from) : new Date(), period);
    const nextStart = this.shiftPeriod(start, period, direction);
    const end = new Date(this.shiftPeriod(nextStart, period, 1).getTime() - 1);
    this.filters.set({ from: nextStart.toISOString(), to: end.toISOString() });
  }

  retryTasks(): void {
    const employeeId = this.taskEmployeeId();
    if (employeeId !== null && Number.isSafeInteger(employeeId) && employeeId > 0) {
      this.service.loadEmployeePerformanceTasks(employeeId);
    }
  }

  retry(): void {
    const employeeId = this.employee()?.id;
    if (employeeId !== undefined && Number.isSafeInteger(employeeId) && employeeId > 0) {
      this.service.loadPerformanceOverview(employeeId, this.filters());
    }
  }

  private periodStart(date: Date, period: CalendarPeriod): Date {
    const start = new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate()));
    if (period === 'thisMonth') start.setUTCDate(1);
    if (period === 'thisYear') start.setUTCMonth(0, 1);
    if (period === 'thisWeek') start.setUTCDate(start.getUTCDate() - (start.getUTCDay() + 6) % 7);
    return start;
  }

  private shiftPeriod(start: Date, period: CalendarPeriod, direction: number): Date {
    const result = new Date(start);
    if (period === 'thisMonth') result.setUTCMonth(result.getUTCMonth() + direction);
    else if (period === 'thisYear') result.setUTCFullYear(result.getUTCFullYear() + direction);
    else result.setUTCDate(result.getUTCDate() + direction * (period === 'thisWeek' ? 7 : 1));
    return result;
  }
}
