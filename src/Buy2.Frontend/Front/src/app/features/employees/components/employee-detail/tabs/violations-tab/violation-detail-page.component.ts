import { Component, computed, inject, signal, type OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonComponent } from '@app/shared/components/button/button.component';
import { EmployeeDetailService } from '../../../../services/employee-detail.service';
import type { ResolveViolationDto } from '../../../../models/view-employee/employee-violations';

@Component({
  selector: 'app-violation-detail-page',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe, ButtonComponent],
  templateUrl: './violation-detail-page.component.html',
})
export class ViolationDetailPageComponent implements OnInit {
  private readonly employeeDetailService = inject(EmployeeDetailService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  // Use service signals directly
  readonly violation = this.employeeDetailService.violationDetail;
  readonly loading = this.employeeDetailService.violationDetailLoading;
  readonly error = this.employeeDetailService.violationDetailError;

  readonly resolving = signal(false);
  readonly resolveError = signal<string | null>(null);

  // Resolve form
  readonly actionType = signal('');
  readonly actionDescription = signal('');
  readonly actionDate = signal('');
  readonly actionTakenById = signal<number | null>(null);

  // Computed severity class
  readonly severityClass = computed(() => {
    const v = this.violation();
    if (!v) return '';
    switch (v.severity.toLowerCase()) {
      case 'low':
        return 'bg-green-100 text-green-700';
      case 'medium':
        return 'bg-yellow-100 text-yellow-700';
      case 'high':
        return 'bg-orange-100 text-orange-700';
      case 'critical':
        return 'bg-red-100 text-red-700';
      default:
        return 'bg-gray-100 text-gray-700';
    }
  });

  // Computed status class
  readonly statusClass = computed(() => {
    const v = this.violation();
    if (!v) return '';
    switch (v.status.toLowerCase()) {
      case 'pending':
        return 'bg-gray-100 text-gray-700';
      case 'approved':
        return 'bg-blue-100 text-blue-700';
      case 'rejected':
        return 'bg-red-100 text-red-700';
      case 'resolved':
        return 'bg-green-100 text-green-700';
      case 'underinvestigation':
        return 'bg-purple-100 text-purple-700';
      default:
        return 'bg-gray-100 text-gray-700';
    }
  });

  ngOnInit(): void {
    // Load from snapshot initially
    this.loadFromSnapshot();

    // Subscribe to param changes for navigation within same component
    this.route.paramMap.subscribe((params) => {
      const violId = Number(params.get('violationId'));
      if (violId) {
        const empId = Number(this.route.parent?.snapshot.paramMap.get('id'));
        if (empId) {
          this.employeeDetailService.loadViolationDetail(empId, violId);
        }
      }
    });
  }

  private loadFromSnapshot(): void {
    const violationId = Number(this.route.snapshot.paramMap.get('violationId'));
    const employeeId = Number(this.route.parent?.snapshot.paramMap.get('id'));
    if (employeeId && violationId) {
      this.employeeDetailService.loadViolationDetail(employeeId, violationId);
    }
  }

  // Go back to violations tab
  goBack(): void {
    const empId = Number(this.route.parent?.snapshot.paramMap.get('id'));
    if (empId) {
      this.router.navigate(['/employees', empId, 'violations']);
    }
  }

  // Handle resolve
  onResolve(): void {
    const v = this.violation();
    const empId = Number(this.route.parent?.snapshot.paramMap.get('id'));
    if (!v || !empId || this.resolving()) return;

    this.resolving.set(true);
    this.resolveError.set(null);

    const payload: ResolveViolationDto = {
      actionType: this.actionType(),
      actionDescription: this.actionDescription(),
      actionDate: this.actionDate() || undefined,
      actionTakenById: this.actionTakenById() || undefined,
    };

    this.employeeDetailService.resolveViolationAction(empId, v.id, payload).subscribe({
      next: () => {
        this.resolving.set(false);
        this.employeeDetailService.loadViolationDetail(empId, v.id);
      },
      error: () => {
        this.resolving.set(false);
        this.resolveError.set('EMPLOYEE_DETAIL.VIOLATION_RESOLVE_FAILED');
      },
    });
  }

  // Format date for display
  formatDate(isoString: string): string {
    const date = new Date(isoString);
    return date.toLocaleDateString('en-US', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
    });
  }

  // Check if can resolve (not already resolved)
  canResolve(): boolean {
    const v = this.violation();
    return v !== null && v.status.toLowerCase() !== 'resolved';
  }
}
