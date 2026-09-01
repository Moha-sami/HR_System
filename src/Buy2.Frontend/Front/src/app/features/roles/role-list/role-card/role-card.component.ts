import { Component, Input, inject, HostListener } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { RoleService } from '../../services/role.service';
import type { RoleListItem, DeletionImpact } from '../../models/role';
import { RoleReassignModalComponent } from '../../components/role-reassign-modal/role-reassign-modal.component';

@Component({
  selector: 'app-role-card',
  standalone: true,
  imports: [TranslatePipe, RoleReassignModalComponent],
  templateUrl: './role-card.component.html',
})
export class RoleCardComponent {
  private readonly roleService = inject(RoleService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);

  @Input({ required: true }) role!: RoleListItem;
  menuOpen = false;
  showReassignModal = false;
  deletionImpact: DeletionImpact | null = null;
  availableRoles: readonly { id: number; name: string }[] = [];

  get permissionCount(): number {
    return this.role.permissionsSummary.length;
  }

  onEdit(): void {
    this.menuOpen = false;
    this.router.navigate(['/roles/edit', this.role.id]);
  }

  onDelete(): void {
    this.menuOpen = false;
    this.loadDeletionImpact();
  }

  private loadDeletionImpact(): void {
    this.roleService.getDeletionImpact(this.role.id).subscribe({
      next: (impact) => {
        this.deletionImpact = impact;
        // Always show the reassignment modal (it handles both cases)
        this.loadAvailableRoles();
        this.showReassignModal = true;
      },
      error: (err) => {
        console.error('Failed to load deletion impact:', err);
        // Fallback to simple confirmation
        this.translate
          .get('ROLES.CARD.DELETE_CONFIRM', { name: this.role.name })
          .subscribe((message) => {
            if (confirm(message)) {
              this.deleteRoleDirectly();
            }
          });
      },
    });
  }

  private loadAvailableRoles(): void {
    // Use roles from RoleService (already loaded via loadAll)
    // Exclude the current role being deleted
    this.availableRoles = this.roleService
      .roles()
      .filter((r) => r.id !== this.role.id)
      .map((r) => ({ id: r.id, name: r.name }));
  }

  private deleteRoleDirectly(): void {
    this.roleService.remove(this.role.id).subscribe({
      next: () => console.log('Role deleted'),
      error: (err) => console.error('Delete failed:', err),
    });
  }

  onReassignConfirm(payload: {
    defaultNewRoleId: number | null;
    reassignments: Array<{ employeeId: number; newRoleId: number }>;
  }): void {
    this.showReassignModal = false;
    this.roleService.reassignAndDelete(this.role.id, payload).subscribe({
      next: (result) => {
        console.log('Role deleted with reassignment:', result);
      },
      error: (err) => console.error('Reassign and delete failed:', err),
    });
  }

  onReassignCancel(): void {
    this.showReassignModal = false;
    this.deletionImpact = null;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.menuOpen) {
      const target = event.target as HTMLElement;
      if (!target.closest('.relative')) {
        this.menuOpen = false;
      }
    }
  }
}
