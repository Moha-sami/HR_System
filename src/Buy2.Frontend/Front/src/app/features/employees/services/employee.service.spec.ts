import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { EmployeeService } from './employee.service';
import type { InsertEmployee } from '../models/insert-employee/insert-employee.models';
import type {
  PaginatedEmployeeListDto,
  EmployeeListRowDto,
} from '../models/employee-list/employee-list';

const API_BASE = environment.baseUrl;
const EMPLOYEES_URL = `${API_BASE}/employees`;
const JOB_ROLES_URL = `${API_BASE}/job-roles`;
const ROLES_URL = `${API_BASE}/roles`;
const SITES_URL = `${API_BASE}/sites`;

describe('EmployeeService', () => {
  let service: EmployeeService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), EmployeeService],
    });

    service = TestBed.inject(EmployeeService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should POST a new employee to real API', () => {
    const input: InsertEmployee = {
      firstName: 'Mona',
      lastName: 'Hassan',
      email: 'mona.hassan@buy2.com',
      phoneNumber: '+966599876543',
      jobRoleId: 2,
      roleId: 4,
      siteId: 1,
      createdAt: '2026-08-19T10:00:00Z',
    };
    const response = { id: 9 };
    let created: { id: number } | undefined;

    service.createEmployee(input).subscribe((value) => (created = value));

    const req = httpMock.expectOne(`${API_BASE}/employees/onboard`);
    expect(req.request.method).toBe('POST');
    // Check that the body contains the expected fields
    expect(req.request.body).toEqual({
      firstName: 'Mona',
      lastName: 'Hassan',
      email: 'mona.hassan@buy2.com',
      phoneNumber: '+966599876543',
      jobRoleId: 2,
      roleId: 4,
      siteId: 1,
      gender: 'Male',
      jobType: 'FullTime',
      attendanceType: 'OnSite',
      defaultPassword: 'Welcome@123',
    });
    req.flush(response);

    expect(created).toEqual(response);
  });

  it('should GET employee form options from real API', () => {
    service.getJobRoles().subscribe();
    service.getRoles().subscribe();
    service.getSites().subscribe();

    const jobRolesRequest = httpMock.expectOne(JOB_ROLES_URL);
    const rolesRequest = httpMock.expectOne(ROLES_URL);
    const sitesRequest = httpMock.expectOne(SITES_URL);

    expect(jobRolesRequest.request.method).toBe('GET');
    expect(rolesRequest.request.method).toBe('GET');
    expect(sitesRequest.request.method).toBe('GET');

    jobRolesRequest.flush([]);
    rolesRequest.flush([]);
    sitesRequest.flush([]);
  });

  it('should DELETE an existing employee through real API', () => {
    service.deleteEmployee(1).subscribe();

    const req = httpMock.expectOne(`${API_BASE}/employees/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('should GET paginated employees with query params', () => {
    const filter = {
      page: 1,
      pageSize: 20,
      search: 'test',
      region: 'Riyadh',
      department: 'IT',
      sort: 'joindate' as const,
      sortDir: 'desc' as const,
    };
    const response: PaginatedEmployeeListDto = {
      items: [
        {
          id: 1,
          employeeCode: 'EMP-001',
          employeeName: 'Test',
          joinDate: '2024-01-01',
          jobTitle: 'Dev',
          email: 'test@test.com',
          adminAccess: false,
        },
      ] as readonly EmployeeListRowDto[],
      totalCount: 1,
      page: 1,
      pageSize: 20,
    };
    let result: PaginatedEmployeeListDto | undefined;

    service.getEmployeesPaginated(filter).subscribe((value) => (result = value));

    const req = httpMock.expectOne((r) => r.url.startsWith(`${EMPLOYEES_URL}?`));
    expect(req.request.method).toBe('GET');
    expect(req.request.url).toContain('page=1');
    expect(req.request.url).toContain('pageSize=20');
    expect(req.request.url).toContain('search=test');
    expect(req.request.url).toContain('region=Riyadh');
    expect(req.request.url).toContain('department=IT');
    expect(req.request.url).toContain('sort=joindate');
    expect(req.request.url).toContain('sortDir=desc');
    req.flush(response);

    expect(result).toEqual(response);
  });

  it('should export employees as CSV blob', () => {
    const filter = {
      page: 1,
      pageSize: 100,
      search: '',
      region: null,
      department: null,
      sort: 'joindate' as const,
      sortDir: 'desc' as const,
    };
    const blob = new Blob(['csv,data'], { type: 'text/csv' });
    let result: Blob | undefined;

    service.exportEmployees(filter).subscribe((value) => (result = value));

    const req = httpMock.expectOne((r) => r.url.startsWith(`${API_BASE}/employees/export?`));
    expect(req.request.method).toBe('GET');
    expect(req.request.responseType).toBe('blob');
    req.flush(blob);

    expect(result).toBe(blob);
  });

  it('should propagate API errors when creating an employee', () => {
    let error: unknown;

    service
      .createEmployee({
        firstName: 'Test',
        lastName: 'User',
        email: 'test@test.com',
        phoneNumber: '+966500000000',
        jobRoleId: 1,
        roleId: 1,
        siteId: 1,
        createdAt: new Date().toISOString(),
      })
      .subscribe({ error: (value) => (error = value) });

    const req = httpMock.expectOne(`${API_BASE}/employees/onboard`);
    req.flush({ message: 'Validation failed' }, { status: 400, statusText: 'Bad Request' });

    expect(error).toBeTruthy();
  });
});
