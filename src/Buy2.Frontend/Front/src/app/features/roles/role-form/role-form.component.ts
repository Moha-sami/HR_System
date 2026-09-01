import { Component, inject, signal, type OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { RoleService } from '../services/role.service';
import { PERMISSION_GROUPS, type PermissionGroup } from '../models/permission';
import { FormsModule } from '@angular/forms';
import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { ModalHeaderComponent } from '@app/shared/components/modal/modal-header.component';
import { ModalBodyComponent } from '@app/shared/components/modal/modal-body.component';
import { ModalFooterComponent } from '@app/shared/components/modal/modal-footer.component';
import { ButtonComponent } from '@app/shared/components/button/button.component';
import { CommonModule } from '@angular/common';
import {
  DropdownComponent,
  type DropdownOption,
} from '@app/shared/components/dropdown/dropdown.component';
import type { RoleDetails, CreateRoleInput, ModulePermissionDto } from '../models/role';

@Component({
  selector: 'app-role-form',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ModalComponent,
    ModalHeaderComponent,
    ModalBodyComponent,
    ModalFooterComponent,
    ButtonComponent,
    TranslatePipe,
    DropdownComponent,
  ],
  templateUrl: './role-form.component.html',
})
export class RoleFormComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  protected readonly roleService = inject(RoleService);

  readonly PERMISSION_GROUPS = PERMISSION_GROUPS;

  roleName = '';
  description = '';
  isActive = true;
  permissions: ModulePermissionDto[] = [];
  showSuccessModal = signal(false);

  /** Current mode: 'create' or 'edit' */
  mode: 'create' | 'edit' = 'create';
  /** Role ID in edit mode */
  roleId: number | null = null;
  /** Whether we're loading existing role data */
  loading = signal(false);
  /** Whether reference data is loading */
  get loadingRefData(): boolean {
    return this.roleService.loadingRefData();
  }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.mode = 'edit';
      this.roleId = Number(idParam);
      this.loadRole(this.roleId);
    } else {
      this.mode = 'create';
    }
    this.roleService.loadReferenceData().subscribe();
  }

  private loadRole(id: number): void {
    this.loading.set(true);
    this.roleService.get(id).subscribe({
      next: (role: RoleDetails) => {
        this.roleName = role.name;
        this.description = role.description ?? '';
        this.isActive = role.isActive;
        this.permissions = [...role.permissions];
        this.loading.set(false);
      },
      error: (err: unknown) => {
        console.error('Failed to load role:', err);
        this.loading.set(false);
      },
    });
  }

  /** Translation key prefix based on mode */
  get prefix(): string {
    return this.mode === 'create' ? 'ROLES.CREATE' : 'ROLES.EDIT';
  }

  /** Submit button text key */
  get submitText(): string {
    return this.mode === 'create' ? 'SAVE' : 'UPDATE';
  }

  /** Success modal title key */
  get successTitle(): string {
    return this.mode === 'create' ? 'SUCCESS_TITLE' : 'SUCCESS_TITLE';
  }

  /** Success modal message key */
  get successMessage(): string {
    return this.mode === 'create' ? 'SUCCESS_MESSAGE' : 'SUCCESS_MESSAGE';
  }

  /** Success modal OK key */
  get successOk(): string {
    return this.mode === 'create' ? 'SUCCESS_OK' : 'SUCCESS_OK';
  }

  /** Get permissions for a specific group/module */
  getGroupPermissions(groupId: string): ModulePermissionDto | undefined {
    return this.permissions.find((p) => p.module === groupId);
  }

  /** Check if an action is enabled for a group */
  isActionEnabled(groupId: string, action: string): boolean {
    const perm = this.getGroupPermissions(groupId);
    return perm?.actions?.includes(action) ?? false;
  }

  /** Toggle an action for a group */
  toggleAction(groupId: string, action: string): void {
    const groupPerm = this.getGroupPermissions(groupId);
    const currentActions = groupPerm?.actions ?? [];
    const updatedActions = currentActions.includes(action)
      ? currentActions.filter((a) => a !== action)
      : [...currentActions, action];

    this.findOrCreateModulePerm(groupId);
    this.permissions = this.permissions.map((p) =>
      p.module === groupId
        ? { ...p, actions: updatedActions.length > 0 ? updatedActions : null }
        : p,
    );
  }

  /** Get selected access type for a group */
  getAccessType(groupId: string): string | undefined {
    const perm = this.getGroupPermissions(groupId);
    return perm?.scope?.scopeType?.toLowerCase();
  }

  /** Get selected scope target IDs for a group */
  getScopeTargetIds(groupId: string): readonly number[] {
    const perm = this.getGroupPermissions(groupId);
    return perm?.scope?.targetIds ?? [];
  }

  /** Get scope options based on access type and group scope */
  getScopeOptions(group: PermissionGroup, accessType: string): DropdownOption[] {
    if (accessType === 'all' || !group.access) return [];

    // Map access type to the correct API data
    switch (accessType) {
      case 'department':
        return this.roleService.departments().map((d) => ({ id: d.id, name: d.name }));
      case 'region':
        return this.roleService.regions().map((r) => ({ id: r.id, name: r.name }));
      case 'sites':
      case 'specific':
        return this.roleService.sites().map((s) => ({ id: s.id, name: s.siteName }));
      case 'team':
      case 'teams':
        // TODO: Implement when teams API is available
        return [];
      default:
        return [];
    }
  }

  /** Get currently selected scope target IDs for display */
  getSelectedScopeNames(group: PermissionGroup): string[] {
    const targetIds = this.getScopeTargetIds(group.id);
    const accessType = this.getAccessType(group.id);
    const options = this.getScopeOptions(group, accessType ?? '');

    return targetIds
      .map((id) => options.find((o) => o.id === id)?.name)
      .filter((n): n is string => !!n);
  }

  /** Handle access type change */
  onAccessTypeChange(group: PermissionGroup, type: string): void {
    if (!group.access) return;

    const groupId = group.id;
    const groupPerm = this.getGroupPermissions(groupId);
    const currentTargetIds = groupPerm?.scope?.targetIds ?? [];

    this.findOrCreateModulePerm(groupId);
    this.permissions = this.permissions.map((p) =>
      p.module === groupId
        ? {
            ...p,
            scope: {
              scopeType: type.charAt(0).toUpperCase() + type.slice(1),
              targetIds: type === 'all' ? null : currentTargetIds,
            },
          }
        : p,
    );
  }

  /** Handle scope selection change */
  onScopeChange(
    group: PermissionGroup,
    selectedIds: number | string | readonly (number | string)[],
  ): void {
    const groupId = group.id;
    const ids = Array.isArray(selectedIds) ? selectedIds : [selectedIds];

    this.findOrCreateModulePerm(groupId);
    this.permissions = this.permissions.map((p) =>
      p.module === groupId
        ? {
            ...p,
            scope: {
              scopeType: p.scope?.scopeType ?? 'All',
              targetIds: ids.length > 0 ? ids.map(Number) : null,
            },
          }
        : p,
    );
  }

  /** Find or create module permission entry */
  private findOrCreateModulePerm(groupId: string): ModulePermissionDto[] {
    const existing = this.permissions.find((p) => p.module === groupId);
    if (existing) return [...this.permissions];
    return [...this.permissions, { module: groupId, actions: [], scope: null }];
  }

  onDiscard(): void {
    this.router.navigate(['/roles']);
  }

  onSubmit(): void {
    if (!this.roleName) return;

    const input: CreateRoleInput = {
      name: this.roleName,
      description: this.description || null,
      isActive: this.isActive,
      permissions: this.permissions,
    };

    if (this.mode === 'create') {
      this.roleService.create(input).subscribe({
        next: () => this.showSuccessModal.set(true),
        error: (err: unknown) => console.error('Create role failed:', err?.toString()),
      });
    } else {
      this.roleService.update(this.roleId!, input).subscribe({
        next: () => this.showSuccessModal.set(true),
        error: (err: unknown) => console.error('Update role failed:', err?.toString()),
      });
    }
  }

  onSuccessClose(): void {
    this.showSuccessModal.set(false);
    this.router.navigate(['/roles']);
  }
}
