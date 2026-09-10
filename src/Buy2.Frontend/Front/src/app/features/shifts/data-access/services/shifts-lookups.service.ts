import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import type { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import type {
  ShiftsEmployeesPage,
  ShiftsJobRoleLookup,
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

  getJobRoles(): Observable<ShiftsJobRoleLookup[]> {
    return this.http.get<ShiftsJobRoleLookup[]>(`${API_BASE}/job-roles`);
  }

  getSiteEmployees(siteId: number): Observable<ShiftsSiteEmployee[]> {
    return this.http.get<ShiftsSiteEmployee[]>(`${API_BASE}/sites/${siteId}/employees`);
  }

  getEmployeesPage(page: number, pageSize: number): Observable<ShiftsEmployeesPage> {
    const params = new HttpParams().set('page', String(page)).set('pageSize', String(pageSize));
    return this.http.get<ShiftsEmployeesPage>(`${API_BASE}/employees`, { params });
  }
}
