import {
  Component,
  computed,
  effect,
  inject,
  signal,
  type TemplateRef,
  viewChild,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  TranslatePipe,
  TranslateService,
} from '@ngx-translate/core';

import { LanguageService } from '../../../core/services/language.service';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { Pagination } from '../../../shared/components/pagination/pagination';
import {
  TableComponent,
  type CellContext,
  type ColumnDef,
} from '../../../shared/components/table/table.component';
import { PointsManagementService } from '../service/points-management.service';

@Component({
  selector: 'app-points-management',
  standalone: true,
  imports: [
    TableComponent,
    ButtonComponent,
    Pagination,
    TranslatePipe,
  ],
  templateUrl: './points-management.html',
  styleUrl: './points-management.css',
})
export class PointsManagement {
  private readonly pointsService = inject(
    PointsManagementService
  );

  private readonly translate = inject(TranslateService);
  private readonly languageService =
    inject(LanguageService);

  private readonly pointsTemplate =
    viewChild<TemplateRef<CellContext>>(
      'pointsTemplate'
    );

  private readonly triggeredByTemplate =
    viewChild<TemplateRef<CellContext>>(
      'triggeredByTemplate'
    );

  private readonly columnLabels = toSignal(
    this.translate.stream([
      'POINTS.TABLE.ID',
      'POINTS.TABLE.NAME',
      'POINTS.TABLE.DATE',
      'POINTS.TABLE.TIME',
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
        'POINTS.TABLE.POINTS': 'Points',
        'POINTS.TABLE.TRIGGERED_BY':
          'Triggered by',
        'POINTS.TABLE.COMMENTS': 'Comments',
      },
    }
  );

  readonly transactions = toSignal(
    this.pointsService.getTransactions(),
    { initialValue: [] }
  );

  readonly columns = computed<ColumnDef[]>(() => {
    const labels = this.columnLabels();

    return [
      {
        key: 'id',
        label: labels['POINTS.TABLE.ID'],
        width: '0.7fr',
      },
      {
        key: 'name',
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
      },
      {
        key: 'points',
        label: labels['POINTS.TABLE.POINTS'],
        width: '0.8fr',
        template: 'points',
      },
      {
        key: 'triggeredBy',
        label:
          labels['POINTS.TABLE.TRIGGERED_BY'],
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
    const triggeredByTemplate =
      this.triggeredByTemplate();

    const templates =
      new Map<string, TemplateRef<CellContext>>();

    if (pointsTemplate) {
      templates.set('points', pointsTemplate);
    }

    if (triggeredByTemplate) {
      templates.set(
        'triggeredBy',
        triggeredByTemplate
      );
    }

    return templates;
  });

  readonly searchTerm = signal('');
  readonly selectedMonth = signal('');
  readonly selectedTrigger = signal('');
  readonly selectedType = signal('');

  readonly pageSize = 5;
  readonly currentPage = signal(1);

  readonly availableMonths = computed(() =>
    [
      ...new Set(
        this.transactions().map(
          (transaction) => transaction.month
        )
      ),
    ].sort((first, second) =>
      second.localeCompare(first)
    )
  );

  readonly availableTriggers = computed(() =>
    [
      ...new Set(
        this.transactions().map(
          (transaction) => transaction.triggeredBy
        )
      ),
    ].sort()
  );

  readonly availableTypes = computed(() =>
    [
      ...new Set(
        this.transactions().map(
          (transaction) =>
            transaction.transactionType
        )
      ),
    ].sort()
  );

  readonly filteredTransactions = computed(() => {
    const search =
      this.searchTerm().trim().toLowerCase();

    const month = this.selectedMonth();
    const trigger = this.selectedTrigger();
    const type = this.selectedType();

    return this.transactions().filter(
      (transaction) => {
        const matchesSearch =
          !search ||
          transaction.name
            .toLowerCase()
            .includes(search) ||
          transaction.id.includes(search);

        const matchesMonth =
          !month || transaction.month === month;

        const matchesTrigger =
          !trigger ||
          transaction.triggeredBy === trigger;

        const matchesType =
          !type ||
          transaction.transactionType === type;

        return (
          matchesSearch &&
          matchesMonth &&
          matchesTrigger &&
          matchesType
        );
      }
    );
  });

  readonly totalPages = computed(() =>
    Math.max(
      1,
      Math.ceil(
        this.filteredTransactions().length /
          this.pageSize
      )
    )
  );

  readonly paginatedTransactions = computed(() => {
    const start =
      (this.currentPage() - 1) * this.pageSize;

    return this.filteredTransactions().slice(
      start,
      start + this.pageSize
    );
  });

  constructor() {
    effect(() => {
      const totalPages = this.totalPages();

      if (this.currentPage() > totalPages) {
        this.currentPage.set(totalPages);
      }
    });
  }

  updateSearch(event: Event): void {
    const input =
      event.target as HTMLInputElement;

    this.searchTerm.set(input.value);
  }

  updateMonth(event: Event): void {
    const select =
      event.target as HTMLSelectElement;

    this.selectedMonth.set(select.value);
  }

  updateTrigger(event: Event): void {
    const select =
      event.target as HTMLSelectElement;

    this.selectedTrigger.set(select.value);
  }

  updateType(event: Event): void {
    const select =
      event.target as HTMLSelectElement;

    this.selectedType.set(select.value);
  }

  changePage(page: number): void {
    this.currentPage.set(page);
  }

  formatMonth(month: string): string {
    const [year, monthNumber] = month.split('-');

    const locale =
      this.languageService.currentLanguage() ===
      'ar'
        ? 'ar-EG'
        : 'en-US';

    return new Intl.DateTimeFormat(locale, {
      month: 'long',
      year: 'numeric',
    }).format(
      new Date(
        Number(year),
        Number(monthNumber) - 1
      )
    );
  }
}