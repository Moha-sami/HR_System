import { Directive, inject, input, TemplateRef, ViewContainerRef, effect } from '@angular/core';
import { PermissionService } from '../services/permission.service';

export type PermissionInput = string | string[] | { any?: string[]; all?: string[] };

@Directive({
  selector: '[hasPermission]',
  standalone: true,
})
export class HasPermissionDirective {
  private readonly permissionSvc = inject(PermissionService);
  private readonly viewContainer = inject(ViewContainerRef);
  private readonly templateRef = inject(TemplateRef);
  private hasView = false;

  readonly hasPermission = input<PermissionInput>('', { alias: 'hasPermission' });
  readonly hasPermissionElse = input<TemplateRef<unknown> | null>(null, {
    alias: 'hasPermissionElse',
  });

  constructor() {
    effect(() => {
      this.updateView();
    });
  }

  private updateView(): void {
    const perm = this.hasPermission();
    const hasAccess = this.checkPermission(perm);

    if (hasAccess && !this.hasView) {
      this.viewContainer.clear();
      this.viewContainer.createEmbeddedView(this.templateRef);
      this.hasView = true;
    } else if (!hasAccess && this.hasView) {
      this.viewContainer.clear();
      if (this.hasPermissionElse()) {
        this.viewContainer.createEmbeddedView(this.hasPermissionElse()!);
      }
      this.hasView = false;
    }
  }

  private checkPermission(perm: PermissionInput): boolean {
    if (typeof perm === 'string') {
      return this.permissionSvc.hasPermission(perm);
    }
    if (Array.isArray(perm)) {
      return this.permissionSvc.hasAnyPermission(perm);
    }
    if (perm && typeof perm === 'object') {
      if (perm.all && perm.all.length > 0) {
        return this.permissionSvc.hasAllPermissions(perm.all);
      }
      if (perm.any && perm.any.length > 0) {
        return this.permissionSvc.hasAnyPermission(perm.any);
      }
    }
    return false;
  }
}
