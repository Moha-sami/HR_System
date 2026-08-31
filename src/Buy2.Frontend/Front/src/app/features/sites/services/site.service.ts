import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import type { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import type {
  SiteListItemDto,
  CreateSiteDto,
  DeleteSiteDto,
  RegionDto,
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

  // ── GET employees for preferred people ────────────────────────────────────
  getEmployees(): Observable<PaginatedResult<EmployeeListItemDto>> {
    return this.http.get<PaginatedResult<EmployeeListItemDto>>(`${API_BASE}/employees`);
  }

  // ── CREATE site ───────────────────────────────────────────────────────────
  createSite(dto: CreateSiteDto): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${API_BASE}/sites`, dto);
  }

  // ── DELETE site ───────────────────────────────────────────────────────────
  deleteSite(id: number, dto: DeleteSiteDto): Observable<void> {
    return this.http.delete<void>(`${API_BASE}/sites/${id}`, { body: dto });
  }
}
