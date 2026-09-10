import {
  AfterViewInit,
  Component,
  ViewChild,
  TemplateRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { TranslateService } from '@ngx-translate/core';

import { ButtonComponent } from '../../../../shared/components/button/button.component';
import { Pagination } from '../../../../shared/components/pagination/pagination';
import { CellContext, ColumnDef, TableComponent } from '../../../../shared/components/table/table.component';
import { ModalComponent } from '../../../../shared/components/modal/modal.component';
import { ModalBodyComponent } from '../../../../shared/components/modal/modal-body.component';
import { ModalFooterComponent } from '../../../../shared/components/modal/modal-footer.component';

import { JobService, Job } from '../../services/job.service';


@Component({
  selector: 'app-job-management',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonComponent,
    Pagination,
    TableComponent,
    ModalComponent,
    ModalBodyComponent,
    ModalFooterComponent,
  ],
  templateUrl: './job-management.component.html',
})
export class JobManagementComponent implements AfterViewInit {

  private readonly jobService = inject(JobService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);

  // =========================================================
  // TEMPLATE REFS
  // =========================================================

  @ViewChild('actionsTemplate') actionsTemplate!: TemplateRef<CellContext>;

  readonly cellTemplates = signal<Map<string, TemplateRef<CellContext>>>(new Map());

  ngAfterViewInit(): void {
    this.cellTemplates.set(new Map([['actionsTemplate', this.actionsTemplate]]));
  }


  // =========================================================
  // DATA
  // =========================================================

  readonly jobs = signal<Job[]>([]);
  readonly totalCount = signal(0);
  readonly currentPage = signal(1);
  readonly pageSize = 10;
  readonly searchTerm = signal('');

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));

  readonly columns = computed<ColumnDef[]>(() => [
    { key: 'title', label: this.translate.instant('JOB_MANAGEMENT.TABLE.JOB_NAME') },
    { key: 'departmentName', label: 'Department', sortable: true },
    { key: 'assignedEmployeesCount', label: this.translate.instant('JOB_MANAGEMENT.TABLE.NUMBER_OF_EMPLOYEES'), sortable: true },
    { key: 'actions', label: this.translate.instant('JOB_MANAGEMENT.TABLE.ACTIONS'), align: 'center', template: 'actionsTemplate', width: '140px' },
  ]);

  readonly displayedJobs = computed(() => this.jobs());


  // =========================================================
  // LIFECYCLE
  // =========================================================

  constructor() {
    this.loadJobs();
  }

  loadJobs(): void {
    this.jobService.getJobs(this.currentPage(), this.pageSize).subscribe({
      next: (response) => {
        this.jobs.set(response.items as Job[]);
        this.totalCount.set(response.totalCount);
      },
      error: (err) => console.error('Error loading jobs:', err),
    });
  }


  // =========================================================
  // SEARCH / PAGINATION / SORT
  // =========================================================

  onSearch(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.searchTerm.set(input.value);
    this.currentPage.set(1);
    this.loadJobs();
  }

  onPageChanged(page: number): void {
    this.currentPage.set(page);
    this.loadJobs();
  }

  onSort(event: { column: string; direction: 'asc' | 'desc' }): void {
    const sorted = [...this.jobs()].sort((a: any, b: any) => {
      const aVal = a[event.column] ?? '';
      const bVal = b[event.column] ?? '';
      if (typeof aVal === 'number' && typeof bVal === 'number') {
        return event.direction === 'asc' ? aVal - bVal : bVal - aVal;
      }
      const r = String(aVal).localeCompare(String(bVal));
      return event.direction === 'asc' ? r : -r;
    });
    this.jobs.set(sorted);
    this.currentPage.set(1);
  }

  onSortToggle(): void {
    const sorted = [...this.jobs()].sort((a, b) => (a.title ?? '').localeCompare(b.title ?? ''));
    this.jobs.set(sorted);
  }


  // =========================================================
  // EXPORT
  // =========================================================

  onExport(): void {
    this.jobService.exportJobs().subscribe({
      next: (csvContent) => {
        const blob = new Blob(['\uFEFF' + csvContent], { type: 'text/csv;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = 'jobs.csv';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
      },
      error: (err) => console.error('Error exporting jobs:', err),
    });
  }


  // =========================================================
  // NAVIGATION
  // =========================================================

  navigateToCreateJob(): void { this.router.navigate(['/jobs/create']); }
  viewJob(job: Job): void { this.router.navigate(['/jobs/details', job.id]); }
  editJob(job: Job): void { this.router.navigate(['/jobs/edit', job.id]); }


  // =========================================================
  // DELETE - STATE
  // =========================================================

  // Modals visibility
  readonly showConfirmDeleteModal = signal(false);  // Modal 1: simple confirm (no employees)
  readonly showReassignModal = signal(false);        // Modal 2: reassign employees first
  readonly showSuccessModal = signal(false);         // Modal 3: success after delete

  // Data
  readonly jobToDelete = signal<Job | null>(null);
  readonly deletionImpact = signal<any>(null);
  readonly isDeleting = signal(false);
  readonly deleteError = signal<string | null>(null);

  // Reassignment state
  readonly defaultReplacementJobId = signal<number | null>(null);
  readonly reassignments = signal<Record<number, number>>({});

  get affectedEmployees(): readonly any[] {
    return this.deletionImpact()?.affectedEmployees ?? [];
  }

  get canConfirmReassign(): boolean {
    const repId = this.defaultReplacementJobId();
    return this.affectedEmployees.every(
      (emp) => this.reassignments()[emp.id] || repId
    );
  }


  // =========================================================
  // DELETE - ACTIONS
  // =========================================================

  deleteJob(job: Job): void {
    this.jobService.getDeletionImpact(job.id).subscribe({
      next: (impact) => {
        this.jobToDelete.set(job);
        this.deletionImpact.set(impact);
        this.deleteError.set(null);
        this.defaultReplacementJobId.set(null);
        this.reassignments.set({});

        if (impact.canDeleteDirectly) {
          // لا يوجد موظفين — اعرض modal تأكيد بسيط
          this.showConfirmDeleteModal.set(true);
        } else {
          // فيه موظفين — اعرض modal الـ reassign
          this.showReassignModal.set(true);
        }
      },
      error: (err) => console.error('Error checking deletion impact:', err),
    });
  }

  // Modal 1: Confirm direct delete
  confirmDirectDelete(): void {
    const job = this.jobToDelete();
    if (!job || this.isDeleting()) return;

    this.isDeleting.set(true);
    this.deleteError.set(null);

    // Use reassign-and-delete with empty payload even when canDeleteDirectly = true
    const payload = {
      defaultReplacementJobId: null,
      reassignments: [],
      replacementJobId: null,
    };

    this.jobService.reassignAndDelete(job.id, payload).subscribe({
      next: () => {
        this.isDeleting.set(false);
        this.showConfirmDeleteModal.set(false);
        this.showSuccessModal.set(true);
      },
      error: () => {
        this.isDeleting.set(false);
        this.deleteError.set('Failed to delete job. Please try again.');
      },
    });
  }

  // Modal 2: Confirm reassign + delete
  confirmReassignAndDelete(): void {
    const job = this.jobToDelete();
    if (!job || this.isDeleting() || !this.canConfirmReassign) return;

    this.isDeleting.set(true);
    this.deleteError.set(null);

    const repId = this.defaultReplacementJobId();
    const reassignmentsArray = this.affectedEmployees
      .map((emp) => {
        const assignedJobId = this.reassignments()[emp.id] ?? repId;
        return assignedJobId ? { employeeId: emp.id, newJobId: +assignedJobId } : null;
      })
      .filter((r): r is { employeeId: number; newJobId: number } => r !== null);

    const payload = {
      defaultReplacementJobId: repId ? +repId : null,
      reassignments: reassignmentsArray,
      replacementJobId: repId ? +repId : null,
    };

    this.jobService.reassignAndDelete(job.id, payload).subscribe({
      next: () => {
        this.isDeleting.set(false);
        this.showReassignModal.set(false);
        this.showSuccessModal.set(true);
      },
      error: () => {
        this.isDeleting.set(false);
        this.deleteError.set('Failed to reassign and delete job. Please try again.');
      },
    });
  }

  onEmployeeJobChange(employeeId: number, jobId: string | number): void {
    const id = typeof jobId === 'string' ? Number(jobId) : jobId;
    this.reassignments.update((cur) => ({ ...cur, [employeeId]: id }));
  }

  // Close modals
  closeConfirmDeleteModal(): void {
    if (this.isDeleting()) return;
    this.showConfirmDeleteModal.set(false);
    this.jobToDelete.set(null);
    this.deletionImpact.set(null);
    this.deleteError.set(null);
  }

  closeReassignModal(): void {
    if (this.isDeleting()) return;
    this.showReassignModal.set(false);
    this.jobToDelete.set(null);
    this.deletionImpact.set(null);
    this.defaultReplacementJobId.set(null);
    this.reassignments.set({});
    this.deleteError.set(null);
  }

  // Success modal — reload jobs after closing
  confirmSuccess(): void {
    this.showSuccessModal.set(false);
    this.jobToDelete.set(null);
    this.deletionImpact.set(null);
    this.loadJobs();
  }
}
