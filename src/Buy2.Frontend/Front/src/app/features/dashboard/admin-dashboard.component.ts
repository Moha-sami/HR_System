import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ButtonComponent } from '@app/shared/components/button/button.component';
import { SiteService } from '../sites/services/site.service';
import { RequestService } from '../requests/services/request.service';
import { EmployeeService } from '../employees/services/employee.service';
import { JobService } from '../jobs/services/job.service';
import { LanguageService } from '../../core/services/language.service';
import type {
  CoverageStatus,
  DayCoverageDto,
  SiteShiftCoverageOverviewDto,
} from '../sites/models/site.models';
import type { SubmittedRequest } from '../requests/models/request';
import type { Job } from '../../core/models/job';

const STATUS_PILL: Record<CoverageStatus, string> = {
  Full: 'bg-success-50 text-success-700',
  Partial: 'bg-warning-50 text-warning-800',
  Critical: 'bg-error-50 text-error-700',
  NoShifts: 'bg-neutral-100 text-neutral-400',
};

const STATUS_LABEL_KEY: Record<CoverageStatus, string> = {
  Full: 'DASHBOARD.STATUS.FULL',
  Partial: 'DASHBOARD.STATUS.PARTIAL',
  Critical: 'DASHBOARD.STATUS.CRITICAL',
  NoShifts: 'DASHBOARD.STATUS.NO_SHIFTS',
};

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslatePipe, ButtonComponent],
  templateUrl: './admin-dashboard.component.html',
})
export class AdminDashboardComponent {
  private readonly siteService = inject(SiteService);
  private readonly requestService = inject(RequestService);
  private readonly employeeService = inject(EmployeeService);
  private readonly jobService = inject(JobService);
  private readonly language = inject(LanguageService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly error = signal(false);

  readonly coverage = signal<readonly SiteShiftCoverageOverviewDto[]>([]);
  readonly pendingCount = signal(0);
  readonly pendingRequests = signal<readonly SubmittedRequest[]>([]);
  readonly headcount = signal(0);
  readonly jobs = signal<readonly Job[]>([]);

  readonly totals = computed(() => {
    let open = 0;
    let filled = 0;
    let total = 0;
    for (const site of this.coverage()) {
      for (const day of site.days) {
        open += day.openShifts;
        filled += day.filledShifts;
        total += day.totalShifts;
      }
    }
    return { open, filled, total };
  });

  readonly jobLedger = computed(() =>
    [...this.jobs()].sort((a, b) => b.assignedEmployeesCount - a.assignedEmployeesCount),
  );

  readonly totalAssigned = computed(() =>
    this.jobLedger().reduce((sum, job) => sum + job.assignedEmployeesCount, 0),
  );

  readonly siteStatuses = computed(
    () =>
      new Map<number, CoverageStatus>(
        this.coverage().map((site) => [site.siteId, this.overallStatus(site.days)]),
      ),
  );

  readonly dayHeaders = computed(() => {
    const days = this.coverage()[0]?.days ?? [];
    const locale = this.language.currentLanguage() === 'ar' ? 'ar-EG' : 'en-GB';
    const format = new Intl.DateTimeFormat(locale, {
      weekday: 'short',
      day: 'numeric',
      month: 'short',
    });
    return days.map((day) => {
      const [year, month, date] = day.date.split('-').map(Number);
      return { date: day.date, label: format.format(new Date(year, month - 1, date)) };
    });
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(false);

    forkJoin({
      coverage: this.siteService.getShiftsOverview(),
      pending: this.requestService.getSubmittedRequests(1, 5, 'SubmittedAt', true, {
        overallStatus: 'Pending',
      }),
      headcount: this.employeeService.getEmployeesPaginated({ page: 1, pageSize: 1 }),
      jobs: this.jobService.getJobs(1, 50),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ coverage, pending, headcount, jobs }) => {
          this.coverage.set(coverage);
          this.pendingCount.set(pending.totalCount);
          this.pendingRequests.set(pending.items);
          this.headcount.set(headcount.totalCount);
          this.jobs.set(jobs.items);
          this.loading.set(false);
        },
        error: () => {
          this.error.set(true);
          this.loading.set(false);
        },
      });
  }

  overallStatus(days: readonly DayCoverageDto[]): CoverageStatus {
    if (days.length === 0 || days.every((day) => day.status === 'NoShifts')) {
      return 'NoShifts';
    }
    if (days.some((day) => day.status === 'Critical')) {
      return 'Critical';
    }
    if (days.some((day) => day.status === 'Partial')) {
      return 'Partial';
    }
    return 'Full';
  }

  pill(status: CoverageStatus): string {
    return `inline-block rounded-full px-2 py-0.5 text-xs font-medium tabular-nums ${STATUS_PILL[status]}`;
  }

  statusLabel(status: CoverageStatus): string {
    return STATUS_LABEL_KEY[status];
  }

  siteOpen(site: SiteShiftCoverageOverviewDto): number {
    return site.days.reduce((sum, day) => sum + day.openShifts, 0);
  }

  siteFilled(site: SiteShiftCoverageOverviewDto): number {
    return site.days.reduce((sum, day) => sum + day.filledShifts, 0);
  }

  siteTotal(site: SiteShiftCoverageOverviewDto): number {
    return site.days.reduce((sum, day) => sum + day.totalShifts, 0);
  }

  filledOf(day: DayCoverageDto): number[] {
    return Array.from({ length: day.filledShifts }, (_, i) => i);
  }

  openOf(day: DayCoverageDto): number[] {
    return Array.from({ length: day.openShifts }, (_, i) => i);
  }

  shareOf(job: Job): number {
    const total = this.totalAssigned();
    return total > 0 ? Math.round((job.assignedEmployeesCount / total) * 100) : 0;
  }
}
