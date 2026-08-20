import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import {
  type CreateEmployeeRequest,
  type EmployeeApiResponse,
  EmployeeService,
} from './employee.service';

const EMPLOYEES_URL = `${environment.jsonServerUrl}/employees`;
const JOB_ROLES_URL = `${environment.jsonServerUrl}/jobRoles`;
const ROLES_URL = `${environment.jsonServerUrl}/roles`;
const SITES_URL = `${environment.jsonServerUrl}/sites`;

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

  it('should GET employees from JSON Server', () => {
    const response: readonly EmployeeApiResponse[] = [sampleEmployee()];
    let employees: readonly EmployeeApiResponse[] | undefined;

    service.getEmployees().subscribe((value) => (employees = value));

    const req = httpMock.expectOne(EMPLOYEES_URL);
    expect(req.request.method).toBe('GET');
    req.flush(response);

    expect(employees).toEqual(response);
  });

  it('should POST a new employee to JSON Server', () => {
    const input: CreateEmployeeRequest = {
      firstName: 'Mona',
      lastName: 'Hassan',
      email: 'mona.hassan@buy2.com',
      phoneNumber: '+966599876543',
      jobRoleId: 2,
      roleId: 4,
      siteId: 1,
      createdAt: '2026-08-19T10:00:00Z',
    };
    const response: EmployeeApiResponse = { id: 9, ...input };
    let created: EmployeeApiResponse | undefined;

    service.createEmployee(input).subscribe((value) => (created = value));

    const req = httpMock.expectOne(EMPLOYEES_URL);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(input);
    req.flush(response);

    expect(created).toEqual(response);
  });

  it('should GET employee form options from JSON Server', () => {
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

  it('should PATCH an existing employee through JSON Server', () => {
    const input = createEmployeeRequest();
    const response: EmployeeApiResponse = { id: 1, ...input };

    service.updateEmployee(1, input).subscribe();

    const req = httpMock.expectOne(`${EMPLOYEES_URL}/1`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual(input);
    req.flush(response);
  });

  it('should DELETE an existing employee through JSON Server', () => {
    service.deleteEmployee(1).subscribe();

    const req = httpMock.expectOne(`${EMPLOYEES_URL}/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('should propagate JSON Server errors when creating an employee', () => {
    let error: unknown;

    service.createEmployee(createEmployeeRequest()).subscribe({ error: (value) => (error = value) });

    const req = httpMock.expectOne(EMPLOYEES_URL);
    req.flush({ message: 'Validation failed' }, { status: 400, statusText: 'Bad Request' });

    expect(error).toBeTruthy();
  });
});

function sampleEmployee(): EmployeeApiResponse {
  return {
    id: 1,
    ...createEmployeeRequest(),
  };
}

function createEmployeeRequest(): CreateEmployeeRequest {
  return {
    firstName: 'Ahmed',
    lastName: 'Ali',
    email: 'a.ali@buy2.com',
    phoneNumber: '+966598432423',
    jobRoleId: 2,
    roleId: 1,
    siteId: 1,
    createdAt: '2026-01-15T08:00:00Z',
  };
}
