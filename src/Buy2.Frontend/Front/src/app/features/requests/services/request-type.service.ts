import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import type { RequestType } from '../models/request-type.model';

@Injectable({
  providedIn: 'root'
})
export class RequestTypeService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.jsonServerUrl}/requestTypes`;

  getRequestTypes(): Observable<RequestType[]> {
    return this.http.get<RequestType[]>(this.apiUrl);
  }

  getRequestTypeById(id: string): Observable<RequestType> {
    return this.http.get<RequestType>(`${this.apiUrl}/${id}`);
  }

  createRequestType(requestType: RequestType): Observable<RequestType> {
    const payload = {
      ...requestType,
      createdAt: new Date().toISOString(),
      addedBy: 'Ahmed Ali' // Mock user for now
    };
    return this.http.post<RequestType>(this.apiUrl, payload);
  }

  updateRequestType(id: string, requestType: RequestType): Observable<RequestType> {
    return this.http.patch<RequestType>(`${this.apiUrl}/${id}`, requestType);
  }

  deleteRequestType(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
