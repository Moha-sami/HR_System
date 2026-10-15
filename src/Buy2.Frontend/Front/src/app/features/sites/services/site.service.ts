import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import type { Observable } from 'rxjs';
import { map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import type {
  SiteListItemDto,
  CreateSiteDto,
  DeleteSiteDto,
  RegionDto,
  CoverageStatus,
  DayCoverageDto,
  SiteShiftCoverageOverviewDto,
} from '../models/site.models';

export interface EmployeeListItemDto {
  id: number;
  employeeCode: string;
  employeeName: string;
  joinDate: string;
  jobTitle: string;
  email: string;
  adminAccess: boolean;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

const API_BASE = environment.baseUrl;
const JSON_BASE = environment.jsonServerUrl;

// Backend serializes CoverageHealthStatus (Full, Partial, Critical, NoShifts) as numbers.
const COVERAGE_STATUS_BY_INDEX: readonly CoverageStatus[] = ['Full', 'Partial', 'Critical', 'NoShifts'];

interface RawDayCoverage extends Omit<DayCoverageDto, 'status'> {
  status: number | CoverageStatus;
}

interface RawSiteShiftCoverageOverview extends Omit<SiteShiftCoverageOverviewDto, 'days'> {
  days: RawDayCoverage[];
}

function normalizeCoverageStatus(status: number | CoverageStatus): CoverageStatus {
  return typeof status === 'number' ? (COVERAGE_STATUS_BY_INDEX[status] ?? 'NoShifts') : status;
}

@Injectable({ providedIn: 'root' })
export class SiteService {
  private readonly http = inject(HttpClient);

  // ── GET all sites ─────────────────────────────────────────────────────────
  getSites(): Observable<SiteListItemDto[]> {
    return this.http.get<SiteListItemDto[]>(`${API_BASE}/sites`);
  }

  // ── GET regions ──────────────────────────────────────────
  getRegions(): Observable<RegionDto[]> {
    return this.http.get<RegionDto[]>(`${API_BASE}/sites/regions`);
  }

  // ── GET 3-day site shift-coverage overview (dashboard) ──────────────────────
  getShiftsOverview(): Observable<SiteShiftCoverageOverviewDto[]> {
    return this.http.get<RawSiteShiftCoverageOverview[]>(`${API_BASE}/sites/shifts-overview`).pipe(
      map((sites) =>
        sites.map((site) => ({
          ...site,
          days: site.days.map((day) => ({ ...day, status: normalizeCoverageStatus(day.status) })),
        })),
      ),
    );
  }

  // ── GET employees for preferred people ────────────────────────────────────
  getEmployees(): Observable<PaginatedResult<EmployeeListItemDto>> {
    return this.http.get<PaginatedResult<EmployeeListItemDto>>(`${API_BASE}/employees`);
  }

  // ── CREATE site ───────────────────────────────────────────────────────────
  createSite(dto: CreateSiteDto): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${API_BASE}/sites`, dto);
  }

  // ── GET site by id ────────────────────────────────────────────────────────
  getSite(id: number): Observable<any> {
    return this.http.get<any>(`${API_BASE}/sites/${id}`);
  }

  // ── UPDATE site ───────────────────────────────────────────────────────────
  updateSite(id: number, dto: CreateSiteDto): Observable<void> {
    return this.http.put<void>(`${API_BASE}/sites/${id}`, dto);
  }

  // ── DELETE site ───────────────────────────────────────────────────────────
  deleteSite(id: number, dto: DeleteSiteDto): Observable<void> {
    return this.http.delete<void>(`${API_BASE}/sites/${id}`, { body: dto });
  }
}
