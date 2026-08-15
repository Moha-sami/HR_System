import { Component, computed, inject, signal, ViewChild, TemplateRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { JobService, Job } from '../../services/job.service';
import { TableComponent, ColumnDef, CellContext } from '../../../../shared/components/table/table.component';
import { Pagination } from '../../../../shared/components/pagination/pagination';

@Component({
  selector: 'app-job-management',
  standalone: true,
  imports: [CommonModule, TableComponent, Pagination],
  templateUrl: './job-management.component.html',
  styleUrl: './job-management.component.css'
})
export class JobManagementComponent implements AfterViewInit {
  private jobService = inject(JobService);

  jobs = signal<Job[]>([]);
  searchTerm = signal('');
  currentPage = signal(1);
  pageSize = signal(5);

  columns: ColumnDef[] = [
    { key: 'jobName', label: 'Job Name', sortable: true },
    { key: 'jobDescription', label: 'Job Description', sortable: true },
    { key: 'numberOfEmployees', label: 'Number of employees', sortable: true, align: 'center' },
    { key: 'actions', label: 'Actions', template: 'actionsTemplate', align: 'center' }
  ];

  @ViewChild('actionsTemplate', { static: true }) actionsTemplate!: TemplateRef<CellContext>;
  cellTemplates = new Map<string, TemplateRef<CellContext>>();

  // Filtered jobs based on search term
  filteredJobs = computed(() => {
    const term = this.searchTerm().toLowerCase();
    const allJobs = this.jobs();
    if (!term) return allJobs;
    return allJobs.filter(job => 
      job.jobName.toLowerCase().includes(term) || 
      job.jobDescription.toLowerCase().includes(term)
    );
  });

  // Calculate total pages
  totalPages = computed(() => {
    return Math.ceil(this.filteredJobs().length / this.pageSize());
  });

  // Paginated jobs for current page
  paginatedJobs = computed(() => {
    const startIndex = (this.currentPage() - 1) * this.pageSize();
    const endIndex = startIndex + this.pageSize();
    return this.filteredJobs().slice(startIndex, endIndex);
  });

  constructor() {
    this.loadJobs();
  }

  ngAfterViewInit() {
    if (this.actionsTemplate) {
      this.cellTemplates.set('actionsTemplate', this.actionsTemplate);
    }
  }

  loadJobs() {
    this.jobService.getJobs().subscribe({
      next: (data) => {
        this.jobs.set(data);
      },
      error: (err) => {
        console.error('Error loading jobs', err);
      }
    });
  }

  onSearch(event: Event) {
    const input = event.target as HTMLInputElement;
    this.searchTerm.set(input.value);
    this.currentPage.set(1); // Reset to first page on search
  }

  onPageChange(page: number) {
    this.currentPage.set(page);
  }

  onSort(event: { column: string, direction: 'asc' | 'desc' }) {
    // Sorting can be done on frontend similar to search
    // The TableComponent might handle it for the currently displayed rows,
    // but if we need to sort across all filtered pages, we'll need to do it here.
    const sorted = [...this.jobs()].sort((a: any, b: any) => {
      const aVal = a[event.column];
      const bVal = b[event.column];
      const comparison = String(aVal).localeCompare(String(bVal));
      return event.direction === 'asc' ? comparison : -comparison;
    });
    this.jobs.set(sorted);
  }
}
