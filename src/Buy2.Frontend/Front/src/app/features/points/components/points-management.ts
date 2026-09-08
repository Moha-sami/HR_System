import {
  Component,
  computed,
  inject,
  signal,
  type OnDestroy,
  type TemplateRef,
  viewChild,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';

import { LanguageService } from '../../../core/services/language.service';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { Pagination } from '../../../shared/components/pagination/pagination';
import {
  TableComponent,
  type CellContext,
  type ColumnDef,
} from '../../../shared/components/table/table.component';
import type {
  PointTableRow,
  PointsSortBy,
  PointsSortDirection,
  PointsTransactionType,
} from '../models/points-transaction';
import { PointsManagementService } from '../service/points-management.service';

const TYPE_OPTIONS: readonly PointsTransactionType[] = ['Add', 'Deduct', 'Earned', 'Redeemed'];
const TYPE_I18N: Record<PointsTransactionType, string> = {
  Add: 'POINTS.TYPES.ADD',
  Deduct: 'POINTS.TYPES.DEDUCT',
  Earned: 'POINTS.TYPES.EARNED',
  Redeemed: 'POINTS.TYPES.REDEEMED',
};
const SORT_MAP: Record<string, PointsSortBy> = {
  employeeName: 'EmployeeName',
  date: 'CreatedAt',
  time: 'CreatedAt',
  transactionType: 'TransactionType',
  points: 'Points',
};

@Component({
  selector: 'app-points-management',
  standalone: true,
  imports: [TableComponent, ButtonComponent, Pagination, TranslatePipe],
  templateUrl: './points-management.html',
  styleUrl: './points-management.css',
})
export class PointsManagement implements OnDestroy {
  private readonly pointsService = inject(PointsManagementService);
  private readonly translate = inject(TranslateService);
  private readonly languageService = inject(LanguageService);
  private readonly router = inject(Router);

  private readonly destroy$ = new Subject<void>();
  private readonly searchSubject = new Subject<string>();
  private loadRequestId = 0;

  private readonly pointsTemplate =
    viewChild<TemplateRef<CellContext>>('pointsTemplate');

  private readonly triggeredByTemplate =
    viewChild<TemplateRef<CellContext>>('triggeredByTemplate');

  private readonly typeTemplate =
    viewChild<TemplateRef<CellContext>>('typeTemplate');

  private readonly columnLabels = toSignal(
    this.translate.stream([
      'POINTS.TABLE.ID',
      'POINTS.TABLE.NAME',
      'POINTS.TABLE.DATE',
      'POINTS.TABLE.TIME',
      'POINTS.TABLE.TYPE',
      'POINTS.TABLE.POINTS',
      'POINTS.TABLE.TRIGGERED_BY',
      'POINTS.TABLE.COMMENTS',
    ]),
    {
      initialValue: {
        'POINTS.TABLE.ID': 'ID',
        'POINTS.TABLE.NAME': 'Name',
        'POINTS.TABLE.DATE': 'Date',
        'POINTS.TABLE.TIME': 'Time',
        'POINTS.TABLE.TYPE': 'Type',
        'POINTS.TABLE.POINTS': 'Points',
        'POINTS.TABLE.TRIGGERED_BY': 'Triggered by',
        'POINTS.TABLE.COMMENTS': 'Comments',
      },
    }
  );

  readonly transactions = signal<PointTableRow[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly loadError = signal(false);

  readonly searchTerm = signal('');
  readonly selectedMonth = signal(this.currentMonthKey());
  readonly selectedTrigger = signal('');
  readonly selectedType = signal<PointsTransactionType | ''>('');
  readonly sortBy = signal<PointsSortBy>('CreatedAt');
  readonly sortDir = signal<PointsSortDirection>('Desc');

  readonly pageSize = 10;
  readonly currentPage = signal(1);
  readonly totalPages = signal(1);

  readonly typeOptions = TYPE_OPTIONS;
  readonly triggerOptions = ['ManualAdjustment'] as const;

  readonly availableMonths = computed(() => {
    const selected = this.selectedMonth();
    const months = new Set(this.rollingMonthKeys(24));
    months.add(selected);
    months.add(this.currentMonthKey());
    return [...months].sort((first, second) => second.localeCompare(first));
  });

  readonly columns = computed<ColumnDef[]>(() => {
    const labels = this.columnLabels();

    return [
      {
        key: 'id',
        label: labels['POINTS.TABLE.ID'],
        width: '0.7fr',
      },
      {
        key: 'employeeName',
        label: labels['POINTS.TABLE.NAME'],
        width: '1.2fr',
        sortable: true,
      },
      {
        key: 'date',
        label: labels['POINTS.TABLE.DATE'],
        width: '1fr',
        sortable: true,
      },
      {
        key: 'time',
        label: labels['POINTS.TABLE.TIME'],
        width: '1fr',
        sortable: true,
      },
      {
        key: 'transactionType',
        label: labels['POINTS.TABLE.TYPE'],
        width: '0.9fr',
        sortable: true,
        template: 'type',
      },
      {
        key: 'points',
        label: labels['POINTS.TABLE.POINTS'],
        width: '0.8fr',
        sortable: true,
        template: 'points',
      },
      {
        key: 'triggeredBy',
        label: labels['POINTS.TABLE.TRIGGERED_BY'],
        width: '1.4fr',
        template: 'triggeredBy',
      },
      {
        key: 'comments',
        label: labels['POINTS.TABLE.COMMENTS'],
        width: '2fr',
      },
    ];
  });

  readonly cellTemplates = computed(() => {
    const pointsTemplate = this.pointsTemplate();
    const triggeredByTemplate = this.triggeredByTemplate();
    const typeTemplate = this.typeTemplate();
    const templates = new Map<string, TemplateRef<CellContext>>();

    if (pointsTemplate) {
      templates.set('points', pointsTemplate);
    }

    if (triggeredByTemplate) {
      templates.set('triggeredBy', triggeredByTemplate);
    }

    if (typeTemplate) {
      templates.set('type', typeTemplate);
    }

    return templates;
  });

  constructor() {
    this.searchSubject
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe((search) => {
        this.searchTerm.set(search);
        this.currentPage.set(1);
        this.loadTransactions();
      });

    this.loadTransactions();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadTransactions(): void {
    const requestId = ++this.loadRequestId;
    this.loading.set(true);
    this.loadError.set(false);

    const [year, month] = this.selectedMonth().split('-').map(Number);

    this.pointsService
      .getTransactions({
        pageNumber: this.currentPage(),
        pageSize: this.pageSize,
        searchTerm: this.searchTerm() || null,
        triggeredBy: this.selectedTrigger() || null,
        transactionType: this.selectedType() || null,
        sortBy: this.sortBy(),
        sortDir: this.sortDir(),
        month,
        year,
      })
      .subscribe({
        next: (response) => {
          if (requestId !== this.loadRequestId) {
            return;
          }

          this.transactions.set(response.items);
          this.totalCount.set(response.totalCount);
          this.totalPages.set(Math.max(1, response.totalPages));
          this.loading.set(false);
        },
        error: () => {
          if (requestId !== this.loadRequestId) {
            return;
          }

          this.transactions.set([]);
          this.totalCount.set(0);
          this.totalPages.set(1);
          this.loadError.set(true);
          this.loading.set(false);
        },
      });
  }

  updateSearch(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.searchSubject.next(input.value);
  }

  updateTrigger(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.selectedTrigger.set(select.value);
    this.currentPage.set(1);
    this.loadTransactions();
  }

  updateType(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.selectedType.set((select.value as PointsTransactionType) || '');
    this.currentPage.set(1);
    this.loadTransactions();
  }

  updateSelectedMonth(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.selectedMonth.set(select.value);
    this.currentPage.set(1);
    this.loadTransactions();
  }

  shiftMonth(offset: number): void {
    const [year, month] = this.selectedMonth().split('-').map(Number);
    const next = new Date(year, month - 1 + offset, 1);
    this.selectedMonth.set(
      `${next.getFullYear()}-${String(next.getMonth() + 1).padStart(2, '0')}`
    );
    this.currentPage.set(1);
    this.loadTransactions();
  }

  changePage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.currentPage()) {
      return;
    }

    this.currentPage.set(page);
    this.loadTransactions();
  }

  onSortChange(event: { column: string; direction: 'asc' | 'desc' }): void {
    this.sortBy.set(SORT_MAP[event.column] ?? 'CreatedAt');
    this.sortDir.set(event.direction === 'asc' ? 'Asc' : 'Desc');
    this.currentPage.set(1);
    this.loadTransactions();
  }

  navigateToAddTransaction(): void {
    void this.router.navigate(['/points/add']);
  }

  navigateToAutomation(): void {
    void this.router.navigate(['/points/automation']);
  }

  monthOptionLabel(month: string): string {
    if (month === this.currentMonthKey()) {
      return this.translate.instant('POINTS.THIS_MONTH');
    }

    return this.formatMonth(month);
  }

  typeLabel(type: string): string {
    const key = TYPE_I18N[type as PointsTransactionType];
    return key ? this.translate.instant(key) : type;
  }

  triggerLabel(trigger: string): string {
    if (trigger === 'ManualAdjustment') {
      return this.translate.instant('POINTS.TRIGGER.MANUAL_ADJUSTMENT');
    }

    return trigger;
  }

  formatPoints(points: number): string {
    if (points > 0) {
      return `+ ${points}`;
    }
    if (points < 0) {
      return `- ${Math.abs(points)}`;
    }
    return '0';
  }

  private currentMonthKey(): string {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
  }

  private rollingMonthKeys(count: number): string[] {
    const now = new Date();
    return Array.from({ length: count }, (_, index) => {
      const date = new Date(now.getFullYear(), now.getMonth() - index, 1);
      return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;
    });
  }

  private formatMonth(month: string): string {
    const [year, monthNumber] = month.split('-');
    const locale =
      this.languageService.currentLanguage() === 'ar' ? 'ar-EG' : 'en-US';

    return new Intl.DateTimeFormat(locale, {
      month: 'long',
      year: 'numeric',
    }).format(new Date(Number(year), Number(monthNumber) - 1));
  }
}
