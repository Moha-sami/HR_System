import { Component, computed, inject, OnInit, TemplateRef, viewChild, effect } from '@angular/core';
import { signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { ButtonComponent } from '../../../../shared/components/button/button.component';
import { Pagination } from '../../../../shared/components/pagination/pagination';
import { TableComponent, ColumnDef, CellContext } from '../../../../shared/components/table/table.component';
import { LanguageService } from '../../../../core/services/language.service';
import { RecognitionService } from '../../services/recognition.service';
import type { Recognition } from '../../models/recognition.models';
import { displayDate, STATUSES } from '../../utils/recognition.utils';

type Row = Recognition & { employeeName: string };
@Component({
  selector: 'app-recognition-list', standalone: true,
  imports: [TranslatePipe, ButtonComponent, Pagination, TableComponent],
  templateUrl: './recognition-list.component.html', styleUrl: '../../recognitions.css',
})
export class RecognitionListComponent implements OnInit {
  private readonly service = inject(RecognitionService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);
  private readonly translationChange = toSignal(this.translate.onLangChange);
  readonly language = inject(LanguageService).currentLanguage;
  readonly items = signal<Row[]>([]);
  readonly loading = signal(false);
  readonly error = signal(false);
  readonly query = signal('');
  readonly status = signal('');
  readonly sort = signal('newest');
  readonly page = signal(1);
  readonly statuses = STATUSES;
  readonly pageSize = 8;
  readonly dateCell = viewChild.required<TemplateRef<CellContext>>('dateCell');
  readonly statusCell = viewChild.required<TemplateRef<CellContext>>('statusCell');
  readonly actionCell = viewChild.required<TemplateRef<CellContext>>('actionCell');
  readonly pointsCell = viewChild.required<TemplateRef<CellContext>>('pointsCell');
  readonly paginator = viewChild(Pagination);
  readonly templates = computed(() => new Map([
    ['date', this.dateCell()], ['status', this.statusCell()], ['action', this.actionCell()], ['points', this.pointsCell()],
  ]));
  readonly columns = computed<ColumnDef[]>(() => {
    this.translationChange();
    this.language();
    return [
      { key: 'publishAt', label: this.translate.instant('RECOGNITIONS.DATE_TIME'), template: 'date', width: '180px' },
      { key: 'employeeName', label: this.translate.instant('RECOGNITIONS.EMPLOYEE') },
      { key: 'title', label: this.translate.instant('RECOGNITIONS.TABLE_TITLE') },
      { key: 'createdBy', label: this.translate.instant('RECOGNITIONS.POSTED_BY') },
      { key: 'points', label: this.translate.instant('RECOGNITIONS.POINTS'), template: 'points', width: '85px' },
      { key: 'status', label: this.translate.instant('RECOGNITIONS.STATUS'), template: 'status', width: '125px' },
      { key: 'action', label: this.translate.instant('RECOGNITIONS.VIEW_DETAILS'), template: 'action', width: '130px' },
    ];
  });
  readonly filtered = computed(() => {
    const q = this.query().trim().toLocaleLowerCase(this.language());
    const items = this.items().filter(item => (!this.status() || item.status === this.status()) &&
      (!q || `${item.employeeName} ${item.title} ${item.createdBy}`.toLocaleLowerCase(this.language()).includes(q)));
    return items.sort((a, b) => {
      if (this.sort() === 'title') return a.title.localeCompare(b.title, this.language());
      if (this.sort() === 'employee') return a.employeeName.localeCompare(b.employeeName, this.language());
      const difference = new Date(a.publishAt || a.createdAt).getTime() - new Date(b.publishAt || b.createdAt).getTime();
      return this.sort() === 'oldest' ? difference : -difference;
    });
  });
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.filtered().length / this.pageSize)));
  readonly rows = computed(() => this.filtered().slice((this.page() - 1) * this.pageSize, this.page() * this.pageSize));
  constructor() {
    // The shared paginator only reads initialPage once; synchronize this instance without changing other features.
    effect(() => this.paginator()?.currentPage.set(this.page()));
  }
  ngOnInit() { this.load(); }
  load() {
    this.loading.set(true); this.error.set(false);
    forkJoin({ items: this.service.list(), employees: this.service.employees() }).subscribe({
      next: ({ items, employees }) => {
        this.items.set(items.map(item => { const employee = employees.find(e => e.id === String(item.employeeId));
          return { ...item, employeeName: employee ? `${employee.firstName} ${employee.lastName}` : '—' }; }));
        this.page.set(1); this.loading.set(false);
      }, error: () => { this.error.set(true); this.loading.set(false); },
    });
  }
  search(value: string) { this.query.set(value); this.page.set(1); }
  filter(value: string) { this.status.set(value); this.page.set(1); }
  order(value: string) { this.sort.set(value); this.page.set(1); }
  date(item: Row, time = false) { return displayDate(item.publishAt || item.createdAt, this.language(), time); }
  points(value: number | null) { return value === null ? '—' : new Intl.NumberFormat(this.language()).format(value); }
  open(id: string) { void this.router.navigate(['/recognitions', id]); }
  create() { void this.router.navigate(['/recognitions/create']); }
}
