import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { RoleService } from '../services/role.service';
import { PermissionGroupComponent } from '@app/features/roles/components/permission-group/permission-group.component';
import { PERMISSION_GROUPS } from '../models/permission';
import { FormsModule } from '@angular/forms';
import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { ModalHeaderComponent } from '@app/shared/components/modal/modal-header.component';
import { ModalBodyComponent } from '@app/shared/components/modal/modal-body.component';
import { ModalFooterComponent } from '@app/shared/components/modal/modal-footer.component';
import { ButtonComponent } from '@app/shared/components/button/button.component';

@Component({
  selector: 'app-role-create',
  standalone: true,
  imports: [
    FormsModule,
    PermissionGroupComponent,
    ModalComponent,
    ModalHeaderComponent,
    ModalBodyComponent,
    ModalFooterComponent,
    ButtonComponent,
  ],
  templateUrl: './role-create.component.html',
})
export class RoleCreateComponent {
  private readonly router = inject(Router);
  private readonly roleService = inject(RoleService);

  readonly PERMISSION_GROUPS = PERMISSION_GROUPS;

  roleName = '';
  description = '';
  permissions: string[] = [];
  showSuccessModal = signal(false);

  onDiscard(): void {
    this.router.navigate(['/roles']);
  }

  onSubmit(): void {
    if (!this.roleName) return;

    this.roleService.create({ roleName: this.roleName, permissions: this.permissions }).subscribe({
      next: () => this.showSuccessModal.set(true),
      error: (err: unknown) => console.error('Create role failed:', err?.toString()),
    });
  }

  onSuccessClose(): void {
    this.showSuccessModal.set(false);
    this.router.navigate(['/roles']);
  }
}
