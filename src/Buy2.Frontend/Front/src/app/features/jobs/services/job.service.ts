import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { Job, JobPaginatedResponse, JobDetail, JobEmployeePaginatedResponse } from '../../../core/models/job';
import { environment } from '../../../../environments/environment';

export type { Job };

@Injectable({
  providedIn: 'root'
})
export class JobService {
  private apiUrl = `${environment.baseUrl}/jobs`;
  private jsonServerUrl = `${environment.jsonServerUrl}`;

  constructor(private http: HttpClient) {}

  getJobs(pageNumber: number = 1, pageSize: number = 10): Observable<JobPaginatedResponse> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<JobPaginatedResponse>(this.apiUrl, { params });
  }

  getJob(id: number): Observable<JobDetail> {
    return this.http.get<JobDetail>(`${this.apiUrl}/${id}`);
  }

  getJobEmployees(id: number, pageNumber: number = 1, pageSize: number = 10): Observable<JobEmployeePaginatedResponse> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<JobEmployeePaginatedResponse>(`${this.apiUrl}/${id}/employees`, { params });
  }

  createJob(job: any): Observable<any> {
    return this.http.post<any>(this.apiUrl, job);
  }

  updateJob(id: number, job: any): Observable<any> {
    return this.http.patch<any>(`${this.apiUrl}/${id}`, job);
  }

  deleteJob(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getDepartments(): Observable<any[]> {
    return this.http.get<any[]>('http://localhost:3000/departments');
  }

  createDepartment(department: any): Observable<any> {
    return this.http.post<any>('http://localhost:3000/departments', department);
  }

  getQualifications(): Observable<any[]> {
    return this.http.get<any[]>('http://localhost:3000/qualifications');
  }

  createQualification(qualification: any): Observable<any> {
    return this.http.post<any>('http://localhost:3000/qualifications', qualification);
  }
}
