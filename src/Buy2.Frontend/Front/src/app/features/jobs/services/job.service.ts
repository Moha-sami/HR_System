import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface Job {
  id: string;
  jobName: string;
  jobDescription: string;
  numberOfEmployees: number;
  department?: string;
  qualifications?: string[];
}

@Injectable({
  providedIn: 'root'
})
export class JobService {
  private apiUrl = 'http://localhost:3000/jobs';

  constructor(private http: HttpClient) {}

  getJobs(): Observable<Job[]> {
    return this.http.get<Job[]>(this.apiUrl);
  }

  getJob(id: string): Observable<Job> {
    return this.http.get<Job>(`${this.apiUrl}/${id}`);
  }

  createJob(job: Omit<Job, 'id'>): Observable<Job> {
    return this.http.post<Job>(this.apiUrl, job);
  }

  updateJob(id: string, job: Partial<Job>): Observable<Job> {
    return this.http.patch<Job>(`${this.apiUrl}/${id}`, job);
  }

  deleteJob(id: string): Observable<void> {
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
