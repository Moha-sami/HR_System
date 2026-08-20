import { Component, EventEmitter, Input, Output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import type { PermissionGroup } from '../../models/permission';

@Component({
  selector: 'app-access-type-tabs',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './access-type-tabs.component.html',
})
export class AccessTypeTabsComponent {
  @Input({ required: true }) group!: PermissionGroup;
  @Input({ required: true }) permissions: readonly string[] = [];
  @Output() permissionsChange = new EventEmitter<string[]>();

  isTypeActive(type: string): boolean {
    const key = this.accessTypeKey(type);
    return this.permissions.includes(key);
  }

  setAccessType(type: string): void {
    const scope = this.group.access!.scope;
    // Remove all existing access type tokens for this scope
    const withoutAccessTypes = this.permissions.filter(
      (p) => !p.startsWith(`${scope}.access.type.`),
    );
    // Remove all group tokens for this scope
    const withoutGroups = withoutAccessTypes.filter((p) => !p.startsWith(`${scope}.group.`));
    // Add the new access type token
    const updated = [...withoutGroups, this.accessTypeKey(type)];
    this.permissionsChange.emit(updated);
  }

  private accessTypeKey(type: string): string {
    return `${this.group.access!.scope}.access.type.${type}`;
  }

  typeLabel(type: string): string {
    return type.charAt(0).toUpperCase() + type.slice(1);
  }

  openGroupsModal(): void {
    // Stub: Unit 5 will wire the groups modal
    console.warn('[AccessTypeTabsComponent] Groups modal not implemented yet');
  }
}
