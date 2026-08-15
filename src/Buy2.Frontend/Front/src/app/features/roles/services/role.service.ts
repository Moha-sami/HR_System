import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { type Observable, of, catchError, tap } from 'rxjs';
import { environment } from '../../../../environments/environment';
import type { Role } from '../models/role';

const ROLES_API = `${environment.baseUrl}/roles`;

export interface NewRoleInput {
  readonly roleName: string;
  readonly permissions: readonly string[];
}

/**
 * RBAC role service. Wraps /api/v1/roles.
 * Backend status: GET missing → loadAll() falls back to MOCK_ROLES + sets mockMode flag.
 *                  PUT missing → update() throws until the endpoint ships.
 * POST + DELETE are live. Wire shape = Role (identity).
 */
@Injectable({ providedIn: 'root' })
export class RoleService {
  private readonly http = inject(HttpClient);

  readonly roles = signal<readonly Role[]>([]);
  readonly mockMode = signal(false);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  /** GET /api/v1/roles — falls back to MOCK_ROLES until backend ships list endpoint. */
  loadAll(): void {
    if (this.loading()) {
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    this.http
      .get<readonly Role[]>(ROLES_API)
      .pipe(
        catchError(() => {
          this.mockMode.set(true);
          return of(MOCK_ROLES);
        }),
      )
      .subscribe({
        next: (roles) => {
          this.roles.set(roles);
          this.loading.set(false);
        },
        error: (err) => this.handleLoadError(err),
      });
  }

  /** GET /api/v1/roles/:id — falls back to MOCK_ROLES until backend ships get endpoint. */
  get(id: string): Observable<Role> {
    return this.http
      .get<Role>(`${ROLES_API}/${id}`)
      .pipe(catchError(() => of(MOCK_ROLES.find((r) => r.id === +id) || MOCK_ROLES[0])));
  }

  /** POST /api/v1/roles — backend CreateRole endpoint accepts {roleName, permissions}. */
  create(input: NewRoleInput): Observable<Role> {
    return this.http
      .post<Role>(ROLES_API, input)
      .pipe(tap((created) => this.roles.update((list) => [...list, created])));
  }

  /** DELETE /api/v1/roles/:id — DeleteRole API exists on the backend. */
  remove(id: number): Observable<void> {
    return this.http
      .delete<void>(`${ROLES_API}/${id}`)
      .pipe(tap(() => this.roles.update((list) => list.filter((r) => r.id !== id))));
  }

  /**
   * Update role — backend PUT is NOT YET IMPLEMENTED. Surface explicit failure so the UI
   * can't be silently confused for a real server round-trip.
   */
  update(id: number, input: NewRoleInput): Observable<Role> {
    throw new Error(
      `UpdateRole endpoint is not implemented on the backend yet. ` +
        `Attempted update for role id=${id}, name="${input.roleName}".`,
    );
  }

  private handleLoadError(err: unknown): void {
    this.error.set(toErrorMessage(err));
    this.roles.set(MOCK_ROLES);
    this.mockMode.set(true);
    this.loading.set(false);
  }
}

function toErrorMessage(err: unknown): string {
  if (err instanceof Error) {
    return err.message;
  }
  return 'Unknown error';
}

export const MOCK_ROLES: readonly Role[] = [
  {
    id: 1,
    roleName: 'Super Admin',
    permissions: ['employee.add', 'employee.edit', 'employee.admin_access'],
  },
  {
    id: 2,
    roleName: 'Facilitator',
    permissions: ['site.edit', 'site.shifts', 'rewards.add', 'rewards.inventory'],
  },
] as const;
