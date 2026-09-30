import {
  AfterViewInit,
  Component,
  OnDestroy,
  OnInit,
  TemplateRef,
  ViewChild,
  computed,
  inject,
  signal,
} from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Subject, debounceTime, distinctUntilChanged, forkJoin, takeUntil } from 'rxjs';
import {
  CellContext,
  ColumnDef,
  TableComponent,
} from '@app/shared/components/table/table.component';
import { Pagination } from '@app/shared/components/pagination/pagination';
import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { ModalBodyComponent } from '@app/shared/components/modal/modal-body.component';
import type {
  InventoryStatus,
  RewardInventoryItem,
  UploadBatchPreview,
  VoucherInventoryFilter,
} from '../../models/reward.models';
import { RewardDetailsContext } from '../../services/reward-details.context';
import { RewardService } from '../../services/reward.service';
import { isAllowedInventoryFile } from '../../utils/parse-inventory-file';

@Component({
  selector: 'app-reward-inventory',
  standalone: true,
  imports: [
    FormsModule,
    TranslatePipe,
    TableComponent,
    Pagination,
    ModalComponent,
    ModalBodyComponent,
  ],
  templateUrl: './reward-inventory.component.html',
  styleUrl: './reward-inventory.component.css',
})
export class RewardInventoryComponent implements OnInit, AfterViewInit, OnDestroy {
  readonly ctx = inject(RewardDetailsContext);
  private readonly rewardService = inject(RewardService);
  private readonly translate = inject(TranslateService);
  private readonly destroy$ = new Subject<void>();
  private readonly search$ = new Subject<string>();

  @ViewChild('checkTemplate') checkTemplate!: TemplateRef<CellContext>;
  @ViewChild('statusTemplate') statusTemplate!: TemplateRef<CellContext>;
  @ViewChild('createdTemplate') createdTemplate!: TemplateRef<CellContext>;

  readonly search = signal('');
  readonly statusFilter = signal<InventoryStatus | ''>('');
  readonly createdDate = signal('');
  readonly page = signal(1);
  readonly pageSize = 10;
  readonly rows = signal<RewardInventoryItem[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly selectedIds = signal<Set<string>>(new Set());
  readonly cellTemplates = signal<Map<string, TemplateRef<CellContext>>>(new Map());

  readonly showTypeError = signal(false);
  readonly showUploadPreview = signal(false);
  readonly showDeleteConfirm = signal(false);
  readonly showDeleteSuccess = signal(false);
  readonly batches = signal<UploadBatchPreview[]>([]);
  readonly isUploading = signal(false);
  readonly isDeleting = signal(false);
  readonly uploadError = signal<string | null>(null);
  readonly deleteError = signal<string | null>(null);

  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / this.pageSize)),
  );

  readonly columns = computed<ColumnDef[]>(() => [
    { key: 'select', label: '', width: '48px', align: 'center', template: 'checkTemplate' },
    { key: 'batchId', label: this.translate.instant('REWARD_MANAGEMENT.COL_BATCH') },
    {
      key: 'createdAt',
      label: this.translate.instant('REWARD_MANAGEMENT.COL_CREATED'),
      template: 'createdTemplate',
    },
    { key: 'voucherCode', label: this.translate.instant('REWARD_MANAGEMENT.COL_CODE') },
    {
      key: 'status',
      label: this.translate.instant('REWARD_MANAGEMENT.COL_STATUS'),
      template: 'statusTemplate',
    },
  ]);

  readonly allFilteredSelected = computed(() => {
    const rows = this.rows();
    if (!rows.length) {
      return false;
    }
    const selected = this.selectedIds();
    return rows.every((row) => selected.has(row.id));
  });

  ngOnInit(): void {
    this.search$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => {
        this.page.set(1);
        this.loadInventory();
      });
    this.loadInventory();
  }

  ngAfterViewInit(): void {
    this.cellTemplates.set(
      new Map([
        ['checkTemplate', this.checkTemplate],
        ['statusTemplate', this.statusTemplate],
        ['createdTemplate', this.createdTemplate],
      ]),
    );
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSearch(value: string): void {
    this.search.set(value);
    this.search$.next(value);
  }

  onStatusChange(value: InventoryStatus | ''): void {
    this.statusFilter.set(value);
    this.page.set(1);
    this.loadInventory();
  }

  onDateChange(value: string): void {
    this.createdDate.set(value);
    this.page.set(1);
    this.loadInventory();
  }

  onPageChanged(page: number): void {
    this.page.set(page);
    this.loadInventory();
  }

  toggleRow(id: string, checked: boolean): void {
    const next = new Set(this.selectedIds());
    if (checked) {
      next.add(id);
    } else {
      next.delete(id);
    }
    this.selectedIds.set(next);
  }

  toggleAll(checked: boolean): void {
    if (!checked) {
      this.selectedIds.set(new Set());
      return;
    }
    this.selectedIds.set(new Set(this.rows().map((row) => row.id)));
  }

  isSelected(id: string): boolean {
    return this.selectedIds().has(id);
  }

  statusLabel(status: InventoryStatus): string {
    if (status === 'Available') {
      return this.translate.instant('REWARD_MANAGEMENT.STATUS_AVAILABLE');
    }
    if (status === 'Redeemed') {
      return this.translate.instant('REWARD_MANAGEMENT.STATUS_REDEEMED');
    }
    return this.translate.instant('REWARD_MANAGEMENT.STATUS_EXPIRED');
  }

  triggerFilePicker(): void {
    document.getElementById('inventory-file-input')?.click();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    input.value = '';
    if (!files.length) {
      return;
    }

    if (files.some((file) => !isAllowedInventoryFile(file.name))) {
      this.showTypeError.set(true);
      return;
    }

    this.previewFiles(files);
  }

  removeBatch(clientId: string): void {
    this.batches.set(this.batches().filter((batch) => batch.clientId !== clientId));
  }

  toggleBatch(clientId: string, checked: boolean): void {
    this.batches.set(
      this.batches().map((batch) =>
        batch.clientId === clientId ? { ...batch, selected: checked } : batch,
      ),
    );
  }

  closeTypeError(): void {
    this.showTypeError.set(false);
  }

  tryAgainUpload(): void {
    this.showTypeError.set(false);
    this.triggerFilePicker();
  }

  closeUploadPreview(): void {
    this.showUploadPreview.set(false);
    this.batches.set([]);
    this.uploadError.set(null);
  }

  submitUpload(): void {
    const rewardId = this.ctx.rewardId();
    const selected = this.batches().filter((batch) => batch.selected && !batch.error);
    if (!rewardId || !selected.length || this.isUploading()) {
      return;
    }

    this.isUploading.set(true);
    this.uploadError.set(null);
    forkJoin(
      selected.map((batch) =>
        this.rewardService.confirmInventoryUpload(rewardId, batch.file, batch.batchId),
      ),
    ).subscribe({
      next: () => {
        this.isUploading.set(false);
        this.closeUploadPreview();
        this.loadInventory();
        this.ctx.reloadProfile();
      },
      error: (error: HttpErrorResponse) => {
        this.isUploading.set(false);
        this.uploadError.set(apiMessage(error, this.translate.instant('REWARD_MANAGEMENT.UPLOAD_SAVE_ERROR')));
      },
    });
  }

  openDeleteConfirm(): void {
    if (!this.selectedIds().size) {
      return;
    }
    this.deleteError.set(null);
    this.showDeleteConfirm.set(true);
  }

  closeDeleteConfirm(): void {
    if (this.isDeleting()) {
      return;
    }
    this.showDeleteConfirm.set(false);
  }

  confirmDelete(): void {
    const rewardId = this.ctx.rewardId();
    const ids = [...this.selectedIds()].map((id) => Number(id)).filter((id) => Number.isFinite(id));
    if (!rewardId || !ids.length || this.isDeleting()) {
      return;
    }
    this.isDeleting.set(true);
    this.deleteError.set(null);
    this.rewardService.deleteInventoryBatch(rewardId, ids).subscribe({
      next: (result) => {
        this.isDeleting.set(false);
        if (result.deletedCount > 0) {
          this.showDeleteConfirm.set(false);
          this.selectedIds.set(new Set());
          this.loadInventory();
          this.ctx.reloadProfile();
          this.showDeleteSuccess.set(true);
        } else {
          this.deleteError.set(result.message);
        }
      },
      error: (error: HttpErrorResponse) => {
        this.isDeleting.set(false);
        this.deleteError.set(apiMessage(error, this.translate.instant('REWARD_MANAGEMENT.UPLOAD_SAVE_ERROR')));
      },
    });
  }

  closeDeleteSuccess(): void {
    this.showDeleteSuccess.set(false);
  }

  formatCreated(iso: string): string {
    const date = new Date(iso);
    if (Number.isNaN(date.getTime())) {
      return iso;
    }
    const dd = String(date.getDate()).padStart(2, '0');
    const mm = String(date.getMonth() + 1).padStart(2, '0');
    const yyyy = date.getFullYear();
    let hours = date.getHours();
    const minutes = String(date.getMinutes()).padStart(2, '0');
    const suffix = hours >= 12 ? 'pm' : 'am';
    hours = hours % 12 || 12;
    return `${dd}-${mm}-${yyyy} ${String(hours).padStart(2, '0')}:${minutes} ${suffix}`;
  }

  private loadInventory(): void {
    const rewardId = this.ctx.rewardId();
    if (!rewardId) {
      return;
    }
    this.loading.set(true);
    this.rewardService.getInventory(rewardId, this.currentFilter()).subscribe({
      next: (page) => {
        this.rows.set(page.items);
        this.totalCount.set(page.totalCount);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  private currentFilter(): VoucherInventoryFilter {
    const search = this.search().trim();
    const date = this.createdDate();
    const numericBatch = /^\d+$/.test(search) ? Number(search) : null;
    return {
      page: this.page(),
      pageSize: this.pageSize,
      voucherCode: search || null,
      batchId: numericBatch,
      status: this.statusFilter() || null,
      dateFrom: date ? `${date}T00:00:00.000Z` : null,
      dateTo: date ? `${date}T23:59:59.999Z` : null,
    };
  }

  private previewFiles(files: File[]): void {
    const rewardId = this.ctx.rewardId();
    if (!rewardId) {
      return;
    }
    this.isUploading.set(true);
    this.uploadError.set(null);
    forkJoin(files.map((file) => this.rewardService.previewInventoryUpload(rewardId, file))).subscribe({
      next: (responses) => {
        const next = [...this.batches()];
        responses.forEach((response, index) => {
          const file = files[index];
          const preview = response.preview;
          next.push({
            clientId: `${file.name}-${Date.now()}-${index}`,
            file,
            fileName: file.name.replace(/\.[^.]+$/, ''),
            batchId: String(preview?.batchId ?? ''),
            totalFound: preview?.totalFound ?? 0,
            validCount: preview?.validCount ?? 0,
            duplicateCount: (preview?.duplicateInFileCount ?? 0) + (preview?.duplicateInDbCount ?? 0),
            selected: (preview?.validCount ?? 0) > 0,
            error: preview ? null : this.translate.instant('REWARD_MANAGEMENT.UPLOAD_SAVE_ERROR'),
          });
        });
        this.batches.set(next);
        this.isUploading.set(false);
        this.showUploadPreview.set(true);
      },
      error: (error: HttpErrorResponse) => {
        this.isUploading.set(false);
        this.showTypeError.set(false);
        this.showUploadPreview.set(true);
        this.uploadError.set(apiMessage(error, this.translate.instant('REWARD_MANAGEMENT.UPLOAD_SAVE_ERROR')));
      },
    });
  }
}

function apiMessage(error: HttpErrorResponse, fallback: string): string {
  const message = error.error?.message;
  return typeof message === 'string' && message.trim() ? message : fallback;
}
