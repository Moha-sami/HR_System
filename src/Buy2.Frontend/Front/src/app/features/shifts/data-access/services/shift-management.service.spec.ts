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

  it('should POST publish preflight with site and target dates', () => {
    service.publishPreflight({ siteId: 3, targetDates: ['2026-10-02'] }).subscribe((res) => {
      expect(res.hasExceptions).toBe(true);
      expect(res.unqualifiedCount).toBe(1);
      expect(res.overtimeCount).toBe(1);
    });

    const req = httpMock.expectOne((r) => r.url.endsWith('/shifts/publish/preflight'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ siteId: 3, targetDates: ['2026-10-02'] });
    req.flush({
      siteId: 3,
      totalUnpublishedShiftsScanned: 2,
      totalDatesScanned: 1,
      scannedDates: ['2026-10-02'],
      unqualifiedAssignees: [
        {
          shiftId: 11,
          employeeId: 100,
          employeeName: 'Sara',
          requiredRoleId: 10,
          requiredRoleTitle: 'Cashier',
          employeeRoleId: 12,
          employeeRoleTitle: 'Cleaner',
          date: '2026-10-02',
          start: '09:00:00',
          end: '13:00:00',
          formattedTime: '09:00 AM - 01:00 PM',
        },
      ],
      overtimeViolations: [
        {
          shiftId: 12,
          employeeId: 101,
          employeeName: 'Omar',
          requiredRoleId: 10,
          requiredRoleTitle: 'Cashier',
          employeeRoleId: 10,
          employeeRoleTitle: 'Cashier',
          date: '2026-10-02',
          start: '13:00:00',
          end: '21:00:00',
          formattedTime: '01:00 PM - 09:00 PM',
          shiftHours: 8,
          totalWeeklyHours: 44,
          projectedOvertimeHours: 4,
          violationReason: 'Exceeds 40h weekly limit',
        },
      ],
      unqualifiedCount: 1,
      overtimeCount: 1,
      hasExceptions: true,
      canPublishImmediately: false,
    });
  });

  it('should POST publish commit with resolutions and justification', () => {
    service
      .publishCommit({
        siteId: 3,
        targetDates: ['2026-10-02'],
        exceptionResolutions: { 11: 1, 12: 2 },
        overtimeJustification: 'Holiday cover',
      })
      .subscribe((res) => {
        expect(res.success).toBe(true);
        expect(res.publishedImmediatelyCount).toBe(1);
        expect(res.skippedCount).toBe(1);
      });

    const req = httpMock.expectOne((r) => r.url.endsWith('/shifts/publish/commit'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      siteId: 3,
      targetDates: ['2026-10-02'],
      exceptionResolutions: { 11: 1, 12: 2 },
      overtimeJustification: 'Holiday cover',
    });
    req.flush({
      success: true,
      publishedImmediatelyCount: 1,
      pendingHrApprovalCount: 0,
      skippedCount: 1,
      ids: [11],
      message: 'Published 1 shift, skipped 1.',
    });
  });

  it('should POST copy preflight with source and targets', () => {
    service
      .copyPreflight({ siteId: 3, sourceDate: '2026-10-02', targetDates: ['2026-10-03'] })
      .subscribe((res) => {
        expect(res.hasConflicts).toBe(true);
        expect(res.conflictingDates.length).toBe(1);
      });

    const req = httpMock.expectOne((r) => r.url.endsWith('/shifts/copy/preflight'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      siteId: 3,
      sourceDate: '2026-10-02',
      targetDates: ['2026-10-03'],
    });
    req.flush({
      siteId: 3,
      sourceDate: '2026-10-02',
      totalTargetDates: 1,
      conflictFreeDates: [],
      conflictingDates: [
        {
          date: '2026-10-03',
          shiftCount: 2,
          shiftNames: ['Morning', 'Evening'],
          existingShifts: [],
        },
      ],
      hasConflicts: true,
    });
  });

  it('should POST copy commit with resolutions and flags', () => {
    service
      .copyCommit({
        siteId: 3,
        sourceDate: '2026-10-02',
        targetDates: ['2026-10-03', '2026-10-04'],
        dateResolutions: { '2026-10-03': 1 },
        bulkReplaceAll: false,
        copyAssignments: true,
      })
      .subscribe((res) => {
        expect(res.success).toBe(true);
        expect(res.copiedDatesCount).toBe(2);
      });

    const req = httpMock.expectOne((r) => r.url.endsWith('/shifts/copy/commit'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      siteId: 3,
      sourceDate: '2026-10-02',
      targetDates: ['2026-10-03', '2026-10-04'],
      dateResolutions: { '2026-10-03': 1 },
      bulkReplaceAll: false,
      copyAssignments: true,
    });
    req.flush({
      success: true,
      totalDatesProcessed: 2,
      copiedDatesCount: 2,
      skippedDatesCount: 0,
      totalShiftsCreated: 4,
      totalShiftsReplaced: 2,
      copiedDates: ['2026-10-03', '2026-10-04'],
      skippedDates: [],
      message: 'Copied 2 dates.',
    });
  });

  it('should POST draft shifts to validate-draft', () => {
    service
      .validateDraft([
        {
          employeeId: 100,
          jobRoleId: 10,
          siteId: 3,
          startTime: '2026-10-02T09:00:00',
          endTime: '2026-10-02T13:00:00',
        },
      ])
      .subscribe((res) => {
        expect(res.isValid).toBe(false);
        expect(res.errors).toEqual(['Unqualified for Cashier.']);
      });

    const req = httpMock.expectOne((r) => r.url.endsWith('/schedules/validate-draft'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual([
      {
        employeeId: 100,
        jobRoleId: 10,
        siteId: 3,
        startTime: '2026-10-02T09:00:00',
        endTime: '2026-10-02T13:00:00',
      },
    ]);
    req.flush({ isValid: false, warnings: [], errors: ['Unqualified for Cashier.'] });
  });
});
