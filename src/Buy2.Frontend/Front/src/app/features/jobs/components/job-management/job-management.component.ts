import {
  AfterViewInit,
  Component,
  ViewChild,
  TemplateRef,
  computed,
  inject,
  signal,
} from '@angular/core';

import { ButtonComponent } from '../../../../shared/components/button/button.component';
import { Pagination } from '../../../../shared/components/pagination/pagination';

import {
  CellContext,
  ColumnDef,
  TableComponent,
} from '../../../../shared/components/table/table.component';

import { JobService, Job } from '../../services/job.service';

@Component({
  selector: 'app-job-management',
  standalone: true,
  imports: [
    ButtonComponent,
    Pagination,
    TableComponent,
  ],
  templateUrl: './job-management.component.html',
})
export class JobManagementComponent implements AfterViewInit {


  private readonly jobService = inject(JobService);


  // =========================================================
  // TEMPLATE
  // =========================================================

  @ViewChild('actionsTemplate')
  actionsTemplate!: TemplateRef<CellContext>;


  // =========================================================
  // DATA
  // =========================================================

  readonly jobs = signal<Job[]>([]);

  readonly currentPage = signal(1);

  readonly pageSize = 5;

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

  readonly columns: ColumnDef[] = [
    {
      key: 'jobName',
      label: 'Job Name',
    },

    {
      key: 'jobDescription',
      label: 'Job Description',
      sortable: true,
    },

    {
      key: 'numberOfEmployees',
      label: 'Number of employees',
      sortable: true,
      align: 'center',
    },

    {
      key: 'actions',
      label: 'Actions',
      align: 'center',
      template: 'actionsTemplate',
      width: '140px',
    },
  ];


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

    const search =
      this.searchTerm()
        .toLowerCase()
        .trim();

    const data = this.jobs();

    if (!search) {
      return data;
    }

    return data.filter(job =>
      (job.jobName ?? '')
        .toLowerCase()
        .includes(search)
      ||
      (job.jobDescription ?? '')
        .toLowerCase()
        .includes(search)
    );

  });


  // =========================================================
  // TOTAL PAGES
  // =========================================================

  readonly totalPages = computed(() => {

    return Math.max(
      1,
      Math.ceil(
        this.filteredJobs().length /
        this.pageSize
      )
    );

  });


  // =========================================================
  // DISPLAYED JOBS
  // =========================================================

  readonly displayedJobs = computed(() => {

    const start =
      (this.currentPage() - 1) *
      this.pageSize;

    return this.filteredJobs().slice(
      start,
      start + this.pageSize
    );

  });


  // =========================================================
  // LOAD JOBS
  // =========================================================

  loadJobs(): void {

    this.jobService
      .getJobs()
      .subscribe({

        next: (jobs) => {

          this.jobs.set(jobs);

          this.currentPage.set(1);

        },

        error: (error) => {

          console.error(
            'Error loading jobs:',
            error
          );

        },

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

    const sorted =
      [...this.jobs()].sort((a, b) =>
        (a.jobName ?? '')
          .localeCompare(
            b.jobName ?? ''
          )
      );

    this.jobs.set(sorted);

    this.currentPage.set(1);

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


    const rows =
      data.map(job => [

        job.jobName ?? '',

        job.jobDescription ?? '',

        job.numberOfEmployees ?? '',

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

    console.log(
      'Create new job'
    );

  }


  // =========================================================
  // VIEW
  // =========================================================

  viewJob(job: Job): void {

    console.log(
      'View job:',
      job
    );

  }


  // =========================================================
  // EDIT
  // =========================================================

  editJob(job: Job): void {

    console.log(
      'Edit job:',
      job
    );

  }


  // =========================================================
  // DELETE
  // =========================================================

  deleteJob(job: Job): void {

    const confirmed =
      window.confirm(
        `Are you sure you want to delete "${job.jobName}"?`
      );


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
