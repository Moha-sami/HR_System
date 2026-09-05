import {
  AfterViewInit,
  Component,
  ViewChild,
  TemplateRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { RouterLink, RouterOutlet, Router } from '@angular/router';

import { ButtonComponent } from '../../../../shared/components/button/button.component';
import { Pagination } from '../../../../shared/components/pagination/pagination';

import {
  CellContext,
  ColumnDef,
  TableComponent,
} from '../../../../shared/components/table/table.component';

import { JobService, Job } from '../../services/job.service';
import { JobCreateComponent } from '../job-create/job-create.component';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-job-management',
  standalone: true,
  imports: [
    ButtonComponent,
    Pagination,
    TableComponent,
    // RouterLink,
    RouterOutlet,
  ],
  templateUrl: './job-management.component.html',
})
export class JobManagementComponent implements AfterViewInit {

  private readonly translate = inject(TranslateService);

  private readonly jobService = inject(JobService);
  private readonly router = inject(Router);


  // =========================================================
  // TEMPLATE
  // =========================================================

  @ViewChild('actionsTemplate')
  actionsTemplate!: TemplateRef<CellContext>;


  // =========================================================
  // DATA
  // =========================================================

  readonly jobs = signal<Job[]>([]);
  readonly totalCount = signal(0);

  readonly currentPage = signal(1);

  readonly pageSize = 10;

  readonly searchTerm = signal('');


  // =========================================================
  // CELL TEMPLATES
  // =========================================================

  readonly cellTemplates =
    signal<Map<string, TemplateRef<CellContext>>>(
      new Map()
    );


  // =========================================================
  // TABLE COLUMNS
  // =========================================================

  readonly columns = computed<ColumnDef[]>(() => [
    {
      key: 'title',
      label: this.translate.instant('JOB_MANAGEMENT.TABLE.JOB_NAME'),
    },
    {
      key: 'departmentName',
      label: 'Department',
      sortable: true,
    },
    {
      key: 'assignedEmployeesCount',
      label: this.translate.instant('JOB_MANAGEMENT.TABLE.NUMBER_OF_EMPLOYEES'),
      sortable: true,
    },
    {
      key: 'actions',
      label: this.translate.instant('JOB_MANAGEMENT.TABLE.ACTIONS'),
      align: 'center',
      template: 'actionsTemplate',
      width: '140px',
    },
  ]);


  // =========================================================
  // LIFECYCLE
  // =========================================================

  constructor() {
    this.loadJobs();
  }


  ngAfterViewInit(): void {

    this.cellTemplates.set(
      new Map([
        [
          'actionsTemplate',
          this.actionsTemplate,
        ],
      ])
    );

  }


  // =========================================================
  // FILTERED JOBS
  // =========================================================

  readonly filteredJobs = computed(() => {
    const search = this.searchTerm().toLowerCase().trim();
    const data = this.jobs();
    if (!search) return data;
    return data.filter(job =>
      (job.title ?? '').toLowerCase().includes(search) ||
      (job.departmentName ?? '').toLowerCase().includes(search)
    );
  });


  // =========================================================
  // TOTAL PAGES
  // =========================================================

  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / this.pageSize))
  );

  readonly displayedJobs = computed(() => this.jobs());


  // =========================================================
  // LOAD JOBS
  // =========================================================

  loadJobs(): void {
    this.jobService.getJobs(this.currentPage(), this.pageSize).subscribe({
      next: (response) => {
        this.jobs.set(response.items as Job[]);
        this.totalCount.set(response.totalCount);
      },
      error: (error) => console.error('Error loading jobs:', error),
    });
  }


  // =========================================================
  // SEARCH
  // =========================================================

  onSearch(event: Event): void {

    const input =
      event.target as HTMLInputElement;

    this.searchTerm.set(
      input.value
    );

    this.currentPage.set(1);

  }


  // =========================================================
  // PAGINATION
  // =========================================================

  onPageChanged(page: number): void {
    this.currentPage.set(page);
    this.loadJobs();
  }


  // =========================================================
  // SORT
  // =========================================================

  onSort(event: {
    column: string;
    direction: 'asc' | 'desc';
  }): void {

    const sorted =
      [...this.jobs()].sort((a: any, b: any) => {

        const aValue =
          a[event.column] ?? '';

        const bValue =
          b[event.column] ?? '';


        if (
          typeof aValue === 'number' &&
          typeof bValue === 'number'
        ) {

          return event.direction === 'asc'
            ? aValue - bValue
            : bValue - aValue;

        }


        const result =
          String(aValue).localeCompare(
            String(bValue)
          );


        return event.direction === 'asc'
          ? result
          : -result;

      });


    this.jobs.set(sorted);

    this.currentPage.set(1);

  }


  // =========================================================
  // SORT BUTTON
  // =========================================================

  onSortToggle(): void {
    const sorted = [...this.jobs()].sort((a, b) => (a.title ?? '').localeCompare(b.title ?? ''));
    this.jobs.set(sorted);
  }


  // =========================================================
  // EXPORT
  // =========================================================

  onExport(): void {

    const data =
      this.filteredJobs();


    if (!data.length) {

      return;

    }


    const headers = [
      'Job Name',
      'Job Description',
      'Number of Employees',
    ];


    const rows = data.map(job => [
      job.title ?? '',
      job.departmentName ?? '',
      job.assignedEmployeesCount ?? '',
    ]);


    const csvContent = [

      headers.join(','),

      ...rows.map(row =>
        row
          .map(value =>
            `"${String(value).replace(/"/g, '""')}"`
          )
          .join(',')
      ),

    ].join('\n');


    const blob =
      new Blob(
        [
          '\uFEFF' + csvContent,
        ],
        {
          type:
            'text/csv;charset=utf-8;',
        }
      );


    const url =
      URL.createObjectURL(blob);


    const link =
      document.createElement('a');


    link.href = url;

    link.download = 'jobs.csv';

    document.body.appendChild(link);

    link.click();

    document.body.removeChild(link);

    URL.revokeObjectURL(url);

  }


  // =========================================================
  // CREATE
  // =========================================================

  navigateToCreateJob(): void {
    this.router.navigate(['/jobs/create']);
  }


  // =========================================================
  // VIEW
  // =========================================================

  viewJob(job: Job): void {
    this.router.navigate(['/jobs/details', job.id]);
  }


  // =========================================================
  // EDIT
  // =========================================================

  editJob(job: Job): void {
    this.router.navigate(['/jobs/edit', job.id]);
  }


  // =========================================================
  // DELETE
  // =========================================================

  deleteJob(job: Job): void {

    const confirmed = window.confirm(`Are you sure you want to delete "${job.title}"?`);


    if (!confirmed) {

      return;

    }


    this.jobService
      .deleteJob(job.id)
      .subscribe({

        next: () => {

          this.jobs.update(
            jobs =>
              jobs.filter(
                item =>
                  item.id !== job.id
              )
          );

        },

        error: (error) => {

          console.error(
            'Error deleting job:',
            error
          );

        },

      });

  }

}
