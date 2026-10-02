import {
  Component,
  computed,
  inject,
  signal,
  type OnDestroy,
  type OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Subject, debounceTime, takeUntil } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ShiftManagementService } from '../../data-access/services/shift-management.service';
import { ShiftsLookupsService } from '../../data-access/services/shifts-lookups.service';
import type { ShiftsRegionLookup } from '../../data-access/models/shifts-lookups.models';
import type { SiteShiftOverviewCard } from '../../data-access/models/shift-management.models';
import { Pagination } from '@app/shared/components/pagination/pagination';
import { ButtonComponent } from '@app/shared/components/button/button.component';
import { CandidatePreviewComponent } from '../candidate-preview/candidate-preview.component';

const STATUS_TINT: Record<string, string> = {
  Covered: 'bg-success-50',
  Shortage: 'bg-warning-50',
  OvertimeRisk: 'bg-error-50',
  UnqualifiedAssignment: 'bg-error-50',
};

const STATUS_DOT: Record<string, string> = {
  Covered: 'bg-success-500',
  Shortage: 'bg-warning-500',
  OvertimeRisk: 'bg-error-500',
  UnqualifiedAssignment: 'bg-error-500',
};

/**
 * Ticket A: site-coverage overview. Server-side search / region / paging,
 * visual-only Export, instant smart-toggle PATCH with revert on error, card
 * click routes to the board (ticket B builds it).
 */
@Component({
  selector: 'app-shift-overview',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslatePipe,
    Pagination,
    ButtonComponent,
    CandidatePreviewComponent,
  ],
  templateUrl: './shift-overview.component.html',
})
export class ShiftOverviewComponent implements OnInit, OnDestroy {
  private readonly managementService = inject(ShiftManagementService);
  private readonly lookups = inject(ShiftsLookupsService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);
  private readonly destroy$ = new Subject<void>();

  readonly sites = signal<SiteShiftOverviewCard[]>([]);
  readonly regions = signal<ShiftsRegionLookup[]>([]);
  readonly totalCount = signal(0);
  readonly currentPage = signal(1);
  readonly pageSize = 100;
  readonly searchTerm = signal('');
  readonly regionId = signal<number | null>(null);
  readonly loading = signal(false);
  readonly loadError = signal(false);
  readonly actionError = signal<string | null>(null);
  readonly previewEmployeeId = signal<number | null>(null);

  private readonly search$ = new Subject<string>();

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));

  constructor() {
    this.search$
      .pipe(debounceTime(300), takeUntilDestroyed())
      .subscribe((term) => {
        this.searchTerm.set(term);
        this.currentPage.set(1);
        this.loadSites();
      });
  }

  ngOnInit(): void {
    this.lookups
      .getRegions()
      .pipe(takeUntil(this.destroy$))
      .subscribe({ next: (res) => this.regions.set(res), error: () => {} });
    this.loadSites();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  cardTint(status: string): string {
    return STATUS_TINT[status] ?? 'bg-neutral-50';
  }

  statusDot(status: string): string {
    return STATUS_DOT[status] ?? 'bg-neutral-400';
  }

  onSearch(event: Event): void {
    this.search$.next((event.target as HTMLInputElement).value);
  }

  onSearchInput(value: string): void {
    this.search$.next(value);
  }

  onRegionChange(value: string): void {
    this.regionId.set(value === '' ? null : +value);
    this.currentPage.set(1);
    this.loadSites();
  }

  onPageChanged(page: number): void {
    this.currentPage.set(page);
    this.loadSites();
  }

  openSite(site: SiteShiftOverviewCard): void {
    this.router.navigate(['/scheduling/shift-management', site.siteId]);
  }

  toggleAssignment(site: SiteShiftOverviewCard): void {
    this.flipSmart(site, 'isSmartAssignmentEnabled');
  }

  togglePosting(site: SiteShiftOverviewCard): void {
    this.flipSmart(site, 'isSmartPostingEnabled');
  }

  private flipSmart(site: SiteShiftOverviewCard, key: 'isSmartAssignmentEnabled' | 'isSmartPostingEnabled'): void {
    const next = !site[key];
    this.sites.update((sites) => sites.map((s) => (s.siteId === site.siteId ? { ...s, [key]: next } : s)));
    this.actionError.set(null);
    this.managementService
      .updateSmartSettings(site.siteId, {
        isSmartAssignmentEnabled:
          key === 'isSmartAssignmentEnabled' ? next : site.isSmartAssignmentEnabled,
        isSmartPostingEnabled: key === 'isSmartPostingEnabled' ? next : site.isSmartPostingEnabled,
      })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        error: () => {
          this.sites.update((sites) =>
            sites.map((s) => (s.siteId === site.siteId ? { ...s, [key]: !next } : s)),
          );
          this.actionError.set(this.translate.instant('SHIFT_MANAGEMENT.OVERVIEW.SMART_ERROR'));
        },
      });
  }

  private loadSites(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.managementService
      .getOverview({
        search: this.searchTerm().trim() || undefined,
        regionId: this.regionId() ?? undefined,
        page: this.currentPage(),
        pageSize: this.pageSize,
      })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.sites.set(res.items);
          this.totalCount.set(res.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.loadError.set(true);
          this.loading.set(false);
        },
      });
  }
}
