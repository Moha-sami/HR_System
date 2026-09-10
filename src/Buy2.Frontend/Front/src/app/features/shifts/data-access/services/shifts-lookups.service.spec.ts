import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../../environments/environment';
import { ShiftsLookupsService } from './shifts-lookups.service';

const API_BASE = environment.baseUrl;

/**
 * Ticket #325: single-owner lookup facades for the shifts area, so the four
 * subfeatures never build their own site/role/employee loaders.
 */
describe('ShiftsLookupsService', () => {
  let service: ShiftsLookupsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), ShiftsLookupsService],
    });

    service = TestBed.inject(ShiftsLookupsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should GET the site list for the sites multi-select', () => {
    const sites = [{ id: 1, siteName: 'Site 1' }];
    let result: typeof sites | undefined;
    service.getSites().subscribe((r) => (result = r));

    const req = httpMock.expectOne(`${API_BASE}/sites`);
    expect(req.request.method).toBe('GET');
    req.flush(sites);
    expect(result).toEqual(sites);
  });

  it('should GET the job-role list for the block role dropdown', () => {
    const roles = [{ id: 3, title: 'Cashier' }];
    let result: typeof roles | undefined;
    service.getJobRoles().subscribe((r) => (result = r));

    const req = httpMock.expectOne(`${API_BASE}/job-roles`);
    expect(req.request.method).toBe('GET');
    req.flush(roles);
    expect(result).toEqual(roles);
  });

  it('should GET the employees of one site for the assignment strip', () => {
    const employees = [{ employeeId: 16, fullName: 'Darrell Steward', roleName: 'Cashier' }];
    let result: typeof employees | undefined;
    service.getSiteEmployees(2).subscribe((r) => (result = r));

    const req = httpMock.expectOne(`${API_BASE}/sites/2/employees`);
    expect(req.request.method).toBe('GET');
    req.flush(employees);
    expect(result).toEqual(employees);
  });

  it('should GET a page of employees with paging params', () => {
    let result: { totalCount: number } | undefined;
    service.getEmployeesPage(2, 20).subscribe((r) => (result = r));

    const req = httpMock.expectOne(
      (r) => r.url === `${API_BASE}/employees` && r.method === 'GET',
    );
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('20');
    req.flush({ items: [], totalCount: 0, page: 2, pageSize: 20 });
    expect(result?.totalCount).toBe(0);
  });
});
