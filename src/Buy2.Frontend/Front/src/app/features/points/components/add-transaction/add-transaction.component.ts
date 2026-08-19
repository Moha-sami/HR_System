import { Component, computed, HostListener, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';

import { ButtonComponent } from '../../../../shared/components/button/button.component';
import { ModalComponent } from '../../../../shared/components/modal/modal.component';
import { ModalBodyComponent } from '../../../../shared/components/modal/modal-body.component';
import type { PointsEmployee } from '../../models/points-transaction';
import { PointsManagementService } from '../../service/points-management.service';

@Component({
  selector: 'app-add-transaction',
  standalone: true,
  imports: [
    FormsModule,
    TranslatePipe,
    ButtonComponent,
    ModalComponent,
    ModalBodyComponent,
  ],
  templateUrl: './add-transaction.component.html',
  styleUrl: './add-transaction.component.css',
})
export class AddTransactionComponent {
  private readonly router = inject(Router);
  private readonly pointsService = inject(PointsManagementService);

  readonly employees = toSignal(this.pointsService.getEmployees(), {
    initialValue: [] as PointsEmployee[],
  });

  readonly employeeSearch = signal('');
  readonly selectedEmployeeId = signal<string | null>(null);
  readonly transactionType = signal<'Add' | 'Deduct'>('Add');
  readonly pointsValue = signal<number | null>(null);
  readonly comments = signal('');
  readonly showEmployeeDropdown = signal(false);
  readonly showSuccessModal = signal(false);
  readonly submitting = signal(false);

  readonly filteredEmployees = computed(() => {
    const term = this.employeeSearch().trim().toLowerCase();
    const employees = this.employees();

    if (!term) {
      return employees.slice(0, 8);
    }

    return employees
      .filter((employee) => {
        const fullName = `${employee.firstName} ${employee.lastName}`.toLowerCase();
        return fullName.includes(term) || employee.id.toLowerCase().includes(term);
      })
      .slice(0, 8);
  });

  readonly canSubmit = computed(() => {
    const points = Number(this.pointsValue());
    return (
      !!this.selectedEmployeeId() &&
      Number.isFinite(points) &&
      points > 0 &&
      !this.submitting()
    );
  });

  @HostListener('document:click')
  onDocumentClick(): void {
    this.showEmployeeDropdown.set(false);
  }

  openEmployeeDropdown(event: Event): void {
    event.stopPropagation();
    this.showEmployeeDropdown.set(true);
  }

  onEmployeeSearchChange(value: string): void {
    this.employeeSearch.set(value);

    const selectedId = this.selectedEmployeeId();
    if (selectedId) {
      const selected = this.employees().find((employee) => employee.id === selectedId);
      const selectedName = selected
        ? `${selected.firstName} ${selected.lastName}`
        : '';

      if (value !== selectedName) {
        this.selectedEmployeeId.set(null);
      }
    }

    this.showEmployeeDropdown.set(true);
  }

  selectEmployee(employee: PointsEmployee, event: Event): void {
    event.stopPropagation();
    this.selectedEmployeeId.set(employee.id);
    this.employeeSearch.set(`${employee.firstName} ${employee.lastName}`);
    this.showEmployeeDropdown.set(false);
  }

  employeeFullName(employee: PointsEmployee): string {
    return `${employee.firstName} ${employee.lastName}`;
  }

  onDiscard(): void {
    void this.router.navigate(['/points']);
  }

  onSubmit(): void {
    const employeeId = this.selectedEmployeeId();
    const pointsValue = this.pointsValue();

    if (!this.canSubmit() || !employeeId || pointsValue == null) {
      return;
    }

    this.submitting.set(true);

    this.pointsService
      .createTransaction({
        employeeId,
        pointsValue: Number(pointsValue),
        type: this.transactionType(),
        comments: this.comments(),
      })
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.showSuccessModal.set(true);
        },
        error: (err: unknown) => {
          this.submitting.set(false);
          console.error('Create transaction failed:', err);
        },
      });
  }

  onSuccessClose(): void {
    this.showSuccessModal.set(false);
    void this.router.navigate(['/points']);
  }
}
