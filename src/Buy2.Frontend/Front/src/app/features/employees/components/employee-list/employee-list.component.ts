import {
  type AfterViewInit,
  Component,
  computed,
  inject,
  signal,
  type TemplateRef,
  ViewChild,
  type OnDestroy,
} from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { Router, ActivatedRoute } from '@angular/router';
import { ButtonComponent } from '@app/shared/components/button/button.component';
import { ModalBodyComponent } from '@app/shared/components/modal/modal-body.component';
import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { ModalFooterComponent } from '@app/shared/components/modal/modal-footer.component';
import { Pagination } from '@app/shared/components/pagination/pagination';
import { TableComponent, type ColumnDef } from '@app/shared/components/table/table.component';
import { EmployeeService } from '../../services/employee.service';
import type {
  EmployeeFilterDto,
  PaginatedEmployeeListDto,
  EmployeeListRowDto,
} from '../../models/employee-list/employee-list';

@Component({
  selector: 'app-employee-list',
  standalone: true,
  imports: [
    ButtonComponent,
    ModalComponent,
    ModalBodyComponent,
    ModalFooterComponent,
    Pagination,
    TableComponent,
    TranslatePipe,
  ],
  templateUrl: './employee-list.component.html',
})
export class EmployeeListComponent implements AfterViewInit, OnDestroy {
  private readonly employeeService = inject(EmployeeService);
  private readonly translate = inject(TranslateService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  @ViewChild('employeeNameTemplate') employeeNameTemplate!: TemplateRef<any>;
  @ViewChild('adminAccessTemplate') adminAccessTemplate!: TemplateRef<any>;
  @ViewChild('actionsTemplate') actionsTemplate!: TemplateRef<any>;

  private readonly destroy$ = new Subject<void>();
  private readonly searchSubject = new Subject<string>();
  private readonly cellTemplatesMap = signal<Map<string, any>>(new Map());

  // State signals
  readonly employees = signal<EmployeeListRowDto[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly loadError = signal(false);
  readonly currentPage = signal(1);
  readonly pageSize = 20;

  // Filters
  readonly search = signal('');
  readonly region = signal<string | null>(null);
  readonly department = signal<string | null>(null);

  // Sort
  readonly sortColumn = signal<'name' | 'employeecode' | 'email' | 'jobtitle' | 'joindate'>(
    'joindate',
  );
  readonly sortDirection = signal<'asc' | 'desc'>('desc');

  // UI state
  readonly showDeleteModal = signal(false);
  readonly showSuccessModal = signal(false);
  readonly deletingEmployee = signal<EmployeeListRowDto | null>(null);
  readonly isDeleting = signal(false);
  readonly deleteError = signal<string | null>(null);
  readonly successMessage = signal<'CREATE_SUCCESS_MESSAGE' | 'DELETE_SUCCESS_MESSAGE'>(
    'CREATE_SUCCESS_MESSAGE',
  );

  // Computed
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));

  readonly columns = computed<ColumnDef[]>(() => {
    this.translate.onLangChange;

    return [
      {
        key: 'id',
        label: this.translate.instant('EMPLOYEE_MANAGEMENT.EMPLOYEE_ID'),
        width: '140px',
        align: 'center',
      },
      {
        key: 'employeeName',
        label: this.translate.instant('EMPLOYEE_MANAGEMENT.EMPLOYEE_NAME'),
        template: 'employeeNameTemplate',
        sortable: true,
      },
      {
        key: 'joinDate',
        label: this.translate.instant('EMPLOYEE_MANAGEMENT.JOIN_DATE'),
        sortable: true,
      },
      {
        key: 'jobTitle',
        label: this.translate.instant('EMPLOYEE_MANAGEMENT.JOB_TITLE'),
        sortable: true,
      },
      { key: 'email', label: this.translate.instant('EMPLOYEE_MANAGEMENT.EMAIL'), sortable: true },
      {
        key: 'adminAccess',
        label: this.translate.instant('EMPLOYEE_MANAGEMENT.ADMIN_ACCESS'),
        align: 'center',
        template: 'adminAccessTemplate',
      },
      {
        key: 'actions',
        label: this.translate.instant('EMPLOYEE_MANAGEMENT.ACTIONS'),
        align: 'center',
        width: '100px',
        template: 'actionsTemplate',
      },
    ];
  });

  constructor() {
    // Debounced search
    this.searchSubject
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe((search) => {
        this.search.set(search);
        this.currentPage.set(1);
        this.syncToUrl();
        this.loadEmployees();
      });

    // Read initial state from URL query params
    this.route.queryParams.pipe(takeUntil(this.destroy$)).subscribe((params) => {
      this.search.set(params['search'] || '');
      this.region.set(params['region'] || null);
      this.department.set(params['department'] || null);
      this.currentPage.set(parseInt(params['page'], 10) || 1);
      this.sortColumn.set((params['sort'] as EmployeeFilterDto['sort']) || 'joindate');
      this.sortDirection.set((params['sortDir'] as 'asc' | 'desc') || 'desc');
    });

    // Initial load
    this.loadEmployees();
  }

  ngAfterViewInit(): void {
    this.cellTemplatesMap.set(
      new Map([
        ['employeeNameTemplate', this.employeeNameTemplate],
        ['actionsTemplate', this.actionsTemplate],
        ['adminAccessTemplate', this.adminAccessTemplate],
      ]),
    );
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // Search handler
  onSearchChange(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.searchSubject.next(target.value);
  }

  // Filter changes
  // Sync state to URL query params
  private syncToUrl(): void {
    const queryParams: Record<string, string | null> = {
      page: this.currentPage() > 1 ? this.currentPage().toString() : null,
      pageSize: this.pageSize.toString(),
      search: this.search() || null,
      region: this.region(),
      department: this.department(),
      sort: this.sortColumn(),
      sortDir: this.sortDirection(),
    };

    // With queryParamsHandling: 'merge', set to null to remove, don't delete the key
    // This ensures params with null values are removed from URL
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  // Filter changes
  onRegionChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    this.region.set(target.value || null);
    this.currentPage.set(1);
    this.syncToUrl();
    this.loadEmployees();
  }

  onDepartmentChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    this.department.set(target.value || null);
    this.currentPage.set(1);
    this.syncToUrl();
    this.loadEmployees();
  }

  // Sort handler from TableComponent
  onSortChange(event: { column: string; direction: 'asc' | 'desc' }): void {
    const sortMap: Record<string, 'name' | 'employeecode' | 'email' | 'jobtitle' | 'joindate'> = {
      employeeName: 'name',
      joinDate: 'joindate',
      jobTitle: 'jobtitle',
      email: 'email',
      employeeCode: 'employeecode',
    };
    const apiSort = sortMap[event.column] || 'joindate';
    this.sortColumn.set(apiSort);
    this.sortDirection.set(event.direction);
    this.currentPage.set(1);
    this.syncToUrl();
    this.loadEmployees();
  }

  // Pagination handler
  onPageChanged(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    if (page === this.currentPage()) return;
    this.currentPage.set(page);
    this.syncToUrl();
    this.loadEmployees();
  }

  // Load employees with current filters/sort/pagination
  loadEmployees(): void {
    this.loading.set(true);
    this.loadError.set(false);

    const filter: EmployeeFilterDto = {
      page: this.currentPage(),
      pageSize: 20,
      search: this.search() || undefined,
      region: this.region() || undefined,
      department: this.department() || undefined,
      sort: this.sortColumn(),
      sortDir: this.sortDirection(),
    };

    this.employeeService.getEmployeesPaginated(filter).subscribe({
      next: (response: PaginatedEmployeeListDto) => {
        this.employees.set([...response.items]);
        this.totalCount.set(response.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.loadError.set(true);
        this.loading.set(false);
      },
    });
  }

  // Export CSV
  exportCSV(): void {
    const filter: EmployeeFilterDto = {
      page: 1,
      pageSize: this.totalCount() || 1000,
      search: this.search() || undefined,
      region: this.region() || undefined,
      department: this.department() || undefined,
      sort: this.sortColumn(),
      sortDir: this.sortDirection(),
    };

    this.employeeService.exportEmployees(filter).subscribe({
      next: (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `employees-${new Date().toISOString().split('T')[0]}.csv`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => {},
    });
  }

  // Navigation actions
  navigateToCreate(): void {
    this.router.navigate(['/employees/create']);
  }

  navigateToDetail(employee: EmployeeListRowDto): void {
    this.router.navigate(['/employees', employee.id, 'information']);
  }

  // Delete actions
  openDeleteModal(employee: EmployeeListRowDto): void {
    this.deletingEmployee.set(employee);
    this.deleteError.set(null);
    this.showDeleteModal.set(true);
  }

  closeDeleteModal(): void {
    if (this.isDeleting() || this.showSuccessModal()) {
      return;
    }
    this.showDeleteModal.set(false);
    this.deletingEmployee.set(null);
    this.deleteError.set(null);
  }

  confirmDelete(): void {
    const employee = this.deletingEmployee();
    if (!employee || this.isDeleting()) {
      return;
    }

    this.isDeleting.set(true);
    this.deleteError.set(null);
    this.employeeService.deleteEmployee(employee.id).subscribe({
      next: () => {
        this.isDeleting.set(false);
        this.successMessage.set('DELETE_SUCCESS_MESSAGE');
        this.showSuccessModal.set(true);
      },
      error: () => {
        this.isDeleting.set(false);
        this.deleteError.set('EMPLOYEE_MANAGEMENT.DELETE_FAILED');
      },
    });
  }

  confirmSuccess(): void {
    this.showSuccessModal.set(false);
    this.showDeleteModal.set(false);
    this.deletingEmployee.set(null);
    this.loadEmployees();
  }

  // Template helpers
  readonly cellTemplates = this.cellTemplatesMap;

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return `${date.getDate()}-${date.getMonth() + 1}-${date.getFullYear()}`;
  }

  getAdminAccessBadgeClass(adminAccess: boolean): string {
    return adminAccess
      ? 'px-2 py-1 text-xs font-medium rounded-full bg-green-100 text-green-800'
      : 'px-2 py-1 text-xs font-medium rounded-full bg-gray-100 text-gray-800';
  }

  getAdminAccessLabel(adminAccess: boolean): string {
    return adminAccess
      ? this.translate.instant('EMPLOYEE_MANAGEMENT.FULL_ACCESS')
      : this.translate.instant('EMPLOYEE_MANAGEMENT.LIMITED_ACCESS');
  }
}
