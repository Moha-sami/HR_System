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

  it('should GET the job-role list from the paginated /jobs endpoint', () => {
    const roles = [{ id: 3, title: 'Cashier' }];
    let result: typeof roles | undefined;
    service.getJobRoles().subscribe((r) => (result = r));

    const req = httpMock.expectOne((r) => r.url === `${API_BASE}/jobs`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('pageNumber')).toBe('1');
    req.flush({ items: roles, totalCount: 1, pageNumber: 1, pageSize: 100, totalPages: 1 });
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

  it('should fan out one paged /shifts/employees request per site and merge', () => {
    const page1 = {
      items: [
        {
          id: 100, employeeCode: 'E100', fullName: 'Sara', roleTitle: 'Cashier',
          jobRoleId: 10, weeklyCompletedHours: 20, ratingScore: 4.6,
          riskStatusToken: 'TopPerformer', isPreferredForSite: true,
        },
      ],
      totalCount: 1, page: 1, pageSize: 20,
    };
    const page2 = {
      items: [
        {
          id: 100, employeeCode: 'E100', fullName: 'Sara', roleTitle: 'Cashier',
          jobRoleId: 10, weeklyCompletedHours: 20, ratingScore: 4.6,
          riskStatusToken: 'TopPerformer', isPreferredForSite: false,
        },
        {
          id: 101, employeeCode: 'E101', fullName: 'Omar', roleTitle: 'Guard',
          jobRoleId: 11, weeklyCompletedHours: 40, ratingScore: 3.5,
          riskStatusToken: 'OvertimeRisk', isPreferredForSite: false,
        },
      ],
      totalCount: 2, page: 1, pageSize: 20,
    };
    let result: { items: { id: number }[]; totalCount: number } | undefined;
    service
      .getShiftEmployees({
        siteIds: [1, 2], page: 1, pageSize: 20,
        search: 'a', ratingTiers: ['4.5+'], isPreferredOnly: true,
      })
      .subscribe((r) => (result = r));

    const reqs = httpMock.match((r) => r.url === `${API_BASE}/shifts/employees`);
    expect(reqs.length).toBe(2);
    for (const req of reqs) {
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('Page')).toBe('1');
      expect(req.request.params.get('PageSize')).toBe('20');
      expect(req.request.params.get('Search')).toBe('a');
      expect(req.request.params.getAll('RatingTiers')).toEqual(['4.5+']);
      expect(req.request.params.get('IsPreferredOnly')).toBe('true');
    }
    expect(reqs[0].request.params.get('SiteId')).toBe('1');
    expect(reqs[1].request.params.get('SiteId')).toBe('2');
    reqs[0].flush(page1);
    reqs[1].flush(page2);

    expect(result?.items.map((e) => e.id)).toEqual([100, 101]);
    expect(result?.totalCount).toBe(3);
  });

  it('should return an empty page without any request when no site is selected', () => {
    let result: { items: unknown[]; totalCount: number } | undefined;
    service.getShiftEmployees({ siteIds: [], page: 1, pageSize: 20 }).subscribe((r) => (result = r));

    httpMock.expectNone((r) => r.url === `${API_BASE}/shifts/employees`);
    expect(result).toEqual({ items: [], totalCount: 0, page: 1, pageSize: 20 });
  });
});
