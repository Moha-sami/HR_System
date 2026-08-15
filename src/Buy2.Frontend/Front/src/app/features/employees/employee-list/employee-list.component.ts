import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ButtonComponent } from '@app/shared/components/button/button.component';
import { Pagination } from '@app/shared/components/pagination/pagination';
import { type ColumnDef, TableComponent } from '@app/shared/components/table/table.component';
import { MOCK_EMPLOYEES } from '../models/employee';

@Component({
  selector: 'app-employee-list',
  standalone: true,
  imports: [ButtonComponent, Pagination, TableComponent],
  templateUrl: './employee-list.component.html',
})
export class EmployeeListComponent {
  private readonly router = inject(Router);

  readonly pageSize = 5;
  readonly employees = MOCK_EMPLOYEES;
  readonly currentPage = signal(1);
  readonly totalPages = Math.max(1, Math.ceil(this.employees.length / this.pageSize));

  readonly columns: ColumnDef[] = [
    { key: 'id', label: 'ID', width: '80px', sortable: true },
    { key: 'firstName', label: 'First Name', sortable: true },
    { key: 'lastName', label: 'Last Name', sortable: true },
    { key: 'email', label: 'Email', sortable: true },
    { key: 'phoneNumber', label: 'Phone Number' },
    { key: 'jobRoleId', label: 'Job Role ID', align: 'center' },
    { key: 'roleId', label: 'Role ID', align: 'center' },
    { key: 'siteId', label: 'Site ID', align: 'center' },
  ];

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
}
