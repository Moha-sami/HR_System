import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import type { Observable } from 'rxjs';
import { forkJoin, map, of } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import type {
  ShiftCandidateEmployee,
  ShiftEmployeesFilter,
  ShiftsCandidateEmployeesPage,
  ShiftsEmployeesPage,
  ShiftsJobRoleLookup,
  ShiftsJobsPage,
  ShiftsSiteEmployee,
  ShiftsSiteLookup,
} from '../models/shifts-lookups.models';

const API_BASE = environment.baseUrl;

/**
 * Single-owner lookup facades for the shifts area. The four scheduling
 * subfeatures consume these instead of building their own loaders.
 */
@Injectable({ providedIn: 'root' })
export class ShiftsLookupsService {
  private readonly http = inject(HttpClient);

  getSites(): Observable<ShiftsSiteLookup[]> {
    return this.http.get<ShiftsSiteLookup[]>(`${API_BASE}/sites`);
  }

  /** Job roles come from the paginated GET /jobs endpoint (no /job-roles route exists). */
  getJobRoles(): Observable<ShiftsJobRoleLookup[]> {
    const params = new HttpParams().set('pageNumber', '1').set('pageSize', '100');
    return this.http
      .get<ShiftsJobsPage>(`${API_BASE}/jobs`, { params })
      .pipe(map((res) => res.items.map((j) => ({ id: j.id, title: j.title }))));
  }

  getSiteEmployees(siteId: number): Observable<ShiftsSiteEmployee[]> {
    return this.http.get<ShiftsSiteEmployee[]>(`${API_BASE}/sites/${siteId}/employees`);
  }

  /**
   * Ticket #329: paged candidate rows for the employee strip.
   * The backend accepts a single SiteId, so one request goes out per
   * selected site and the pages are merged (deduped by id, totals summed).
   */
  getShiftEmployees(filter: ShiftEmployeesFilter): Observable<ShiftsCandidateEmployeesPage> {
    if (filter.siteIds.length === 0) {
      return of({ items: [], totalCount: 0, page: filter.page, pageSize: filter.pageSize });
    }
    const requests = filter.siteIds.map((siteId) => {
      let params = new HttpParams()
        .set('SiteId', String(siteId))
        .set('Page', String(filter.page))
        .set('PageSize', String(filter.pageSize));
      if (filter.search?.trim()) params = params.set('Search', filter.search.trim());
      for (const roleId of filter.roleIds ?? []) params = params.append('RoleIds', String(roleId));
      for (const tier of filter.ratingTiers ?? []) params = params.append('RatingTiers', tier);
      if (filter.isPreferredOnly === true) params = params.set('IsPreferredOnly', 'true');
      return this.http.get<ShiftsCandidateEmployeesPage>(`${API_BASE}/shifts/employees`, { params });
    });
    return forkJoin(requests).pipe(
      map((pages) => {
        const seen = new Map<number, ShiftCandidateEmployee>();
        let totalCount = 0;
        for (const page of pages) {
          totalCount += page.totalCount;
          for (const emp of page.items) seen.set(emp.id, emp);
        }
        return {
          items: [...seen.values()],
          totalCount,
          page: filter.page,
          pageSize: filter.pageSize,
        };
      }),
    );
  }

  getEmployeesPage(page: number, pageSize: number): Observable<ShiftsEmployeesPage> {
    const params = new HttpParams().set('page', String(page)).set('pageSize', String(pageSize));
    return this.http.get<ShiftsEmployeesPage>(`${API_BASE}/employees`, { params });
  }
}
