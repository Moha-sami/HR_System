import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';
import type { RequestType } from '../models/request-type.model';

@Injectable({
  providedIn: 'root'
})
export class RequestTypeService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.baseUrl}/request-types`;

  getRequestTypes(): Observable<RequestType[]> {
    return this.http.get<any>(this.apiUrl).pipe(
      map(response => {
        // Handle wrapped responses from ASP.NET or other structures
        if (response && response.$values) return response.$values;
        if (response && response.data) return response.data;
        if (response && response.items) return response.items;
        return response || [];
      })
    );
  }

  getRequestTypeById(id: string | number): Observable<RequestType> {
    return this.http.get<RequestType>(`${this.apiUrl}/${id}`);
  }

  createRequestType(requestType: RequestType): Observable<RequestType> {
    const payload = {
      ...requestType,
      requiresDates: true,
      requiresReason: true,
      isActive: true,
      addedBy: 'Ahmed Ali' // Mock user for now
    };
    return this.http.post<RequestType>(this.apiUrl, payload);
  }

  updateRequestType(id: string | number, requestType: RequestType): Observable<RequestType> {
    const payload = {
      ...requestType,
      id: Number(id), // API requires ID in the body for PUT
      requiresDates: true,
      requiresReason: true,
      isActive: true
    };
    return this.http.put<RequestType>(`${this.apiUrl}/${id}`, payload);
  }

  deleteRequestType(id: string | number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
