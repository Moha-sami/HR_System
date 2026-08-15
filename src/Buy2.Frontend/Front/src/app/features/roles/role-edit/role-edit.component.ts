import { Component, Input, type OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { RoleService } from '../services/role.service';
import { PermissionGroupComponent } from '@app/features/roles/components/permission-group/permission-group.component';
import { AccessTypeTabsComponent } from '@app/features/roles/components/access-type-tabs/access-type-tabs.component';
import { PERMISSION_GROUPS } from '../models/permission';
import { FormsModule } from '@angular/forms';
import type { Role } from '../models/role';
import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { ModalHeaderComponent } from '@app/shared/components/modal/modal-header.component';
import { ModalBodyComponent } from '@app/shared/components/modal/modal-body.component';
import { ModalFooterComponent } from '@app/shared/components/modal/modal-footer.component';
import { ButtonComponent } from '@app/shared/components/button/button.component';

@Component({
  selector: 'app-role-edit',
  standalone: true,
  imports: [
    FormsModule,
    PermissionGroupComponent,
    AccessTypeTabsComponent,
    ModalComponent,
    ModalHeaderComponent,
    ModalBodyComponent,
    ModalFooterComponent,
    ButtonComponent,
  ],
  templateUrl: './role-edit.component.html',
})
export class RoleEditComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly roleService = inject(RoleService);

  @Input({ required: true }) id!: string;

  readonly PERMISSION_GROUPS = PERMISSION_GROUPS;

  roleName = '';
  description = '';
  permissions: string[] = [];
  showSuccessModal = signal(false);

  ngOnInit(): void {
    this.roleService.get(this.id).subscribe({
      next: (role: Role) => this.loadRole(role),
      error: (err: unknown) => console.error('Failed to load role:', err),
    });
  }

  private loadRole(role: Role): void {
    this.roleName = role.roleName;
    this.description = ''; // No description in backend yet
    this.permissions = role.permissions || [];
  }

  onDiscard(): void {
    this.router.navigate(['/roles']);
  }

  onSubmit(): void {
    throw new Error(`Update endpoint not implemented yet. Role: ${this.id} (${this.roleName})`);
  }

  onSuccessClose(): void {
    this.showSuccessModal.set(false);
    this.router.navigate(['/roles']);
  }
}
