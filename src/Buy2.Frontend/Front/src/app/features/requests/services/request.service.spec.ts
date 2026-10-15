import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { RequestService } from './request.service';

const EMPTY_PAGE = {
  items: [],
  totalCount: 0,
  pageNumber: 1,
  pageSize: 20,
  totalPages: 0,
  hasPreviousPage: false,
  hasNextPage: false,
};

describe('RequestService', () => {
  let service: RequestService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(RequestService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getSubmittedRequests builds default pagination params', () => {
    service.getSubmittedRequests().subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/requests/submitted'));
    expect(req.request.params.get('pageNumber')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('20');
    expect(req.request.params.get('sortBy')).toBe('SubmittedAt');
    expect(req.request.params.get('sortDescending')).toBe('true');
    expect(req.request.params.get('overallStatus')).toBeNull();
    expect(req.request.params.get('search')).toBeNull();
    req.flush(EMPTY_PAGE);
  });

  it('getSubmittedRequests applies status and search filters when provided', () => {
    service
      .getSubmittedRequests(1, 1, 'SubmittedAt', true, { search: '7', overallStatus: 'Pending' })
      .subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/requests/submitted'));
    expect(req.request.params.get('search')).toBe('7');
    expect(req.request.params.get('overallStatus')).toBe('Pending');
    expect(req.request.params.get('managerStatus')).toBeNull();
    expect(req.request.params.get('hrStatus')).toBeNull();
    req.flush(EMPTY_PAGE);
  });

  it('getSubmittedRequests omits absent filter params', () => {
    service
      .getSubmittedRequests(2, 10, 'EmployeeName', false, { managerStatus: 'Approved' })
      .subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/requests/submitted'));
    expect(req.request.params.get('pageNumber')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('10');
    expect(req.request.params.get('sortBy')).toBe('EmployeeName');
    expect(req.request.params.get('sortDescending')).toBe('false');
    expect(req.request.params.get('managerStatus')).toBe('Approved');
    expect(req.request.params.get('overallStatus')).toBeNull();
    req.flush(EMPTY_PAGE);
  });
});
