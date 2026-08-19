import { Component, EventEmitter, Input, Output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import type { PermissionGroup } from '../../models/permission';
import { AccessTypeTabsComponent } from '../access-type-tabs/access-type-tabs.component';

@Component({
  selector: 'app-permission-group',
  standalone: true,
  templateUrl: './permission-group.component.html',
  imports: [AccessTypeTabsComponent, TranslatePipe],
})
export class PermissionGroupComponent {
  @Input({ required: true }) group!: PermissionGroup;
  @Input({ required: true }) permissions: readonly string[] = [];
  @Output() permissionsChange = new EventEmitter<string[]>();

  isActive(toggle: string): boolean {
    return this.permissions.includes(this.permissionKey(toggle));
  }

  togglePermission(toggle: string): void {
    const key = this.permissionKey(toggle);
    const updated = this.permissions.includes(key)
      ? this.permissions.filter((p) => p !== key)
      : [...this.permissions, key];
    this.permissionsChange.emit(updated);
  }

  private permissionKey(toggle: string): string {
    return `${this.group.id}.${toggle}`;
  }

  toggleLabel(toggle: string): string {
    return toggle
      .split('_')
      .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ');
  }
}
