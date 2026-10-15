import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { PaginatedResponse, SubmittedRequest, RequestHistoryItem, RequestDetails, RequestDecisionDto } from '../models/request';

export interface SubmittedRequestsFilters {
  search?: string;
  managerStatus?: string;
  hrStatus?: string;
  overallStatus?: string;
}

@Injectable({
  providedIn: 'root'
})
export class RequestService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.baseUrl}/requests`;

  getSubmittedRequests(
    pageNumber: number = 1,
    pageSize: number = 20,
    sortBy: string = 'SubmittedAt',
    sortDescending: boolean = true,
    filters?: SubmittedRequestsFilters,
  ): Observable<PaginatedResponse<SubmittedRequest>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize)
      .set('sortBy', sortBy)
      .set('sortDescending', sortDescending);

    if (filters?.search) params = params.set('search', filters.search);
    if (filters?.managerStatus) params = params.set('managerStatus', filters.managerStatus);
    if (filters?.hrStatus) params = params.set('hrStatus', filters.hrStatus);
    if (filters?.overallStatus) params = params.set('overallStatus', filters.overallStatus);

    return this.http.get<PaginatedResponse<SubmittedRequest>>(`${this.apiUrl}/submitted`, { params });
  }

  getRequestsHistory(pageNumber: number = 1, pageSize: number = 20, sortBy: string = 'SubmittedAt', sortDescending: boolean = true): Observable<PaginatedResponse<RequestHistoryItem>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize)
      .set('sortBy', sortBy)
      .set('sortDescending', sortDescending);
    
    return this.http.get<PaginatedResponse<RequestHistoryItem>>(`${this.apiUrl}/history`, { params });
  }

  exportRequestsHistory(format: string = 'csv', sortBy: string = 'SubmittedAt', sortDescending: boolean = true): Observable<Blob> {
    let params = new HttpParams()
      .set('format', format)
      .set('sortBy', sortBy)
      .set('sortDescending', sortDescending);

    return this.http.get(`${this.apiUrl}/history/export`, { params, responseType: 'blob' });
  }

  getRequestDetails(id: number): Observable<RequestDetails> {
    return this.http.get<RequestDetails>(`${this.apiUrl}/${id}`);
  }

  submitRequestDecision(id: number, decision: RequestDecisionDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/decision`, decision);
  }
}
