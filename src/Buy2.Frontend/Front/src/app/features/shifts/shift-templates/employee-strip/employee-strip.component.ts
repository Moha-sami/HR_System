import {
  Component,
  computed,
  DestroyRef,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { CdkDrag, CdkDropList } from '@angular/cdk/drag-drop';
import { Subject, debounceTime } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ShiftsLookupsService } from '../../data-access/services/shifts-lookups.service';
import type { ShiftCandidateEmployee } from '../../data-access/models/shifts-lookups.models';

export const STRIP_PAGE_SIZE = 20;

/** Rating tiers mirror the backend RatingTiers contract. */
export const RATING_TIERS = ['4.5+', '4.0-4.49', '3.0-3.99', '2.0-2.99', 'below 2.0', 'unrated'];

/**
 * Ticket #329: horizontal employee strip scoped to the selected sites.
 * Paged server-side (scroll loads more); every card is a drag source for
 * timeline-bar drops. Emits each loaded page so the editor can feed its
 * picker from the same fetch.
 */
@Component({
  selector: 'app-employee-strip',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe, CdkDrag, CdkDropList],
  templateUrl: './employee-strip.component.html',
})
export class EmployeeStripComponent {
  private readonly lookups = inject(ShiftsLookupsService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly search$ = new Subject<string>();

  readonly siteIds = input.required<number[]>();
  readonly assignedIds = input<number[]>([]);

  readonly pageLoaded = output<{ page: number; items: ShiftCandidateEmployee[] }>();

  readonly items = signal<ShiftCandidateEmployee[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly loading = signal(false);
  readonly loadError = signal(false);
  readonly search = signal('');
  readonly selectedTiers = signal<string[]>([]);
  readonly preferredOnly = signal(false);

  readonly tiers = RATING_TIERS;
  readonly hasMore = computed(() => this.items().length < this.totalCount());

  constructor() {
    this.search$.pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef)).subscribe(() => this.reload());
    effect(() => {
      this.siteIds();
      untracked(() => this.reload());
    });
  }

  onSearchInput(value: string): void {
    this.search.set(value);
    this.search$.next(value);
  }

  toggleTier(tier: string): void {
    this.selectedTiers.update((tiers) =>
      tiers.includes(tier) ? tiers.filter((t) => t !== tier) : [...tiers, tier],
    );
    this.reload();
  }

  togglePreferred(): void {
    this.preferredOnly.update((v) => !v);
    this.reload();
  }

  isAssigned(id: number): boolean {
    return this.assignedIds().includes(id);
  }

  riskBadgeClass(token: string): string {
    switch (token) {
      case 'TopPerformer':
        return 'bg-success-50 text-success-700';
      case 'OvertimeRisk':
        return 'bg-warning-50 text-warning-700';
      case 'Warning':
        return 'bg-error-50 text-error-600';
      default:
        return 'bg-neutral-100 text-neutral-500';
    }
  }

  riskLabelKey(token: string): string {
    switch (token) {
      case 'TopPerformer':
        return 'SHIFT_TEMPLATES.STRIP.RISK_TOP_PERFORMER';
      case 'OvertimeRisk':
        return 'SHIFT_TEMPLATES.STRIP.RISK_OVERTIME';
      case 'Warning':
        return 'SHIFT_TEMPLATES.STRIP.RISK_WARNING';
      default:
        return 'SHIFT_TEMPLATES.STRIP.RISK_NORMAL';
    }
  }

  onScroll(event: Event): void {
    const el = event.target as HTMLElement;
    if (this.loading() || !this.hasMore()) return;
    if (el.scrollLeft + el.clientWidth >= el.scrollWidth - 120) {
      this.loadPage(this.page() + 1);
    }
  }

  reload(): void {
    this.loadPage(1);
  }

  private loadPage(page: number): void {
    const siteIds = this.siteIds();
    if (siteIds.length === 0) {
      this.items.set([]);
      this.totalCount.set(0);
      this.page.set(1);
      return;
    }
    this.loading.set(true);
    this.loadError.set(false);
    this.lookups
      .getShiftEmployees({
        siteIds,
        page,
        pageSize: STRIP_PAGE_SIZE,
        search: this.search(),
        ratingTiers: this.selectedTiers(),
        isPreferredOnly: this.preferredOnly() ? true : undefined,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.items.update((items) => (page === 1 ? res.items : [...items, ...res.items]));
          this.totalCount.set(res.totalCount);
          this.page.set(page);
          this.loading.set(false);
          this.pageLoaded.emit({ page, items: res.items });
        },
        error: () => {
          this.loading.set(false);
          this.loadError.set(true);
        },
      });
  }
}
