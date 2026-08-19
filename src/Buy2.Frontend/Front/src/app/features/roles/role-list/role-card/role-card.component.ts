import { Component, Input, inject, HostListener } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { RoleService } from '../../services/role.service';
import type { Role } from '../../models/role';

@Component({
  selector: 'app-role-card',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './role-card.component.html',
})
export class RoleCardComponent {
  private readonly roleService = inject(RoleService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);

  @Input({ required: true }) role!: Role;
  menuOpen = false;

  get permissionCount(): number {
    return this.role.permissions.length;
  }

  onEdit(): void {
    this.menuOpen = false;
    this.router.navigate(['/roles/edit', this.role.id]);
  }

  onDelete(): void {
    this.menuOpen = false;
    this.translate
      .get('ROLES.CARD.DELETE_CONFIRM', { name: this.role.roleName })
      .subscribe((message) => {
        if (confirm(message)) {
          this.roleService.remove(this.role.id).subscribe({
            next: () => console.log('Role deleted'),
            error: (err) => console.error('Delete failed:', err),
          });
        }
      });
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
