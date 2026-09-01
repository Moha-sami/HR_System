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
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import {
  CellContext,
  ColumnDef,
  TableComponent,
} from '@app/shared/components/table/table.component';
import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { ModalBodyComponent } from '@app/shared/components/modal/modal-body.component';
import type { CreateInventoryDto, InventoryStatus, UploadBatchPreview } from '../../models/reward.models';
import { RewardDetailsContext } from '../../services/reward-details.context';
import { RewardService } from '../../services/reward.service';
import {
  isAllowedInventoryFile,
  nextBatchId,
  parseSpreadsheetCodes,
} from '../../utils/parse-inventory-file';

@Component({
  selector: 'app-reward-inventory',
  standalone: true,
  imports: [FormsModule, TranslatePipe, TableComponent, ModalComponent, ModalBodyComponent],
  templateUrl: './reward-inventory.component.html',
  styleUrl: './reward-inventory.component.css',
})
export class RewardInventoryComponent implements AfterViewInit {
  readonly ctx = inject(RewardDetailsContext);
  private readonly rewardService = inject(RewardService);
  private readonly translate = inject(TranslateService);

  @ViewChild('checkTemplate') checkTemplate!: TemplateRef<CellContext>;
  @ViewChild('statusTemplate') statusTemplate!: TemplateRef<CellContext>;
  @ViewChild('createdTemplate') createdTemplate!: TemplateRef<CellContext>;

  readonly search = signal('');
  readonly statusFilter = signal<InventoryStatus | ''>('');
  readonly createdDate = signal('');
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

  readonly filteredRows = computed(() => {
    const search = this.search().trim().toLowerCase();
    const status = this.statusFilter();
    const date = this.createdDate();
    return this.ctx.inventory().filter((item) => {
      const matchesSearch =
        !search ||
        item.batchId.toLowerCase().includes(search) ||
        item.voucherCode.toLowerCase().includes(search);
      const matchesStatus = !status || item.status === status;
      const matchesDate = !date || item.createdAt.slice(0, 10) === date;
      return matchesSearch && matchesStatus && matchesDate;
    });
  });

  readonly allFilteredSelected = computed(() => {
    const rows = this.filteredRows();
    if (!rows.length) {
      return false;
    }
    const selected = this.selectedIds();
    return rows.every((row) => selected.has(row.id));
  });

  ngAfterViewInit(): void {
    this.cellTemplates.set(
      new Map([
        ['checkTemplate', this.checkTemplate],
        ['statusTemplate', this.statusTemplate],
        ['createdTemplate', this.createdTemplate],
      ]),
    );
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
    this.selectedIds.set(new Set(this.filteredRows().map((row) => row.id)));
  }

  isSelected(id: string): boolean {
    return this.selectedIds().has(id);
  }

  triggerFilePicker(): void {
    document.getElementById('inventory-file-input')?.click();
  }

  async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    input.value = '';
    if (!files.length) {
      return;
    }

    const invalid = files.some((file) => !isAllowedInventoryFile(file.name));
    if (invalid) {
      this.showTypeError.set(true);
      return;
    }

    await this.addFiles(files);
  }

  async addFiles(files: File[]): Promise<void> {
    const next = [...this.batches()];
    for (const file of files) {
      const codes = await parseSpreadsheetCodes(file);
      next.push({
        clientId: `${file.name}-${Date.now()}-${Math.random()}`,
        batchId: nextBatchId(),
        fileName: file.name.replace(/\.[^.]+$/, ''),
        codes,
        selected: true,
      });
    }
    this.batches.set(next);
    this.showUploadPreview.set(true);
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
    const selected = this.batches().filter((batch) => batch.selected && batch.codes.length);
    if (!rewardId || !selected.length || this.isUploading()) {
      return;
    }

    this.isUploading.set(true);
    this.uploadError.set(null);
    const now = new Date().toISOString();
    const requests = selected.flatMap((batch) =>
      batch.codes.map((code) => {
        const dto: CreateInventoryDto = {
          rewardItemId: rewardId,
          batchId: batch.batchId,
          fileName: batch.fileName,
          voucherCode: code,
          status: 'Available',
          createdAt: now,
          redeemedAt: null,
          employeeId: null,
        };
        return this.rewardService.createInventory(dto);
      }),
    );

    forkJoin(requests).subscribe({
      next: () => {
        this.isUploading.set(false);
        this.closeUploadPreview();
        this.ctx.reloadInventory();
      },
      error: () => {
        this.isUploading.set(false);
        this.uploadError.set(this.translate.instant('REWARD_MANAGEMENT.UPLOAD_SAVE_ERROR'));
      },
    });
  }

  openDeleteConfirm(): void {
    if (!this.selectedIds().size) {
      return;
    }
    this.showDeleteConfirm.set(true);
  }

  closeDeleteConfirm(): void {
    if (this.isDeleting()) {
      return;
    }
    this.showDeleteConfirm.set(false);
  }

  confirmDelete(): void {
    const ids = [...this.selectedIds()];
    if (!ids.length || this.isDeleting()) {
      return;
    }
    this.isDeleting.set(true);
    forkJoin(ids.map((id) => this.rewardService.deleteInventory(id))).subscribe({
      next: () => {
        this.isDeleting.set(false);
        this.showDeleteConfirm.set(false);
        this.selectedIds.set(new Set());
        this.showDeleteSuccess.set(true);
        this.ctx.reloadInventory();
      },
      error: () => this.isDeleting.set(false),
    });
  }

  closeDeleteSuccess(): void {
    this.showDeleteSuccess.set(false);
  }

  formatCreated(iso: string): string {
    const date = new Date(iso);
    const dd = String(date.getDate()).padStart(2, '0');
    const mm = String(date.getMonth() + 1).padStart(2, '0');
    const yyyy = date.getFullYear();
    let hours = date.getHours();
    const minutes = String(date.getMinutes()).padStart(2, '0');
    const suffix = hours >= 12 ? 'pm' : 'am';
    hours = hours % 12 || 12;
    return `${dd}-${mm}-${yyyy} ${String(hours).padStart(2, '0')}:${minutes} ${suffix}`;
  }
}
