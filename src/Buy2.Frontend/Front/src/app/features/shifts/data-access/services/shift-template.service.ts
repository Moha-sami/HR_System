import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import type { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import type {
  CreateShiftTemplateRequest,
  DuplicateShiftTemplateResponse,
  ShiftTemplateDetails,
  ShiftTemplateFilter,
  ShiftTemplateListResponse,
  UpdateShiftTemplateRequest,
} from '../models/shift-template.models';

const API_BASE = environment.baseUrl;

/** Template CRUD against api/v1/shift-templates. HTTP calls only. */
@Injectable({ providedIn: 'root' })
export class ShiftTemplateService {
  private readonly http = inject(HttpClient);

  getTemplates(filter: ShiftTemplateFilter = {}): Observable<ShiftTemplateListResponse> {
    let params = new HttpParams();
    if (filter.searchTerm) params = params.set('searchTerm', filter.searchTerm);
    if (filter.nameSort) params = params.set('nameSort', filter.nameSort);
    if (filter.creationSort) params = params.set('creationSort', filter.creationSort);
    if (filter.updatedSort) params = params.set('updatedSort', filter.updatedSort);
    if (filter.numberOfAssignedSort)
      params = params.set('numberOfAssignedSort', filter.numberOfAssignedSort);
    params = params
      .set('pageNumber', String(filter.pageNumber ?? 1))
      .set('pageSize', String(filter.pageSize ?? 10));
    return this.http.get<ShiftTemplateListResponse>(`${API_BASE}/shift-templates`, { params });
  }

  getTemplateById(id: number): Observable<ShiftTemplateDetails> {
    return this.http.get<ShiftTemplateDetails>(`${API_BASE}/shift-templates/${id}`);
  }

  createTemplate(dto: CreateShiftTemplateRequest): Observable<ShiftTemplateDetails> {
    return this.http.post<ShiftTemplateDetails>(`${API_BASE}/shift-templates`, dto);
  }

  updateTemplate(id: number, dto: UpdateShiftTemplateRequest): Observable<ShiftTemplateDetails> {
    return this.http.put<ShiftTemplateDetails>(`${API_BASE}/shift-templates/${id}`, dto);
  }

  duplicateTemplate(id: number): Observable<DuplicateShiftTemplateResponse> {
    return this.http.post<DuplicateShiftTemplateResponse>(
      `${API_BASE}/shift-templates/${id}/duplicate`,
      null,
    );
  }

  deleteTemplate(id: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE}/shift-templates/${id}`);
  }
}
