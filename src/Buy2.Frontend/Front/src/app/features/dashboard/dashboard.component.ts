import { Component, computed, inject } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';
import { AdminDashboardComponent } from './admin-dashboard.component';
import { EmployeeDashboardComponent } from './employee-dashboard.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [AdminDashboardComponent, EmployeeDashboardComponent],
  template: `
    @if (isAdmin()) {
      <app-admin-dashboard />
    } @else {
      <app-employee-dashboard />
    }
  `,
})
export class DashboardComponent {
  private readonly auth = inject(AuthService);

  readonly isAdmin = computed(() => this.auth.currentUser()?.role !== 'Employee');
}
