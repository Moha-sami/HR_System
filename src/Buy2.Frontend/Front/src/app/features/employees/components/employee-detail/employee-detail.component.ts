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

  readonly employeeId = signal<number | null>(null);
  readonly employee = this.employeeDetailService.detailEmployee;
  readonly loading = this.employeeDetailService.detailLoading;
  readonly loadError = this.employeeDetailService.detailError;

  readonly tabs = [
    { id: 'information', label: 'EMPLOYEE_DETAIL.TABS.INFORMATION' },
    { id: 'performance', label: 'EMPLOYEE_DETAIL.TABS.PERFORMANCE' },
    { id: 'attendance', label: 'EMPLOYEE_DETAIL.TABS.ATTENDANCE' },
    { id: 'documents', label: 'EMPLOYEE_DETAIL.TABS.DOCUMENTS' },
    { id: 'violations', label: 'EMPLOYEE_DETAIL.TABS.VIOLATIONS' },
    { id: 'points-rewards', label: 'EMPLOYEE_DETAIL.TABS.POINTS_REWARDS' },
  ] as const;

  constructor() {
    this.route.paramMap.subscribe((params) => {
      const rawId = params.get('id');
      const id = rawId && /^\d+$/.test(rawId) ? Number(rawId) : NaN;
      if (Number.isInteger(id) && id > 0 && id <= 2147483647) {
        this.employeeId.set(id);
        this.employeeDetailService.loadDetailEmployee(id);
      } else {
        this.employeeId.set(null);
        this.employeeDetailService.clearDetailEmployee();
      }
    });


  }

  readonly formatGender = (gender: number | null): string => {
    if (gender === 1) return 'Male';
    if (gender === 2) return 'Female';
    return '—';
  };
}
