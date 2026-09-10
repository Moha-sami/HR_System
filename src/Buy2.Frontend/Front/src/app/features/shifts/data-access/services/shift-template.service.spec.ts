import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../../environments/environment';
import { ShiftTemplateService } from './shift-template.service';
import type {
  CreateShiftTemplateRequest,
  ShiftTemplateDetails,
  ShiftTemplateListResponse,
  UpdateShiftTemplateRequest,
} from '../models/shift-template.models';

const API_BASE = environment.baseUrl;

/**
 * Ticket #325: template CRUD against the real backend contract
 * (api/v1/shift-templates). Bare DTOs, 12-hour 'hh:mm tt' time strings.
 */
describe('ShiftTemplateService', () => {
  let service: ShiftTemplateService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), ShiftTemplateService],
    });

    service = TestBed.inject(ShiftTemplateService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should GET the paged list with search, sort and paging params', () => {
    const response: ShiftTemplateListResponse = {
      items: [
        {
          id: 1,
          name: 'Morning Shift',
          creationDate: '2020-03-09',
          lastUpdated: '2024-07-15',
          numberOfAssignedSites: 5,
        },
      ],
      totalCount: 1,
      pageNumber: 2,
      pageSize: 10,
      totalPages: 1,
    };
    let result: ShiftTemplateListResponse | undefined;
    service
      .getTemplates({ searchTerm: 'morn', nameSort: 'asc', pageNumber: 2, pageSize: 10 })
      .subscribe((r) => (result = r));

    const req = httpMock.expectOne(
      (r) => r.url === `${API_BASE}/shift-templates` && r.method === 'GET',
    );
    expect(req.request.params.get('searchTerm')).toBe('morn');
    expect(req.request.params.get('nameSort')).toBe('asc');
    expect(req.request.params.get('pageNumber')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('10');
    req.flush(response);
    expect(result).toEqual(response);
  });

  it('should GET template details by id', () => {
    const details: ShiftTemplateDetails = {
      id: 1,
      name: 'Morning Shift',
      startTime: '09:00 AM',
      endTime: '05:00 PM',
      creationDate: '2020-03-09',
      lastUpdated: '2024-07-15',
      numberOfAssignedSites: 2,
      sites: [
        { siteId: 1, siteName: 'Site 1' },
        { siteId: 2, siteName: 'Site 2' },
      ],
      shiftBlocks: [
        {
          id: 11,
          startTime: '09:00 AM',
          endTime: '11:00 AM',
          jobRoleId: 3,
          jobRoleTitle: 'Cashier',
          assignedUserId: 16,
          assignedUserName: 'Darrell Steward',
        },
      ],
      lastUpdatedByEmployeeId: 7,
    };
    let result: ShiftTemplateDetails | undefined;
    service.getTemplateById(1).subscribe((r) => (result = r));

    const req = httpMock.expectOne(`${API_BASE}/shift-templates/1`);
    expect(req.request.method).toBe('GET');
    req.flush(details);
    expect(result).toEqual(details);
  });

  it('should POST a new template as a bare DTO and return its details', () => {
    const payload: CreateShiftTemplateRequest = {
      name: 'Morning Shift',
      siteIds: [1, 2],
      startTime: '09:00 AM',
      endTime: '05:00 PM',
      shiftBlocks: [{ startTime: '09:00 AM', endTime: '11:00 AM', jobRoleId: 3 }],
    };
    let result: ShiftTemplateDetails | undefined;
    service.createTemplate(payload).subscribe((r) => (result = r));

    const req = httpMock.expectOne(`${API_BASE}/shift-templates`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush({ id: 9, name: 'Morning Shift' });
    expect(result?.id).toBe(9);
  });

  it('should PUT the complete template on update (full-replace semantics)', () => {
    const payload: UpdateShiftTemplateRequest = {
      name: 'Morning Shift',
      siteIds: [1],
      startTime: '09:00 AM',
      endTime: '05:00 PM',
      shiftBlocks: [
        { id: 11, startTime: '09:00 AM', endTime: '11:00 AM', jobRoleId: 3, assignedUserId: 16 },
        { id: 0, startTime: '11:00 AM', endTime: '01:00 PM', jobRoleId: 4 },
      ],
    };
    let result: ShiftTemplateDetails | undefined;
    service.updateTemplate(1, payload).subscribe((r) => (result = r));

    const req = httpMock.expectOne(`${API_BASE}/shift-templates/1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(payload);
    req.flush({ id: 1, name: 'Morning Shift' });
    expect(result?.id).toBe(1);
  });

  it('should POST to the duplicate endpoint and return the copy identity', () => {
    let result: { id: number; name: string } | undefined;
    service.duplicateTemplate(1).subscribe((r) => (result = r));

    const req = httpMock.expectOne(`${API_BASE}/shift-templates/1/duplicate`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeNull();
    req.flush({ id: 10, name: 'Morning Shift (Copy)' });
    expect(result).toEqual({ id: 10, name: 'Morning Shift (Copy)' });
  });

  it('should DELETE the template by id', () => {
    let completed = false;
    service.deleteTemplate(1).subscribe(() => (completed = true));

    const req = httpMock.expectOne(`${API_BASE}/shift-templates/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
    expect(completed).toBe(true);
  });
});
