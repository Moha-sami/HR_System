import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of, catchError, tap, throwError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import type {
  RoleListItem,
  RoleDetails,
  RolePaginatedResponse,
  RoleLookupItem,
  CreateRoleInput,
  DeletionImpact,
} from '../models/role';

const ROLES_API = `${environment.baseUrl}/roles`;
const SITES_API = `${environment.baseUrl}/sites`;

@Injectable({ providedIn: 'root' })
export class RoleService {
  private readonly http = inject(HttpClient);

  // State signals
  readonly roles = signal<readonly RoleListItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly totalCount = signal(0);
  readonly pageNumber = signal(1);
  readonly pageSize = signal(10);
  readonly totalPages = signal(0);

  // Reference data state (loaded on demand)
  readonly regions = signal<readonly { id: number; name: string }[]>([]);
  readonly sites = signal<readonly { id: number; siteName: string; regionId?: number }[]>([]);
  readonly departments = signal<readonly { id: number; name: string }[]>([]);
  readonly loadingRefData = signal(false);

  // Computed
  readonly hasRoles = computed(() => this.roles().length > 0);
  readonly isEmpty = computed(() => !this.loading() && this.roles().length === 0);

  /** Load reference data (regions, sites, departments) on demand */
  loadReferenceData(): Observable<void> {
    if (this.regions().length > 0 && this.sites().length > 0 && this.departments().length > 0) {
      return of(void 0);
    }
    if (this.loadingRefData()) {
      return of(void 0);
    }

    this.loadingRefData.set(true);

    return new Observable<void>((observer) => {
      let completed = 0;
      const checkComplete = () => {
        completed++;
        if (completed === 3) {
          this.loadingRefData.set(false);
          observer.next();
          observer.complete();
        }
      };

      this.http.get<readonly { id: number; name: string }[]>(`${SITES_API}/regions`).subscribe({
        next: (regions) => this.regions.set(regions),
        error: () => checkComplete(),
        complete: checkComplete,
      });

      this.http
        .get<readonly { id: number; siteName: string; regionId?: number }[]>(SITES_API)
        .subscribe({
          next: (sites) => this.sites.set(sites),
          error: () => checkComplete(),
          complete: checkComplete,
        });

      this.http
        .get<readonly { id: number; name: string }[]>(`${environment.baseUrl}/departments`)
        .subscribe({
          next: (departments) => this.departments.set(departments),
          error: () => checkComplete(),
          complete: checkComplete,
        });
    });
  }

  /** GET /api/v1/roles — paginated list with filters */
  loadAll(
    options: {
      searchTerm?: string;
      isActive?: boolean;
      pageNumber?: number;
      pageSize?: number;
    } = {},
  ): void {
    if (this.loading()) return;

    this.loading.set(true);
    this.error.set(null);

    let params = new HttpParams();
    if (options.searchTerm) params = params.set('searchTerm', options.searchTerm);
    if (options.isActive !== undefined)
      params = params.set('isActive', options.isActive.toString());
    params = params.set('pageNumber', (options.pageNumber ?? this.pageNumber()).toString());
    params = params.set('pageSize', (options.pageSize ?? this.pageSize()).toString());

    this.http
      .get<RolePaginatedResponse>(ROLES_API, { params })
      .pipe(
        catchError((err) => {
          this.handleLoadError(err);
          return of(null);
        }),
      )
      .subscribe((response) => {
        if (response) {
          this.roles.set(response.items);
          this.totalCount.set(response.totalCount);
          this.pageNumber.set(response.pageNumber);
          this.pageSize.set(response.pageSize);
          this.totalPages.set(response.totalPages);
        }
        this.loading.set(false);
      });
  }

  /** GET /api/v1/roles/{id} — full role details */
  get(id: number): Observable<RoleDetails> {
    return this.http
      .get<RoleDetails>(`${ROLES_API}/${id}`)
      .pipe(catchError((err) => throwError(() => this.toErrorMessage(err))));
  }

  /** GET /api/v1/roles/lookup — lightweight list for dropdowns */
  getLookup(excludeRoleId?: number): Observable<readonly RoleLookupItem[]> {
    let params = new HttpParams();
    if (excludeRoleId !== undefined) params = params.set('excludeRoleId', excludeRoleId.toString());
    return this.http.get<readonly RoleLookupItem[]>(`${ROLES_API}/lookup`, { params });
  }

  /** GET /api/v1/roles/{id}/deletion-impact — check deletion impact */
  getDeletionImpact(id: number): Observable<DeletionImpact> {
    return this.http
      .get<DeletionImpact>(`${ROLES_API}/${id}/deletion-impact`)
      .pipe(catchError((err) => throwError(() => this.toErrorMessage(err))));
  }

  /** POST /api/v1/roles — create new role */
  create(input: CreateRoleInput): Observable<RoleDetails> {
    return this.http
      .post<RoleDetails>(ROLES_API, input)
      .pipe(tap((created) => this.roles.update((list) => [...list, this.toListItem(created)])));
  }

  /** DELETE /api/v1/roles/{id} — delete role */
  remove(id: number): Observable<void> {
    return this.http
      .delete<void>(`${ROLES_API}/${id}`)
      .pipe(tap(() => this.roles.update((list) => list.filter((r) => r.id !== id))));
  }

  /** PUT /api/v1/roles/{id} — update role */
  update(id: number, input: CreateRoleInput): Observable<RoleDetails> {
    return this.http
      .put<RoleDetails>(`${ROLES_API}/${id}`, input)
      .pipe(
        tap((updated) =>
          this.roles.update((list) =>
            list.map((r) => (r.id === id ? this.toListItem(updated) : r)),
          ),
        ),
      );
  }

  /** POST /api/v1/roles/{id}/reassign-and-delete — reassign employees and delete */
  reassignAndDelete(
    id: number,
    payload: {
      defaultNewRoleId?: number | null;
      reassignments?: Array<{ employeeId: number; newRoleId: number }>;
    },
  ): Observable<{
    success: boolean;
    deletedRoleId: number;
    reassignedEmployeesCount: number;
    message: string;
  }> {
    return this.http
      .post<{
        success: boolean;
        deletedRoleId: number;
        reassignedEmployeesCount: number;
        message: string;
      }>(`${ROLES_API}/${id}/reassign-and-delete`, payload)
      .pipe(tap(() => this.roles.update((list) => list.filter((r) => r.id !== id))));
  }

  private toListItem(details: RoleDetails): RoleListItem {
    return {
      id: details.id,
      name: details.name,
      description: details.description,
      assignedEmployeesCount: details.assignedEmployeesCount,
      isSystemRole: details.isSystemRole,
      isActive: details.isActive,
      createdAt: details.createdAt,
      permissionsSummary: details.permissions.map((p) => p.module),
    };
  }

  private handleLoadError(err: unknown): void {
    this.error.set(this.toErrorMessage(err));
    this.roles.set([]);
    this.loading.set(false);
  }

  private toErrorMessage(err: unknown): string {
    if (err instanceof Error) return err.message;
    if (typeof err === 'object' && err !== null && 'message' in err)
      return String((err as any).message);
    return 'Unknown error loading roles';
  }
}
