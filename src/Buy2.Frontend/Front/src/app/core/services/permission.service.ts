import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TokenService } from '../auth/token.service';

const ROLES_API = `${environment.baseUrl}/roles`;

@Injectable({ providedIn: 'root' })
export class PermissionService {
  private readonly http = inject(HttpClient);
  private readonly tokenSvc = inject(TokenService);

  private readonly rolePermissionsCache = signal<Map<string, string[]>>(new Map());
  private readonly loadingRole = signal<string | null>(null);

  readonly currentUserRole = computed(() => this.tokenSvc.user()?.role ?? '');

  readonly permissions = computed(() => {
    const role = this.currentUserRole();
    if (!role) return [] as string[];
    const cached = this.rolePermissionsCache().get(role);
    return cached ?? [];
  });

  hasPermission(permission: string): boolean {
    const perms = this.permissions();
    return perms.includes(permission);
  }

  hasAnyPermission(permissions: string[]): boolean {
    const perms = this.permissions();
    return permissions.some((p) => perms.includes(p));
  }

  hasAllPermissions(permissions: string[]): boolean {
    const perms = this.permissions();
    return permissions.every((p) => perms.includes(p));
  }

  async refresh(): Promise<void> {
    const role = this.currentUserRole();
    if (!role) return;
    if (this.loadingRole() === role) return;
    if (this.rolePermissionsCache().has(role)) return;

    this.loadingRole.set(role);
    try {
      const roleData = await this.fetchRoleByName(role);
      this.rolePermissionsCache.update((map) => {
        const next = new Map(map);
        next.set(role, roleData.permissions);
        return next;
      });
    } finally {
      this.loadingRole.set(null);
    }
  }

  clear(): void {
    this.rolePermissionsCache.set(new Map());
  }

  invalidateRole(role: string): void {
    this.rolePermissionsCache.update((map) => {
      const next = new Map(map);
      next.delete(role);
      return next;
    });
  }

  private async fetchRoleByName(name: string): Promise<{ permissions: string[] }> {
    try {
      return await firstValueFrom(
        this.http.get<{ id: number; roleName: string; permissions: string[] }>(
          `${ROLES_API}/by-name/${encodeURIComponent(name)}`,
        ),
      );
    } catch (err) {
      throw new Error(
        `Failed to fetch permissions for role "${name}". ` +
          `Ensure GET /api/v1/roles/by-name/:name exists on backend. ` +
          `Original error: ${err instanceof Error ? err.message : String(err)}`,
      );
    }
  }
}
