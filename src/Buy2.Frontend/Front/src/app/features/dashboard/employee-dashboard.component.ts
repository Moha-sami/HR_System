import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ButtonComponent } from '@app/shared/components/button/button.component';
import { AuthService } from '../../core/auth/auth.service';
import { EmployeeDetailService } from '../employees/services/employee-detail.service';
import { RequestService } from '../requests/services/request.service';
import type { EmployeePointsSummary } from '../employees/models/view-employee/employee-points';
import type { AttendanceCalendarDto } from '../employees/models/view-employee/employee-attendance';
import type { SubmittedRequest } from '../requests/models/request';

interface RequestStatusView {
  label: string;
  pill: string;
}

const REQUEST_STATUS_VIEW: Record<string, RequestStatusView> = {
  approved: {
    label: 'DASHBOARD.EMPLOYEE.STATUS.APPROVED',
    pill: 'shrink-0 rounded-full bg-success-50 px-2 py-0.5 text-xs font-medium text-success-700',
  },
  rejected: {
    label: 'DASHBOARD.EMPLOYEE.STATUS.REJECTED',
    pill: 'shrink-0 rounded-full bg-error-50 px-2 py-0.5 text-xs font-medium text-error-700',
  },
  pending: {
    label: 'DASHBOARD.EMPLOYEE.STATUS.PENDING',
    pill: 'shrink-0 rounded-full bg-warning-50 px-2 py-0.5 text-xs font-medium text-warning-800',
  },
};

@Component({
  selector: 'app-employee-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslatePipe, ButtonComponent],
  templateUrl: './employee-dashboard.component.html',
})
export class EmployeeDashboardComponent {
  private readonly auth = inject(AuthService);
  private readonly detailService = inject(EmployeeDetailService);
  private readonly requestService = inject(RequestService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly error = signal(false);

  readonly points = signal<EmployeePointsSummary | null>(null);
  readonly attendance = signal<AttendanceCalendarDto | null>(null);
  readonly pendingCount = signal(0);
  readonly myRequests = signal<readonly SubmittedRequest[]>([]);

  readonly employeeId = computed(() => this.auth.currentUser()?.userId ?? 0);
  readonly attendanceRate = computed(() => this.attendance()?.summary.attendanceRate ?? 0);

  constructor() {
    this.load();
  }

  load(): void {
    const id = this.employeeId();
    const now = new Date();

    this.loading.set(true);
    this.error.set(false);

    forkJoin({
      points: this.detailService.getEmployeePointsSummary(id),
      attendance: this.detailService.getAttendanceCalendar(id, now.getMonth() + 1, now.getFullYear()),
      pending: this.requestService.getSubmittedRequests(1, 1, 'SubmittedAt', true, {
        search: String(id),
        overallStatus: 'Pending',
      }),
      mine: this.requestService.getSubmittedRequests(1, 5, 'SubmittedAt', true, {
        search: String(id),
      }),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ points, attendance, pending, mine }) => {
          this.points.set(points);
          this.attendance.set(attendance);
          this.pendingCount.set(pending.totalCount);
          this.myRequests.set(mine.items);
          this.loading.set(false);
        },
        error: () => {
          this.error.set(true);
          this.loading.set(false);
        },
      });
  }

  requestStatus(status: string | null | undefined): RequestStatusView {
    return REQUEST_STATUS_VIEW[(status ?? '').toLowerCase()] ?? REQUEST_STATUS_VIEW['pending'];
  }
}
