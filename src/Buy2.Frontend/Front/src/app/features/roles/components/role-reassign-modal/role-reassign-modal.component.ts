import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';

import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { ModalHeaderComponent } from '@app/shared/components/modal/modal-header.component';
import { ModalBodyComponent } from '@app/shared/components/modal/modal-body.component';
import { ModalFooterComponent } from '@app/shared/components/modal/modal-footer.component';
import { ButtonComponent } from '@app/shared/components/button/button.component';
import type { DeletionImpact, AffectedEmployee } from '../../models/role';

@Component({
  selector: 'app-role-reassign-modal',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslatePipe,
    ModalComponent,
    ModalHeaderComponent,
    ModalBodyComponent,
    ModalFooterComponent,
    ButtonComponent,
  ],
  templateUrl: './role-reassign-modal.component.html',
})
export class RoleReassignModalComponent {
  @Input({ required: true }) impact!: DeletionImpact;
  @Input({ required: true }) availableRoles!: readonly { id: number; name: string }[];
  @Output() confirm = new EventEmitter<{
    defaultNewRoleId: number | null;
    reassignments: Array<{ employeeId: number; newRoleId: number }>;
  }>();
  @Output() cancel = new EventEmitter<void>();

  loading = signal(false);
  error = signal<string | null>(null);

  /** Default role for all unassigned employees */
  defaultNewRoleId: number | null = null;

  /** Individual reassignments: employeeId -> newRoleId */
  reassignments = signal<Record<number, number>>({});

  get affectedEmployees(): readonly AffectedEmployee[] {
    return this.impact?.affectedEmployees ?? [];
  }

  get canConfirm(): boolean {
    // All employees must have a role assigned (either default or individual)
    return this.affectedEmployees.every(
      (emp) => this.reassignments()[emp.employeeId] || this.defaultNewRoleId,
    );
  }

  onEmployeeRoleChange(employeeId: number, roleId: string | number): void {
    const id = typeof roleId === 'string' ? Number(roleId) : roleId;
    this.reassignments.update((current) => ({
      ...current,
      [employeeId]: id,
    }));
  }

  onDefaultRoleChange(roleId: string | number): void {
    this.defaultNewRoleId = typeof roleId === 'string' ? Number(roleId) : roleId;
  }

  onConfirm(): void {
    if (!this.canConfirm) return;

    this.loading.set(true);
    this.error.set(null);

    const reassignments: Array<{ employeeId: number; newRoleId: number }> = this.affectedEmployees
      .map((emp) => {
        const assignedRoleId = this.reassignments()[emp.employeeId] ?? this.defaultNewRoleId;
        return assignedRoleId ? { employeeId: emp.employeeId, newRoleId: assignedRoleId } : null;
      })
      .filter((r): r is { employeeId: number; newRoleId: number } => r !== null);

    this.confirm.emit({
      defaultNewRoleId: this.defaultNewRoleId,
      reassignments,
    });
  }

  onCancel(): void {
    this.cancel.emit();
  }
}
