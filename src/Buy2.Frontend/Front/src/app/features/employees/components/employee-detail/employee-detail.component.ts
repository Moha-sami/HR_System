import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { EmployeeDetailService } from '../../services/employee-detail.service';

@Component({
  selector: 'app-employee-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  templateUrl: './employee-detail.component.html',
})
export class EmployeeDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly employeeDetailService = inject(EmployeeDetailService);

  readonly employeeId = signal<number>(0);
  readonly employee = this.employeeDetailService.detailEmployee;
  readonly loading = this.employeeDetailService.detailLoading;
  readonly loadError = this.employeeDetailService.detailError;

  readonly activeTab = signal<
    'information' | 'payroll' | 'attendance' | 'documents' | 'violations'
  >('information');

  readonly tabs = [
    { id: 'information', label: 'EMPLOYEE_DETAIL.TABS.INFORMATION' },
    { id: 'payroll', label: 'EMPLOYEE_DETAIL.TABS.PAYROLL' },
    { id: 'attendance', label: 'EMPLOYEE_DETAIL.TABS.ATTENDANCE' },
    { id: 'documents', label: 'EMPLOYEE_DETAIL.TABS.DOCUMENTS' },
    { id: 'violations', label: 'EMPLOYEE_DETAIL.TABS.VIOLATIONS' },
  ] as const;

  constructor() {
    this.route.paramMap.subscribe((params) => {
      const id = Number(params.get('id'));
      if (id) {
        this.employeeId.set(id);
        this.employeeDetailService.loadDetailEmployee(id);
      }
    });

    this.route.firstChild?.url.subscribe((segments) => {
      const tab = segments[0]?.path as
        'information' | 'payroll' | 'attendance' | 'documents' | 'violations';
      if (tab && this.tabs.some((t) => t.id === tab)) {
        this.activeTab.set(tab);
      }
    });
  }

  readonly formatGender = (gender: number | null): string => {
    if (gender === 1) return 'Male';
    if (gender === 2) return 'Female';
    return '—';
  };
}
