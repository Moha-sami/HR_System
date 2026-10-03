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

  it('should fetch the daily schedule with query params and normalize it', () => {
    service.getDailySchedule(3, '2026-10-02').subscribe((res) => {
      expect(res.siteName).toBe('Cairo HQ');
      expect(res.totalCost).toBe(450);
      expect(res.week[0].status).toBe('CoveredAndPublished');
      expect(res.blocks.length).toBe(1);
      expect(res.blocks[0].startMin).toBe(540);
      expect(res.blocks[0].endMin).toBe(720);
    });

    const req = httpMock.expectOne((r) => r.url.endsWith('/shifts/daily'));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('siteId')).toBe('3');
    expect(req.request.params.get('date')).toBe('2026-10-02');
    req.flush({
      siteId: 3,
      siteName: 'Cairo HQ',
      date: '2026-10-02',
      isDayOff: false,
      totalEstimatedLaborCost: 450,
      weekCalendarStrip: [
        { date: '2026-10-02', dayOfWeek: 5, status: 0, isSelected: true, isDayOff: false },
      ],
      hourlyTimeline: [
        {
          startHour: '09:00:00',
          endHour: '10:00:00',
          blocks: [
            {
              shiftId: 11,
              siteId: 3,
              jobRoleId: 10,
              roleTitle: 'Cashier',
              startTime: '2026-10-02T09:00:00+03:00',
              endTime: '2026-10-02T12:00:00+03:00',
              isPublished: false,
              employeeId: null,
              employeeName: null,
              employeeAvatarUrl: null,
              statusColorCode: '#22c55e',
            },
            {
              shiftId: 12,
              siteId: 3,
              jobRoleId: 10,
              roleTitle: 'Broken',
              startTime: 'not-a-time',
              endTime: '2026-10-02T12:00:00+03:00',
              isPublished: false,
              employeeId: null,
              employeeName: null,
              employeeAvatarUrl: null,
              statusColorCode: null,
            },
          ],
        },
      ],
    });
  });

  it('should POST a new shift block', () => {
    service
      .createShiftBlock({
        siteId: 3,
        date: '2026-10-02',
        startTime: '09:00:00',
        endTime: '12:00:00',
        jobRoleId: 10,
        dispatchPolicy: 0,
      })
      .subscribe((res) => expect(res.shiftId).toBe(11));

    const req = httpMock.expectOne((r) => r.url.endsWith('/shifts/blocks'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body.jobRoleId).toBe(10);
    req.flush({ shiftId: 11 });
  });

  it('should POST an assignment with override flags', () => {
    service
      .assignBlock(11, { employeeId: 100, confirmOverride: true, overrideReason: 'OK' })
      .subscribe((res) => {
        expect(res.hasConflicts).toBe(false);
        expect(res.updatedCost).toBe(500);
      });

    const req = httpMock.expectOne((r) => r.url.endsWith('/shifts/blocks/11/assign'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ employeeId: 100, confirmOverride: true, overrideReason: 'OK' });
    req.flush({ success: true, hasConflicts: false, warnings: [], wasOverridden: true, updatedCost: 500 });
  });

  it('should DELETE an assignment with action params', () => {
    service.removeBlock(11, 'DeleteBlock', true).subscribe((res) => {
      expect(res.isDeleted).toBe(true);
    });

    const req = httpMock.expectOne((r) => r.url.endsWith('/shifts/blocks/11/assign'));
    expect(req.request.method).toBe('DELETE');
    expect(req.request.params.get('action')).toBe('DeleteBlock');
    expect(req.request.params.get('confirmPublishedDeletion')).toBe('true');
    req.flush({ shiftBlockId: 11, actionTaken: 1, isDeleted: true, updatedCost: 0, siteCoverageStatus: 3 });
  });

  it('should POST template apply with id and keep mode', () => {
    service
      .applyTemplate(3, '2026-10-02', { templateId: 1, keep: 'existing' })
      .subscribe((res) => {
        expect(res.warnings.length).toBe(0);
        expect(res.totalLaborCost).toBe(400);
      });

    const req = httpMock.expectOne((r) =>
      r.url.endsWith('/sites/3/dates/2026-10-02/apply-template'),
    );
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ templateId: 1, keep: 'existing' });
    req.flush({
      siteId: 3,
      date: '2026-10-02',
      templateId: 1,
      warnings: [],
      totalLaborCost: 400,
      regularCost: 400,
      overtimeCost: 0,
      prunedCount: 0,
    });
  });

  it('should POST save-as-template with the name', () => {
    service.saveAsTemplate(3, '2026-10-02', 'Morning rush').subscribe((res) => {
      expect(res.id).toBe(7);
      expect(res.name).toBe('Morning rush');
    });

    const req = httpMock.expectOne((r) =>
      r.url.endsWith('/sites/3/dates/2026-10-02/save-as-template'),
    );
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ name: 'Morning rush' });
    req.flush({ id: 7, name: 'Morning rush', totalBlockCount: 4 });
  });

  it('should POST save-as-template with null name when unnamed', () => {
    service.saveAsTemplate(3, '2026-10-02', null).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/save-as-template'));
    expect(req.request.body).toEqual({ name: null });
    req.flush({ id: 8, name: 'Cairo HQ 2026-10-02', totalBlockCount: 2 });
  });
});
