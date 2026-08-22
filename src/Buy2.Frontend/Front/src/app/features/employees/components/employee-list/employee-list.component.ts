import {
  type AfterViewInit,
  Component,
  computed,
  inject,
  signal,
  type TemplateRef,
  ViewChild,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { LanguageService } from '@app/core/services/language.service';
import { ButtonComponent } from '@app/shared/components/button/button.component';
import { ModalBodyComponent } from '@app/shared/components/modal/modal-body.component';
import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { ModalFooterComponent } from '@app/shared/components/modal/modal-footer.component';
import { ModalHeaderComponent } from '@app/shared/components/modal/modal-header.component';
import { Pagination } from '@app/shared/components/pagination/pagination';
import {
  type CellContext,
  type ColumnDef,
  TableComponent,
} from '@app/shared/components/table/table.component';
import { EmployeeCreateComponent } from '../employee-create/employee-create.component';
import { type Employee } from '../../models/employee';
import { type EmployeeApiResponse, EmployeeService } from '../../services/employee.service';

interface EmployeeListRow extends Employee {
  readonly apiEmployee: EmployeeApiResponse;
}

@Component({
  selector: 'app-employee-list',
  standalone: true,
  imports: [
    ButtonComponent,
    EmployeeCreateComponent,
    ModalComponent,
    ModalHeaderComponent,
    ModalBodyComponent,
    ModalFooterComponent,
    Pagination,
    TableComponent,
    TranslatePipe,
  ],
  templateUrl: './employee-list.component.html',
})
export class EmployeeListComponent implements AfterViewInit {
  private readonly employeeService = inject(EmployeeService);
  private readonly languageService = inject(LanguageService);
  private readonly translate = inject(TranslateService);
  private readonly languageChange = toSignal(this.translate.onLangChange, { initialValue: null });

  @ViewChild('employeeNameTemplate') employeeNameTemplate!: TemplateRef<CellContext>;
  @ViewChild('actionsTemplate') actionsTemplate!: TemplateRef<CellContext>;
  @ViewChild(EmployeeCreateComponent) employeeCreateComponent?: EmployeeCreateComponent;

  readonly pageSize = 5;
  readonly employees = signal<readonly EmployeeListRow[]>([]);
  readonly currentPage = signal(1);
  readonly loading = signal(false);
  readonly loadError = signal(false);
  readonly showCreateModal = signal(false);
  readonly showEditModal = signal(false);
  readonly showDeleteModal = signal(false);
  readonly showSuccessModal = signal(false);
  readonly editingEmployee = signal<EmployeeApiResponse | null>(null);
  readonly deletingEmployee = signal<EmployeeApiResponse | null>(null);
  readonly isDeleting = signal(false);
  readonly deleteError = signal<string | null>(null);
  readonly successMessage = signal<'CREATE_SUCCESS_MESSAGE' | 'UPDATE_SUCCESS_MESSAGE' | 'DELETE_SUCCESS_MESSAGE'>(
    'CREATE_SUCCESS_MESSAGE',
  );
  readonly cellTemplates = signal<Map<string, TemplateRef<CellContext>>>(new Map());
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.employees().length / this.pageSize)));

  readonly columns = computed<ColumnDef[]>(() => {
    this.languageService.currentLanguage();
    this.languageChange();

    return [
      { key: 'id', label: this.translate.instant('EMPLOYEE_MANAGEMENT.EMPLOYEE_ID'), width: '140px' },
      { key: 'employeeName', label: this.translate.instant('EMPLOYEE_MANAGEMENT.EMPLOYEE_NAME'), template: 'employeeNameTemplate' },
      { key: 'joinDate', label: this.translate.instant('EMPLOYEE_MANAGEMENT.JOIN_DATE') },
      { key: 'jobTitle', label: this.translate.instant('EMPLOYEE_MANAGEMENT.JOB_TITLE') },
      { key: 'email', label: this.translate.instant('EMPLOYEE_MANAGEMENT.EMAIL'), align: 'center' },
      { key: 'adminAccess', label: this.translate.instant('EMPLOYEE_MANAGEMENT.ADMIN_ACCESS'), align: 'center' },
      { key: 'actions', label: this.translate.instant('EMPLOYEE_MANAGEMENT.ACTIONS'), align: 'center', template: 'actionsTemplate', width: '90px' },
    ];
  });

  readonly displayedEmployees = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.employees().slice(start, start + this.pageSize);
  });

  constructor() {
    this.loadEmployees();
  }

  ngAfterViewInit(): void {
    this.cellTemplates.set(
      new Map([
        ['employeeNameTemplate', this.employeeNameTemplate],
        ['actionsTemplate', this.actionsTemplate],
      ]),
    );
  }

  onPageChanged(page: number): void {
    this.currentPage.set(page);
  }

  openCreateModal(): void {
    this.showCreateModal.set(true);
  }

  closeCreateModal(): void {
    if (this.showSuccessModal() || this.isSubmittingEmployee()) {
      return;
    }
    this.showCreateModal.set(false);
  }

  openEditModal(employee: EmployeeListRow): void {
    this.editingEmployee.set(employee.apiEmployee);
    this.showEditModal.set(true);
  }

  closeEditModal(): void {
    if (this.showSuccessModal() || this.isSubmittingEmployee()) {
      return;
    }
    this.showEditModal.set(false);
    this.editingEmployee.set(null);
  }

  openDeleteModal(employee: EmployeeListRow): void {
    this.deletingEmployee.set(employee.apiEmployee);
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

  submitEmployeeForm(): void {
    this.employeeCreateComponent?.submit();
  }

  isSubmittingEmployee(): boolean {
    return this.employeeCreateComponent?.isSubmitting() ?? false;
  }

  isEmployeeFormUnavailable(): boolean {
    return (
      this.employeeCreateComponent?.loadingOptions() ?? true
    ) || this.isSubmittingEmployee();
  }

  onEmployeeCreated(): void {
    this.successMessage.set(this.showEditModal() ? 'UPDATE_SUCCESS_MESSAGE' : 'CREATE_SUCCESS_MESSAGE');
    this.showSuccessModal.set(true);
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
    this.showCreateModal.set(false);
    this.showEditModal.set(false);
    this.showDeleteModal.set(false);
    this.editingEmployee.set(null);
    this.deletingEmployee.set(null);
    this.loadEmployees(true);
  }

  private loadEmployees(showNewest = false): void {
    this.loading.set(true);
    this.loadError.set(false);

    forkJoin({
      employees: this.employeeService.getEmployees(),
      jobRoles: this.employeeService.getJobRoles(),
      roles: this.employeeService.getRoles(),
    }).subscribe({
      next: ({ employees, jobRoles, roles }) => {
        const jobTitles = new Map(jobRoles.map((jobRole) => [jobRole.id, jobRole.title]));
        const roleNames = new Map(roles.map((role) => [role.id, role.name]));
        this.employees.set(
          employees.map((employee) => ({
            id: String(employee.id).padStart(4, '0'),
            firstName: employee.firstName,
            lastName: employee.lastName,
            joinDate: formatJoinDate(employee.createdAt),
            jobTitle: jobTitles.get(employee.jobRoleId) ?? '',
            email: employee.email,
            adminAccess: roleNames.get(employee.roleId) === 'SuperAdmin' ? 'Full' : 'Limited',
            apiEmployee: employee,
          })),
        );
        this.currentPage.set(showNewest ? this.totalPages() : 1);
        this.loading.set(false);
      },
      error: () => {
        this.loadError.set(true);
        this.loading.set(false);
      },
    });
  }
}

function formatJoinDate(value: string): string {
  const date = new Date(value);
  return `${date.getUTCDate()}-${date.getUTCMonth() + 1}-${date.getUTCFullYear()}`;
}
