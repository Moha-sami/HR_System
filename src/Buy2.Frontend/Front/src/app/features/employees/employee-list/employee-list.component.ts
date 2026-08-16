import {
  type AfterViewInit,
  Component,
  computed,
  inject,
  signal,
  type TemplateRef,
  ViewChild,
} from '@angular/core';
import { Router } from '@angular/router';
import { ButtonComponent } from '@app/shared/components/button/button.component';
import { Pagination } from '@app/shared/components/pagination/pagination';
import {
  type CellContext,
  type ColumnDef,
  TableComponent,
} from '@app/shared/components/table/table.component';
import { type Employee, MOCK_EMPLOYEES } from '../models/employee';

@Component({
  selector: 'app-employee-list',
  standalone: true,
  imports: [ButtonComponent, Pagination, TableComponent],
  templateUrl: './employee-list.component.html',
})
export class EmployeeListComponent implements AfterViewInit {
  private readonly router = inject(Router);

  @ViewChild('employeeNameTemplate') employeeNameTemplate!: TemplateRef<CellContext>;
  @ViewChild('actionsTemplate') actionsTemplate!: TemplateRef<CellContext>;

  readonly pageSize = 5;
  readonly employees = MOCK_EMPLOYEES;
  readonly currentPage = signal(1);
  readonly totalPages = Math.max(1, Math.ceil(this.employees.length / this.pageSize));
  readonly cellTemplates = signal<Map<string, TemplateRef<CellContext>>>(new Map());

  readonly columns: ColumnDef[] = [
    { key: 'id', label: 'Employee ID', width: '140px' },
    { key: 'employeeName', label: 'Employee Name', template: 'employeeNameTemplate' },
    { key: 'joinDate', label: 'Join Date' },
    { key: 'jobTitle', label: 'Job Title' },
    { key: 'email', label: 'Email', align: 'center' },
    { key: 'adminAccess', label: 'Admin Access', align: 'center' },
    { key: 'actions', label: 'Actions', align: 'center', template: 'actionsTemplate', width: '90px' },
  ];

  ngAfterViewInit(): void {
    this.cellTemplates.set(
      new Map([
        ['employeeNameTemplate', this.employeeNameTemplate],
        ['actionsTemplate', this.actionsTemplate],
      ]),
    );
  }

  readonly displayedEmployees = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.employees.slice(start, start + this.pageSize);
  });

  onPageChanged(page: number): void {
    this.currentPage.set(page);
  }

  navigateToAddEmployee(): void {
    void this.router.navigate(['/employees/add']);
  }

  viewEmployee(_employee: Employee): void {}
}
