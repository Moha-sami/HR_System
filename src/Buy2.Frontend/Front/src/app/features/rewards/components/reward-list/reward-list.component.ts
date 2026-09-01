import {
  AfterViewInit,
  Component,
  TemplateRef,
  ViewChild,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { ButtonComponent } from '@app/shared/components/button/button.component';
import { Pagination } from '@app/shared/components/pagination/pagination';
import {
  CellContext,
  ColumnDef,
  TableComponent,
} from '@app/shared/components/table/table.component';
import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { ModalBodyComponent } from '@app/shared/components/modal/modal-body.component';
import type { RewardListRow, RewardStatus } from '../../models/reward.models';
import { RewardService } from '../../services/reward.service';

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
export class RewardListComponent implements AfterViewInit {
  private readonly rewardService = inject(RewardService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);

  @ViewChild('imageTemplate') imageTemplate!: TemplateRef<CellContext>;
  @ViewChild('priceTemplate') priceTemplate!: TemplateRef<CellContext>;
  @ViewChild('costTemplate') costTemplate!: TemplateRef<CellContext>;
  @ViewChild('actionsTemplate') actionsTemplate!: TemplateRef<CellContext>;

  readonly rewards = signal<RewardListRow[]>([]);
  readonly loading = signal(false);
  readonly loadError = signal(false);
  readonly searchTerm = signal('');
  readonly selectedStatus = signal<RewardStatus | ''>('');
  readonly customDate = signal('');
  readonly currentPage = signal(1);
  readonly pageSize = 5;

  readonly showDeleteModal = signal(false);
  readonly showSuccessModal = signal(false);
  readonly deletingReward = signal<RewardListRow | null>(null);
  readonly isDeleting = signal(false);
  readonly deleteError = signal<string | null>(null);

  readonly cellTemplates = signal<Map<string, TemplateRef<CellContext>>>(new Map());

  readonly columns = computed<ColumnDef[]>(() => [
    {
      key: 'imageUrl',
      label: this.translate.instant('REWARD_MANAGEMENT.TABLE.IMAGE'),
      width: '80px',
      align: 'center',
      template: 'imageTemplate',
    },
    {
      key: 'name',
      label: this.translate.instant('REWARD_MANAGEMENT.TABLE.NAME'),
    },
    {
      key: 'pointsValue',
      label: this.translate.instant('REWARD_MANAGEMENT.TABLE.POINTS'),
      sortable: true,
    },
    {
      key: 'price',
      label: this.translate.instant('REWARD_MANAGEMENT.TABLE.PRICE'),
      sortable: true,
      template: 'priceTemplate',
    },
    {
      key: 'cost',
      label: this.translate.instant('REWARD_MANAGEMENT.TABLE.COST'),
      sortable: true,
      template: 'costTemplate',
    },
    {
      key: 'redemptionCount',
      label: this.translate.instant('REWARD_MANAGEMENT.TABLE.REDEMPTION_COUNT'),
      sortable: true,
    },
    {
      key: 'actions',
      label: this.translate.instant('REWARD_MANAGEMENT.TABLE.ACTIONS'),
      align: 'center',
      width: '140px',
      template: 'actionsTemplate',
    },
  ]);

  readonly filteredRewards = computed(() => {
    const search = this.searchTerm().toLowerCase().trim();
    const status = this.selectedStatus();
    const date = this.customDate();

    return this.rewards().filter((reward) => {
      const matchesSearch =
        !search ||
        reward.name.toLowerCase().includes(search) ||
        reward.category.toLowerCase().includes(search);
      const matchesStatus = !status || reward.status === status;
      const matchesDate = !date || reward.createdAt.slice(0, 10) === date;
      return matchesSearch && matchesStatus && matchesDate;
    });
  });

  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.filteredRewards().length / this.pageSize)),
  );

  readonly displayedRewards = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.filteredRewards().slice(start, start + this.pageSize);
  });

  constructor() {
    this.loadRewards();
  }

  ngAfterViewInit(): void {
    this.cellTemplates.set(
      new Map([
        ['imageTemplate', this.imageTemplate],
        ['priceTemplate', this.priceTemplate],
        ['costTemplate', this.costTemplate],
        ['actionsTemplate', this.actionsTemplate],
      ]),
    );
  }

  loadRewards(): void {
    this.loading.set(true);
    this.loadError.set(false);

    forkJoin({
      items: this.rewardService.getRewards(),
      redemptions: this.rewardService.getRedemptions(),
    }).subscribe({
      next: ({ items, redemptions }) => {
        const counts = new Map<string, number>();
        for (const redemption of redemptions) {
          const key = String(redemption.rewardItemId);
          counts.set(key, (counts.get(key) ?? 0) + 1);
        }

        this.rewards.set(
          items.map((item) => ({
            ...item,
            redemptionCount: counts.get(String(item.id)) ?? 0,
          })),
        );
        this.currentPage.set(1);
        this.loading.set(false);
      },
      error: () => {
        this.loadError.set(true);
        this.loading.set(false);
      },
    });
  }

  onSearch(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value);
    this.currentPage.set(1);
  }

  onStatusChange(value: string): void {
    this.selectedStatus.set(value as RewardStatus | '');
    this.currentPage.set(1);
  }

  onDateChange(value: string): void {
    this.customDate.set(value);
    this.currentPage.set(1);
  }

  onPageChanged(page: number): void {
    this.currentPage.set(page);
  }

  onSort(event: { column: string; direction: 'asc' | 'desc' }): void {
    const sorted = [...this.rewards()].sort((a, b) => {
      const aValue = a[event.column as keyof RewardListRow];
      const bValue = b[event.column as keyof RewardListRow];

      if (typeof aValue === 'number' && typeof bValue === 'number') {
        return event.direction === 'asc' ? aValue - bValue : bValue - aValue;
      }

      const result = String(aValue ?? '').localeCompare(String(bValue ?? ''));
      return event.direction === 'asc' ? result : -result;
    });

    this.rewards.set(sorted);
    this.currentPage.set(1);
  }

  onSortToggle(): void {
    this.onSort({ column: 'name', direction: 'asc' });
  }

  onExport(): void {
    const data = this.filteredRewards();
    if (!data.length) {
      return;
    }

    const headers = ['Name', 'Points', 'Price', 'Cost', 'Redemption Count', 'Status', 'Category'];
    const rows = data.map((reward) => [
      reward.name,
      reward.pointsValue,
      reward.price,
      reward.cost,
      reward.redemptionCount,
      reward.status,
      reward.category,
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

  navigateToCreate(): void {
    this.router.navigate(['/rewards/create']);
  }

  editReward(reward: RewardListRow): void {
    this.router.navigate(['/rewards/edit', reward.id]);
  }

  viewReward(reward: RewardListRow): void {
    this.router.navigate(['/rewards/details', reward.id]);
  }

  openDeleteModal(reward: RewardListRow): void {
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

  formatCost(value: number): string {
    return `${value}$`;
  }
}
