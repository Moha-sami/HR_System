import { CommonModule } from '@angular/common';
import { Component, computed, effect, inject, signal, type TemplateRef, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LanguageService } from '@app/core/services/language.service';
import { Pagination } from '@app/shared/components/pagination/pagination';
import {
  TableComponent,
  type CellContext,
  type ColumnDef,
} from '@app/shared/components/table/table.component';
import type {
  EmployeePointsTransaction,
  EmployeePointsTransactionFilters,
  EmployeePointsTransactionType,
} from '../../../../models/view-employee/employee-points';
import { EmployeeDetailService } from '../../../../services/employee-detail.service';

@Component({
  selector: 'app-points-rewards-tab',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe, TableComponent, Pagination],
  templateUrl: './points-rewards-tab.component.html',
})
export class PointsRewardsTabComponent {
  private readonly employeeDetailService = inject(EmployeeDetailService);
  private readonly translate = inject(TranslateService);
  private readonly languageService = inject(LanguageService);

  private readonly dateTemplate = viewChild<TemplateRef<CellContext>>('dateTemplate');
  private readonly amountTemplate = viewChild<TemplateRef<CellContext>>('amountTemplate');
  private readonly nullableTemplate = viewChild<TemplateRef<CellContext>>('nullableTemplate');

  readonly employee = this.employeeDetailService.detailEmployee;
  readonly summary = this.employeeDetailService.pointsSummary;
  readonly summaryLoading = this.employeeDetailService.pointsSummaryLoading;
  readonly summaryError = this.employeeDetailService.pointsSummaryError;
  readonly transactionsResponse = this.employeeDetailService.pointsTransactions;
  readonly transactionsLoading = this.employeeDetailService.pointsTransactionsLoading;
  readonly transactionsError = this.employeeDetailService.pointsTransactionsError;

  readonly selectedDate = signal('');
  readonly selectedType = signal<EmployeePointsTransactionType | ''>('');
  readonly triggeredBy = signal('');
  readonly searchText = signal('');
  readonly currentPage = signal(1);
  readonly pageSize = 10;
  readonly paginationKey = signal(0);
  readonly transactionTypes: readonly EmployeePointsTransactionType[] = [
    'Earned',
    'Redeemed',
    'Add',
    'Deduct',
  ];

  readonly columns = computed<ColumnDef[]>(() => {
    this.languageService.currentLanguage();
    return [
      {
        key: 'date',
        label: this.translate.instant('EMPLOYEE_DETAIL.POINTS_REWARDS.DATE'),
        width: '1.2fr',
        template: 'date',
      },
      {
        key: 'amount',
        label: this.translate.instant('EMPLOYEE_DETAIL.POINTS_REWARDS.POINTS_VALUE'),
        width: '1fr',
        template: 'amount',
      },
      {
        key: 'triggeredBy',
        label: this.translate.instant('EMPLOYEE_DETAIL.POINTS_REWARDS.TRIGGERED_BY'),
        width: '1.4fr',
        template: 'nullable',
      },
      {
        key: 'comments',
        label: this.translate.instant('EMPLOYEE_DETAIL.POINTS_REWARDS.COMMENTS'),
        width: '2fr',
        template: 'nullable',
      },
    ];
  });

  readonly cellTemplates = computed(() => {
    const templates = new Map<string, TemplateRef<CellContext>>();
    const date = this.dateTemplate();
    const amount = this.amountTemplate();
    const nullable = this.nullableTemplate();
    if (date) templates.set('date', date);
    if (amount) templates.set('amount', amount);
    if (nullable) templates.set('nullable', nullable);
    return templates;
  });

  readonly visibleTransactions = computed(() => {
    const search = this.searchText().trim().toLocaleLowerCase();
    const items = this.transactionsResponse()?.items ?? [];
    if (!search) return [...items];

    return items.filter((transaction) =>
      [
        transaction.triggeredBy,
        transaction.comments,
        transaction.type,
        this.formatAmount(transaction.amount),
        String(transaction.amount),
      ].some((value) => value?.toLocaleLowerCase().includes(search)),
    );
  });

  private lastEmployeeId: number | null = null;
  private lastAppliedTriggeredBy = '';

  constructor() {
    effect(() => {
      const employeeId = this.employee()?.id;
      if (!employeeId || employeeId === this.lastEmployeeId) return;

      this.lastEmployeeId = employeeId;
      this.resetForEmployeeChange();
      this.employeeDetailService.loadEmployeePointsSummary(employeeId);
      this.loadTransactions();
    });
  }

  onServerFilterChange(): void {
    this.currentPage.set(1);
    this.paginationKey.update((key) => key + 1);
    this.loadTransactions();
  }

  onTriggeredByKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      (event.target as HTMLInputElement).blur();
    }
  }

  applyTriggeredByFilter(): void {
    const nextTriggeredBy = this.triggeredBy().trim();
    if (nextTriggeredBy === this.lastAppliedTriggeredBy) return;

    this.lastAppliedTriggeredBy = nextTriggeredBy;
    this.onServerFilterChange();
  }

  onSearchChange(value: string): void {
    this.searchText.set(value);
  }

  changePage(page: number): void {
    this.currentPage.set(page);
    this.loadTransactions();
  }

  retrySummary(): void {
    const employeeId = this.employee()?.id;
    if (employeeId) this.employeeDetailService.loadEmployeePointsSummary(employeeId);
  }

  loadTransactions(): void {
    const employeeId = this.employee()?.id;
    if (!employeeId) return;
    this.employeeDetailService.loadEmployeePointsTransactions(employeeId, this.buildFilters());
  }

  formatDate(value: string): string {
    const locale = this.languageService.currentLanguage() === 'ar' ? 'ar-EG' : 'en-US';
    return new Date(value).toLocaleDateString(locale, {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
    });
  }

  formatAmount(amount: number): string {
    return amount > 0 ? `+${amount}` : String(amount);
  }

  private buildFilters(): EmployeePointsTransactionFilters {
    const selectedDate = this.selectedDate();
    let dateFrom: string | undefined;
    let dateTo: string | undefined;

    if (selectedDate) {
      const [year, month, day] = selectedDate.split('-').map(Number);
      // Treat the selected date as a local calendar day, then send its exact UTC boundaries.
      dateFrom = new Date(year, month - 1, day, 0, 0, 0, 0).toISOString();
      dateTo = new Date(year, month - 1, day, 23, 59, 59, 999).toISOString();
    }

    return {
      page: this.currentPage(),
      pageSize: this.pageSize,
      type: this.selectedType() || undefined,
      triggeredBy: this.triggeredBy().trim() || undefined,
      dateFrom,
      dateTo,
    };
  }

  private resetForEmployeeChange(): void {
    this.selectedDate.set('');
    this.selectedType.set('');
    this.triggeredBy.set('');
    this.lastAppliedTriggeredBy = '';
    this.searchText.set('');
    this.currentPage.set(1);
    this.paginationKey.update((key) => key + 1);
    this.employeeDetailService.clearEmployeePoints();
  }
}
