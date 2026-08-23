import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HasPermissionDirective } from '../../../core/directives/has-permission.directive';
import { ButtonComponent } from '../../../shared/components/button/button.component';

@Component({
  selector: 'app-has-permission-docs',
  standalone: true,
  imports: [RouterLink, HasPermissionDirective, ButtonComponent],
  templateUrl: './has-permission-docs.component.html',
  styleUrl: './has-permission-docs.component.css',
})
export class HasPermissionDocsComponent {
  // Mock user permissions for demo
  currentPermissions = ['employee.add', 'employee.edit', 'site.view', 'rewards.inventory'];

  hasPermission(perm: string): boolean {
    return this.currentPermissions.includes(perm);
  }

  hasAnyPermission(perms: string[]): boolean {
    return perms.some((p) => this.currentPermissions.includes(p));
  }

  hasAllPermissions(perms: string[]): boolean {
    return perms.every((p) => this.currentPermissions.includes(p));
  }

  // Demo state
  demoUserRole = 'Facilitator';
  demoPermissions = ['employee.add', 'employee.edit', 'site.view', 'rewards.inventory'];

  setDemoRole(role: 'Super Admin' | 'Facilitator' | 'Manager'): void {
    this.demoUserRole = role;
    this.demoPermissions = this.getMockPermissions(role);
  }

  private getMockPermissions(role: string): string[] {
    switch (role) {
      case 'Super Admin':
        return [
          'employee.add',
          'employee.edit',
          'employee.delete',
          'employee.suspend',
          'employee.admin_access',
          'job.add',
          'job.edit',
          'job.delete',
          'site.add',
          'site.edit',
          'site.delete',
          'site.shifts',
          'points.add_transaction',
          'points.automation',
          'points.view_transactions',
          'notifications.send',
          'rewards.add',
          'rewards.edit',
          'rewards.delete',
          'rewards.inventory',
        ];
      case 'Facilitator':
        return ['site.edit', 'site.shifts', 'rewards.add', 'rewards.inventory'];
      case 'Manager':
        return [
          'employee.add',
          'employee.edit',
          'job.add',
          'job.edit',
          'site.add',
          'site.edit',
          'points.add_transaction',
          'notifications.send',
        ];
      default:
        return [];
    }
  }
}
