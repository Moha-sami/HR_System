import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { EmployeeDetailService } from './employee-detail.service';
import type {
  UpdateJobDetailsRequest,
  UpdatePayrollProfileRequest,
  UpdatePersonalInfoRequest,
} from '../models/view-employee/information-tab.models';

const API_BASE = environment.baseUrl;

/**
 * Ticket #261: consolidated tab models + bare update envelopes.
 * Backend expects bare flat DTOs (no `{ dto: {...} }` wrapper) and the
 * current payroll field names (workWeekStartDay/EndDay,
 * overtimeRateMultiplier, assignedWorkSiteIds).
 */
describe('EmployeeDetailService (information tab contracts)', () => {
  let service: EmployeeDetailService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), EmployeeDetailService],
    });

    service = TestBed.inject(EmployeeDetailService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should PUT personal info as a bare DTO (no wrapper)', () => {
    const payload: UpdatePersonalInfoRequest = {
      firstName: 'John',
      lastName: 'Doe',
      phoneNumber: '+1234567890',
      birthdate: '1990-01-15T00:00:00.000Z',
      gender: 1,
      email: 'john@example.com',
    };
    let completed = false;
    service.updatePersonalInfo(1, payload).subscribe(() => (completed = true));

    const req = httpMock.expectOne(`${API_BASE}/employees/1/personal`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(payload);
    expect(req.request.body).not.toHaveProperty('dto');
    req.flush(null);
    expect(completed).toBe(true);
  });

  it('should PUT job details as a bare DTO (no wrapper)', () => {
    const payload: UpdateJobDetailsRequest = {
      jobRoleId: 3,
      directManagerId: 7,
      seniorityLevel: 'Senior',
      experienceYears: 5,
      jobType: 'FullTime',
      attendanceType: 'Hybrid',
      onlineWorkdays: ['Sunday', 'Monday'],
      offlineWorkdays: ['Tuesday'],
      qualifications: ['POS System'],
    };
    let completed = false;
    service.updateJobDetails(1, payload).subscribe(() => (completed = true));

    const req = httpMock.expectOne(`${API_BASE}/employees/1/job`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(payload);
    expect(req.request.body).not.toHaveProperty('dto');
    req.flush(null);
    expect(completed).toBe(true);
  });

  it('should PUT payroll with current field names', () => {
    const payload: UpdatePayrollProfileRequest = {
      salaryType: 1,
      payoutPeriod: 'Monthly',
      payoutDay: 1,
      workWeekStartDay: 0,
      workWeekEndDay: 4,
      paymentAmount: 5000,
      overtimeThresholdHours: 40,
      overtimeRateMultiplier: 1.5,
      attendanceType: 'Hybrid',
      assignedWorkSiteIds: [2, 5],
      onlineWorkdays: ['Sunday'],
      offlineWorkdays: ['Monday'],
    };
    let completed = false;
    service.updatePayrollProfile(1, payload).subscribe(() => (completed = true));

    const req = httpMock.expectOne(`${API_BASE}/employees/1/payroll`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(payload);
    req.flush(null);
    expect(completed).toBe(true);
  });

  it('should fetch jobs lookup with paging params', () => {
    let items: readonly unknown[] | undefined;
    service.getJobsLookup(1, 100).subscribe((res) => (items = res.items));

    const req = httpMock.expectOne(
      (r) => r.url === `${API_BASE}/jobs` && r.method === 'GET',
    );
    expect(req.request.params.get('pageNumber')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('100');
    req.flush({ items: [{ id: 3, title: 'Cashier' }], totalCount: 1 });
    expect(items).toEqual([{ id: 3, title: 'Cashier' }]);
  });

  it('should fetch employees lookup with paging and search params', () => {
    service.getEmployeesLookup(1, 50, 'jane').subscribe();

    const req = httpMock.expectOne(
      (r) => r.url === `${API_BASE}/employees` && r.method === 'GET',
    );
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('50');
    expect(req.request.params.get('search')).toBe('jane');
    req.flush({ items: [], totalCount: 0 });
  });

  it('should fetch sites lookup as a plain list', () => {
    let items: readonly unknown[] | undefined;
    service.getSitesLookup().subscribe((res) => (items = res));

    const req = httpMock.expectOne(`${API_BASE}/sites`);
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 2, siteName: 'Cairo Branch' }]);
    expect(items).toEqual([{ id: 2, siteName: 'Cairo Branch' }]);
  });
});
