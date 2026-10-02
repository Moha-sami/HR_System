import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import type { Observable } from 'rxjs';
import { map } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import type {
  ShiftCandidatePreview,
  SiteShiftOverviewCard,
  SiteShiftsOverviewFilter,
  SiteShiftsOverviewPage,
  SiteSmartSettingsUpdate,
} from '../models/shift-management.models';

const API_BASE = environment.baseUrl;

const STATUS_BY_INDEX = ['Covered', 'Shortage', 'OvertimeRisk', 'UnqualifiedAssignment'] as const;

/** Ticket A: site-coverage overview, smart settings, candidate preview. */
@Injectable({ providedIn: 'root' })
export class ShiftManagementService {
  private readonly http = inject(HttpClient);

  getOverview(filter: SiteShiftsOverviewFilter): Observable<SiteShiftsOverviewPage> {
    let params = new HttpParams()
      .set('page', String(filter.page))
      .set('pageSize', String(filter.pageSize));
    if (filter.search?.trim()) params = params.set('search', filter.search.trim());
    if (filter.regionId !== undefined) params = params.set('regionId', String(filter.regionId));
    return this.http
      .get<SiteShiftsOverviewPage>(`${API_BASE}/shifts/overview`, { params })
      .pipe(map((res) => ({ ...res, items: res.items.map(normalizeCard) })));
  }

  updateSmartSettings(siteId: number, dto: SiteSmartSettingsUpdate): Observable<void> {
    return this.http.patch<void>(`${API_BASE}/sites/${siteId}/smart-settings`, dto);
  }

  getCandidatePreview(id: number): Observable<ShiftCandidatePreview> {
    return this.http.get<ShiftCandidatePreview>(`${API_BASE}/shifts/candidates/${id}/preview`);
  }

  getEmployeePreview(employeeId: number): Observable<ShiftCandidatePreview> {
    return this.http.get<ShiftCandidatePreview>(
      `${API_BASE}/shifts/employees/${employeeId}/preview`,
    );
  }
}

function normalizeCard(card: SiteShiftOverviewCard): SiteShiftOverviewCard {
  if (typeof card.status === 'number') {
    return { ...card, status: STATUS_BY_INDEX[card.status] ?? 'Covered' };
  }
  return card;
}
