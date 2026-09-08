import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../../environments/environment';
import { PointsManagementService } from './points-management.service';

const TRANSACTIONS_URL = `${environment.baseUrl}/points/transactions`;
const EMPLOYEES_URL = `${environment.baseUrl}/employees`;

describe('PointsManagementService', () => {
  let service: PointsManagementService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), PointsManagementService],
    });

    service = TestBed.inject(PointsManagementService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should GET paginated transactions with filter query params', () => {
    service
      .getTransactions({
        pageNumber: 2,
        pageSize: 10,
        searchTerm: 'Mona',
        triggeredBy: 'ManualAdjustment',
        transactionType: 'Add',
        sortBy: 'EmployeeName',
        sortDir: 'Asc',
        month: 9,
        year: 2026,
      })
      .subscribe((response) => {
        expect(response.totalCount).toBe(1);
        expect(response.items[0].employeeName).toBe('Mona Ali');
        expect(response.items[0].transactionType).toBe('Add');
        expect(response.items[0].comments).toBe('Bonus');
      });

    const req = httpMock.expectOne((request) => request.url === TRANSACTIONS_URL);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('pageNumber')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('10');
    expect(req.request.params.get('searchTerm')).toBe('Mona');
    expect(req.request.params.get('triggeredBy')).toBe('ManualAdjustment');
    expect(req.request.params.get('transactionType')).toBe('Add');
    expect(req.request.params.get('sortBy')).toBe('EmployeeName');
    expect(req.request.params.get('sortDir')).toBe('Asc');
    expect(req.request.params.get('month')).toBe('9');
    expect(req.request.params.get('year')).toBe('2026');

    req.flush({
      items: [
        {
          id: 12,
          employeeId: 4,
          employeeName: 'Mona Ali',
          employeeCode: 'EMP-0004',
          departmentName: 'HR',
          siteName: 'Cairo',
          avatarUrl: null,
          date: '2026-09-01T10:00:00Z',
          time: '10:00:00',
          transactionType: 'Add',
          points: 100,
          triggeredBy: 'ManualAdjustment',
          comments: 'Bonus',
          createdAt: '2026-09-01T10:00:00Z',
        },
      ],
      totalCount: 1,
      pageNumber: 2,
      pageSize: 10,
      totalPages: 1,
    });
  });

  it('should omit empty optional filter params', () => {
    service
      .getTransactions({
        pageNumber: 1,
        pageSize: 10,
      })
      .subscribe();

    const req = httpMock.expectOne((request) => request.url === TRANSACTIONS_URL);
    expect(req.request.params.get('searchTerm')).toBeNull();
    expect(req.request.params.get('triggeredBy')).toBeNull();
    expect(req.request.params.get('transactionType')).toBeNull();
    req.flush({ items: [], totalCount: 0, pageNumber: 1, pageSize: 10, totalPages: 0 });
  });

  it('should POST a manual transaction body matching the API DTO', () => {
    service
      .createTransaction({
        employeeId: 4,
        transactionType: 'Deduct',
        pointsValue: 75,
        comments: 'Late arrival',
      })
      .subscribe((result) => {
        expect(result.isSuccess).toBe(true);
        expect(result.transactionId).toBe(99);
      });

    const req = httpMock.expectOne(TRANSACTIONS_URL);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      employeeId: 4,
      transactionType: 'Deduct',
      pointsValue: 75,
      comments: 'Late arrival',
    });

    req.flush({ isSuccess: true, transactionId: 99 }, { status: 201, statusText: 'Created' });
  });

  it('should GET employees from the real employees API', () => {
    service.getEmployees('Ali').subscribe((employees) => {
      expect(employees).toEqual([{ id: 4, employeeName: 'Mona Ali' }]);
    });

    const req = httpMock.expectOne((request) => request.url.startsWith(`${EMPLOYEES_URL}?`));
    expect(req.request.method).toBe('GET');
    expect(req.request.url).toContain('search=Ali');
    expect(req.request.url).toContain('page=1');
    expect(req.request.url).toContain('pageSize=20');

    req.flush({
      items: [
        {
          id: 4,
          employeeCode: 'EMP-0004',
          employeeName: 'Mona Ali',
          joinDate: '2024-01-01',
          jobTitle: 'HR',
          email: 'mona@example.com',
          adminAccess: false,
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 20,
    });
  });
});
