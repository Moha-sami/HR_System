import {
  Component,
  computed,
  inject,
  signal,
  type AfterViewInit,
  type OnDestroy,
  type OnInit,
  type TemplateRef,
  ViewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Subject, debounceTime, takeUntil } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ShiftTemplateService } from '../../data-access/services/shift-template.service';
import type {
  ShiftTemplateFilter,
  ShiftTemplateListItem,
} from '../../data-access/models/shift-template.models';
import {
  TableComponent,
  type CellContext,
  type ColumnDef,
} from '@app/shared/components/table/table.component';
import { Pagination } from '@app/shared/components/pagination/pagination';
import { ButtonComponent } from '@app/shared/components/button/button.component';
import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { ModalBodyComponent } from '@app/shared/components/modal/modal-body.component';

type SortField = 'nameSort' | 'creationSort' | 'updatedSort' | 'numberOfAssignedSort';

const COLUMN_TO_SORT_FIELD: Record<string, SortField> = {
  name: 'nameSort',
  creationDate: 'creationSort',
  lastUpdated: 'updatedSort',
  numberOfAssignedSites: 'numberOfAssignedSort',
};

/**
 * Ticket #326: server-side searchable / sortable / paginated template list.
 * Edit navigates to the editor route; duplicate copies + refreshes with a
 * success modal; delete confirms via small modal then shows success feedback.
 * Export is visual-only (no handler) until a later ticket wires it.
 */
@Component({
  selector: 'app-shift-template-list',
  standalone: true,
  imports: [
    CommonModule,
    TranslatePipe,
    TableComponent,
    Pagination,
    ButtonComponent,
    ModalComponent,
    ModalBodyComponent,
  ],
  templateUrl: './shift-template-list.component.html',
})
export class ShiftTemplateListComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly templateService = inject(ShiftTemplateService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);
  private readonly destroy$ = new Subject<void>();

  @ViewChild('actionsTemplate') actionsTemplate!: TemplateRef<CellContext>;

  readonly templates = signal<ShiftTemplateListItem[]>([]);
  readonly totalCount = signal(0);
  readonly currentPage = signal(1);
  readonly pageSize = 10;
  readonly searchTerm = signal('');
  readonly sortField = signal<SortField | null>(null);
  readonly sortDirection = signal<'asc' | 'desc'>('asc');
  readonly loading = signal(false);
  readonly loadError = signal(false);
  readonly actionError = signal<string | null>(null);

  private readonly search$ = new Subject<string>();

  // ── Delete modal state ──────────────────────────────────────────────────
  readonly showDeleteModal = signal(false);
  readonly deletingTemplate = signal<ShiftTemplateListItem | null>(null);
  readonly isDeleting = signal(false);
  readonly deleteError = signal<string | null>(null);

  // ── Shared success modal state ──────────────────────────────────────────
  readonly showSuccessModal = signal(false);
  readonly successMessage = signal('');

  readonly duplicatingId = signal<number | null>(null);

  readonly cellTemplates = signal<Map<string, TemplateRef<CellContext>>>(new Map());

  readonly columns = computed<ColumnDef[]>(() => [
    {
      key: 'name',
      label: this.translate.instant('SHIFT_TEMPLATES.TABLE.NAME'),
    },
    {
      key: 'creationDate',
      label: this.translate.instant('SHIFT_TEMPLATES.TABLE.CREATION_DATE'),
      sortable: true,
    },
    {
      key: 'lastUpdated',
      label: this.translate.instant('SHIFT_TEMPLATES.TABLE.LAST_UPDATED'),
      sortable: true,
    },
    {
      key: 'numberOfAssignedSites',
      label: this.translate.instant('SHIFT_TEMPLATES.TABLE.ASSIGNED_SITES'),
      sortable: true,
      align: 'center',
    },
    {
      key: 'actions',
      label: this.translate.instant('SHIFT_TEMPLATES.TABLE.ACTIONS'),
      align: 'center',
      template: 'actionsTemplate',
      width: '140px',
    },
  ]);

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));

  constructor() {
    this.search$
      .pipe(debounceTime(300), takeUntilDestroyed())
      .subscribe((term) => {
        this.searchTerm.set(term);
        this.currentPage.set(1);
        this.loadTemplates();
      });
  }

  ngOnInit(): void {
    this.loadTemplates();
  }

  ngAfterViewInit(): void {
    this.cellTemplates.set(new Map([['actionsTemplate', this.actionsTemplate]]));
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadTemplates(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.templateService
      .getTemplates(this.buildFilter())
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.templates.set(res.items);
          this.totalCount.set(res.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.loadError.set(true);
          this.loading.set(false);
        },
      });
  }

  private buildFilter(): ShiftTemplateFilter {
    const filter: ShiftTemplateFilter = {
      searchTerm: this.searchTerm().trim() || undefined,
      pageNumber: this.currentPage(),
      pageSize: this.pageSize,
    };
    const field = this.sortField();
    if (field) filter[field] = this.sortDirection();
    return filter;
  }

  onSearch(event: Event): void {
    this.search$.next((event.target as HTMLInputElement).value);
  }

  onSort(event: { column: string; direction: 'asc' | 'desc' }): void {
    const field = COLUMN_TO_SORT_FIELD[event.column];
    if (!field) return;
    this.sortField.set(field);
    this.sortDirection.set(event.direction);
    this.currentPage.set(1);
    this.loadTemplates();
  }

  onPageChanged(page: number): void {
    this.currentPage.set(page);
    this.loadTemplates();
  }

  navigateToCreate(): void {
    this.router.navigate(['/scheduling/shift-templates/new']);
  }

  editTemplate(template: ShiftTemplateListItem): void {
    this.router.navigate(['/scheduling/shift-templates', template.id, 'edit']);
  }

  duplicateTemplate(template: ShiftTemplateListItem): void {
    if (this.duplicatingId() !== null) return;
    this.duplicatingId.set(template.id);
    this.actionError.set(null);
    this.templateService
      .duplicateTemplate(template.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.duplicatingId.set(null);
          this.successMessage.set(this.translate.instant('SHIFT_TEMPLATES.DUPLICATE_SUCCESS'));
          this.showSuccessModal.set(true);
        },
        error: () => {
          this.duplicatingId.set(null);
          this.actionError.set(this.translate.instant('SHIFT_TEMPLATES.DUPLICATE_ERROR'));
        },
      });
  }

  openDeleteModal(template: ShiftTemplateListItem): void {
    this.deletingTemplate.set(template);
    this.deleteError.set(null);
    this.showDeleteModal.set(true);
  }

  closeDeleteModal(): void {
    if (this.isDeleting()) return;
    this.showDeleteModal.set(false);
    this.deletingTemplate.set(null);
    this.deleteError.set(null);
  }

  confirmDelete(): void {
    const template = this.deletingTemplate();
    if (!template || this.isDeleting()) return;
    this.isDeleting.set(true);
    this.deleteError.set(null);
    this.templateService
      .deleteTemplate(template.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.isDeleting.set(false);
          this.showDeleteModal.set(false);
          this.successMessage.set(this.translate.instant('SHIFT_TEMPLATES.DELETE_SUCCESS'));
          this.showSuccessModal.set(true);
        },
        error: () => {
          this.isDeleting.set(false);
          this.deleteError.set(this.translate.instant('SHIFT_TEMPLATES.DELETE_ERROR'));
        },
      });
  }

  confirmSuccess(): void {
    this.showSuccessModal.set(false);
    this.deletingTemplate.set(null);
    this.loadTemplates();
  }
}
