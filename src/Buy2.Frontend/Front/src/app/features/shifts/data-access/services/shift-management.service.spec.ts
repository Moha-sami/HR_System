import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ShiftManagementService } from './shift-management.service';

describe('ShiftManagementService', () => {
  let service: ShiftManagementService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ShiftManagementService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should fetch the overview with paging params', () => {
    service.getOverview({ page: 2, pageSize: 10 }).subscribe((res) => {
      expect(res.totalCount).toBe(1);
      expect(res.items[0].status).toBe('Shortage');
    });

    const req = httpMock.expectOne((r) => r.url.endsWith('/shifts/overview'));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('10');
    expect(req.request.params.has('search')).toBe(false);
    expect(req.request.params.has('regionId')).toBe(false);
    req.flush({
      items: [
        {
          siteId: 1,
          siteName: 'Cairo HQ',
          address: 'Cairo',
          regionId: 2,
          regionName: 'Cairo',
          totalShifts: 3,
          openShifts: 1,
          filledShifts: 2,
          status: 1,
          isSmartAssignmentEnabled: true,
          isSmartPostingEnabled: false,
        },
      ],
      totalCount: 1,
      page: 2,
      pageSize: 10,
    });
  });

  it('should send search and regionId when set', () => {
    service.getOverview({ search: '  Cairo ', regionId: 2, page: 1, pageSize: 10 }).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/shifts/overview'));
    expect(req.request.params.get('search')).toBe('Cairo');
    expect(req.request.params.get('regionId')).toBe('2');
    req.flush({ items: [], totalCount: 0, page: 1, pageSize: 10 });
  });

  it('should PATCH smart settings for the site', () => {
    service
      .updateSmartSettings(1, { isSmartAssignmentEnabled: false, isSmartPostingEnabled: true })
      .subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/sites/1/smart-settings'));
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({
      isSmartAssignmentEnabled: false,
      isSmartPostingEnabled: true,
    });
    req.flush(null);
  });

  it('should fetch the candidate preview by id', () => {
    service.getCandidatePreview(7).subscribe((res) => {
      expect(res.fullName).toBe('Sara');
    });

    const req = httpMock.expectOne((r) => r.url.endsWith('/shifts/candidates/7/preview'));
    expect(req.request.method).toBe('GET');
    req.flush({
      id: 7,
      employeeCode: 'EMP-1',
      fullName: 'Sara',
      avatarUrl: null,
      jobTitle: 'Cashier',
      joinDate: '2020-07-29',
      hourlyRate: 11,
      rating: 4.5,
      careerCompletedHours: 2956,
      qualifications: ['Cashier Training'],
    });
  });
});
