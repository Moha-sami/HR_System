import {
  AfterViewInit,
  Component,
  TemplateRef,
  ViewChild,
  computed,
  inject,
  signal,
  type OnDestroy,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { ButtonComponent } from '@app/shared/components/button/button.component';
import { Pagination } from '@app/shared/components/pagination/pagination';
import {
  CellContext,
  ColumnDef,
  TableComponent,
} from '@app/shared/components/table/table.component';
import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { ModalBodyComponent } from '@app/shared/components/modal/modal-body.component';
import type {
  RewardListDto,
  RewardListFilter,
  RewardListStatusFilter,
  RewardSortBy,
} from '../../models/reward.models';
import { RewardService } from '../../services/reward.service';

const SORT_MAP: Record<string, RewardSortBy> = {
  name: 'name',
  points: 'points',
  monetaryValue: 'price',
  redemptionCount: 'redemptioncount',
  stockRatio: 'name',
};

@Component({
  selector: 'app-reward-list',
  standalone: true,
  imports: [
    FormsModule,
    TranslatePipe,
    ButtonComponent,
    Pagination,
    TableComponent,
    ModalComponent,
    ModalBodyComponent,
  ],
  templateUrl: './reward-list.component.html',
  styleUrl: './reward-list.component.css',
})
export class RewardListComponent implements AfterViewInit, OnDestroy {
  private readonly rewardService = inject(RewardService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);
  private readonly destroy$ = new Subject<void>();
  private readonly searchSubject = new Subject<string>();
  private loadRequestId = 0;

  @ViewChild('priceTemplate') priceTemplate!: TemplateRef<CellContext>;
  @ViewChild('statusTemplate') statusTemplate!: TemplateRef<CellContext>;
  @ViewChild('actionsTemplate') actionsTemplate!: TemplateRef<CellContext>;

  readonly rewards = signal<RewardListDto[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly loadError = signal(false);
  readonly searchTerm = signal('');
  readonly selectedStatus = signal<RewardListStatusFilter>('');
  readonly customDate = signal('');
  readonly sortBy = signal<RewardSortBy>('name');
  readonly sortDescending = signal(false);
  readonly currentPage = signal(1);
  readonly pageSize = 10;

  readonly showDeleteModal = signal(false);
  readonly showSuccessModal = signal(false);
  readonly deletingReward = signal<RewardListDto | null>(null);
  readonly isDeleting = signal(false);
  readonly deleteError = signal<string | null>(null);

  readonly cellTemplates = signal<Map<string, TemplateRef<CellContext>>>(new Map());

  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / this.pageSize)),
  );

  readonly columns = computed<ColumnDef[]>(() => [
    {
      key: 'name',
      label: this.translate.instant('REWARD_MANAGEMENT.TABLE.NAME'),
      sortable: true,
    },
    {
      key: 'category',
      label: this.translate.instant('REWARD_MANAGEMENT.TABLE.CATEGORY'),
    },
    {
      key: 'points',
      label: this.translate.instant('REWARD_MANAGEMENT.TABLE.POINTS'),
      sortable: true,
    },
    {
      key: 'monetaryValue',
      label: this.translate.instant('REWARD_MANAGEMENT.TABLE.PRICE'),
      sortable: true,
      template: 'priceTemplate',
    },
    {
      key: 'stockRatio',
      label: this.translate.instant('REWARD_MANAGEMENT.TABLE.STOCK'),
    },
    {
      key: 'redemptionCount',
      label: this.translate.instant('REWARD_MANAGEMENT.TABLE.REDEMPTION_COUNT'),
      sortable: true,
    },
    {
      key: 'isActive',
      label: this.translate.instant('REWARD_MANAGEMENT.TABLE.STATUS'),
      template: 'statusTemplate',
    },
    {
      key: 'actions',
      label: this.translate.instant('REWARD_MANAGEMENT.TABLE.ACTIONS'),
      align: 'center',
      width: '140px',
      template: 'actionsTemplate',
    },
  ]);

  constructor() {
    this.searchSubject
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe((search) => {
        this.searchTerm.set(search);
        this.currentPage.set(1);
        this.loadRewards();
      });

    this.loadRewards();
  }

  ngAfterViewInit(): void {
    this.cellTemplates.set(
      new Map([
        ['priceTemplate', this.priceTemplate],
        ['statusTemplate', this.statusTemplate],
        ['actionsTemplate', this.actionsTemplate],
      ]),
    );
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadRewards(): void {
    const requestId = ++this.loadRequestId;
    this.loading.set(true);
    this.loadError.set(false);

    this.rewardService.getRewards(this.currentFilter()).subscribe({
      next: (response) => {
        if (requestId !== this.loadRequestId) {
          return;
        }
        this.rewards.set([...response.items]);
        this.totalCount.set(response.totalCount);
        this.loading.set(false);
      },
      error: () => {
        if (requestId !== this.loadRequestId) {
          return;
        }
        this.rewards.set([]);
        this.totalCount.set(0);
        this.loadError.set(true);
        this.loading.set(false);
      },
    });
  }

  onSearch(event: Event): void {
    this.searchSubject.next((event.target as HTMLInputElement).value);
  }

  onStatusChange(value: string): void {
    this.selectedStatus.set(value as RewardListStatusFilter);
    this.currentPage.set(1);
    this.loadRewards();
  }

  onDateChange(value: string): void {
    this.customDate.set(value);
    this.currentPage.set(1);
    this.loadRewards();
  }

  onPageChanged(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.currentPage()) {
      return;
    }
    this.currentPage.set(page);
    this.loadRewards();
  }

  onSort(event: { column: string; direction: 'asc' | 'desc' }): void {
    this.sortBy.set(SORT_MAP[event.column] ?? 'name');
    this.sortDescending.set(event.direction === 'desc');
    this.currentPage.set(1);
    this.loadRewards();
  }

  onSortToggle(): void {
    this.onSort({ column: 'name', direction: this.sortDescending() ? 'asc' : 'desc' });
  }

  onExport(): void {
    this.rewardService
      .getRewards({
        ...this.currentFilter(),
        page: 1,
        pageSize: 100,
      })
      .subscribe({
        next: (response) => this.downloadCsv([...response.items]),
      });
  }

  navigateToCreate(): void {
    this.router.navigate(['/rewards/create']);
  }

  editReward(reward: RewardListDto): void {
    this.router.navigate(['/rewards/edit', reward.id]);
  }

  viewReward(reward: RewardListDto): void {
    this.router.navigate(['/rewards/details', reward.id]);
  }

  openDeleteModal(reward: RewardListDto): void {
    this.deletingReward.set(reward);
    this.deleteError.set(null);
    this.showDeleteModal.set(true);
  }

  closeDeleteModal(): void {
    if (this.isDeleting()) {
      return;
    }
    this.showDeleteModal.set(false);
    this.deletingReward.set(null);
  }

  confirmDelete(): void {
    const reward = this.deletingReward();
    if (!reward || this.isDeleting()) {
      return;
    }

    this.isDeleting.set(true);
    this.rewardService.deleteReward(reward.id).subscribe({
      next: () => {
        this.isDeleting.set(false);
        this.showDeleteModal.set(false);
        this.deletingReward.set(null);
        this.showSuccessModal.set(true);
      },
      error: () => {
        this.isDeleting.set(false);
        this.deleteError.set(this.translate.instant('REWARD_MANAGEMENT.DELETE_ERROR'));
      },
    });
  }

  confirmSuccess(): void {
    this.showSuccessModal.set(false);
    this.loadRewards();
  }

  formatPrice(value: number): string {
    return `$${Number(value).toFixed(2)}`;
  }

  statusLabel(isActive: boolean): string {
    return this.translate.instant(
      isActive ? 'REWARD_MANAGEMENT.STATUS_ACTIVE' : 'REWARD_MANAGEMENT.STATUS_INACTIVE',
    );
  }

  private currentFilter(): RewardListFilter {
    const date = this.customDate();
    return {
      page: this.currentPage(),
      pageSize: this.pageSize,
      search: this.searchTerm() || null,
      status: this.selectedStatus() || null,
      fromDate: date ? `${date}T00:00:00.000Z` : null,
      toDate: date ? `${date}T23:59:59.999Z` : null,
      sortBy: this.sortBy(),
      sortDescending: this.sortDescending(),
    };
  }

  private downloadCsv(data: RewardListDto[]): void {
    if (!data.length) {
      return;
    }

    const headers = ['Name', 'Category', 'Points', 'Price', 'Stock', 'Redemption Count', 'Status'];
    const rows = data.map((reward) => [
      reward.name,
      reward.category,
      reward.points,
      reward.monetaryValue,
      reward.stockRatio,
      reward.redemptionCount,
      reward.isActive ? 'Active' : 'Inactive',
    ]);

    const csvContent = [
      headers.join(','),
      ...rows.map((row) =>
        row.map((value) => `"${String(value).replace(/"/g, '""')}"`).join(','),
      ),
    ].join('\n');

    const blob = new Blob(['\uFEFF' + csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `rewards-${new Date().toISOString().split('T')[0]}.csv`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }
}
