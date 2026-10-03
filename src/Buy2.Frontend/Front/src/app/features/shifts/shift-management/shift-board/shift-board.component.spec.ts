import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA, Component, Pipe, input, output, type PipeTransform } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { Router, ActivatedRoute, convertToParamMap } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ShiftBoardComponent } from './shift-board.component';
import { EmployeeStripComponent } from '../../shift-templates/employee-strip/employee-strip.component';
import { ShiftTimelineComponent } from '../../ui/shift-timeline/shift-timeline.component';
import { CandidatePreviewComponent } from '../candidate-preview/candidate-preview.component';
import { TemplatePickerComponent } from '../template-picker/template-picker.component';
import type { ShiftCandidateEmployee } from '../../data-access/models/shifts-lookups.models';

@Pipe({ name: 'translate', standalone: true })
class MockTranslatePipe implements PipeTransform {
  transform(value: string): string {
    return value;
  }
}

/** Strip stub: card gestures are driven through outputs, not DOM. */
@Component({ selector: 'app-employee-strip', standalone: true, template: '' })
class StubStripComponent {
  readonly siteIds = input<number[]>([]);
  readonly assignedIds = input<number[]>([]);
  readonly pageLoaded = output<{ page: number; items: ShiftCandidateEmployee[] }>();
  readonly cardClick = output<ShiftCandidateEmployee>();
}

/** Timeline stub: bar rendering lives in the timeline's own spec. */
@Component({ selector: 'app-shift-timeline', standalone: true, template: '' })
class StubTimelineComponent {
  readonly blocks = input<unknown[]>([]);
  readonly rangeStart = input<number | null>(null);
  readonly rangeEnd = input<number | null>(null);
  readonly hideEdit = input(false);
  readonly editBlock = output<string | number>();
  readonly assignBlock = output<string | number>();
  readonly deleteBlock = output<string | number>();
  readonly dropOnBlock = output<{ id: string | number; data: unknown }>();
}

/** Preview stub: preview fetching lives in its own spec. */
@Component({ selector: 'app-candidate-preview', standalone: true, template: '' })
class StubPreviewComponent {
  readonly employeeId = input<number | null>(null);
  readonly closed = output<void>();
}

/** Picker stub: apply flow lives in the picker's own spec. */
@Component({ selector: 'app-template-picker', standalone: true, template: '' })
class StubPickerComponent {
  readonly siteId = input(0);
  readonly date = input('');
  readonly applied = output<void>();
  readonly closed = output<void>();
}

const ROLES = [
  { id: 10, title: 'Cashier' },
  { id: 11, title: 'Guard' },
];

const SARA: ShiftCandidateEmployee = {
  id: 100, employeeCode: 'E100', fullName: 'Sara', roleTitle: 'Cashier',
  jobRoleId: 10, weeklyCompletedHours: 20, ratingScore: 4.6,
  riskStatusToken: 'TopPerformer', isPreferredForSite: true,
};

function dayDto() {
  return {
    siteId: 3,
    siteName: 'Cairo HQ',
    date: '2026-10-02',
    isDayOff: false,
    totalEstimatedLaborCost: 450,
    weekCalendarStrip: [
      { date: '2026-10-02', dayOfWeek: 5, status: 0, isSelected: true, isDayOff: false },
      { date: '2026-10-03', dayOfWeek: 6, status: 3, isSelected: false, isDayOff: false },
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
        ],
      },
    ],
  };
}

/** Ticket B: day board loads, posts, assigns, unassigns, and deletes. */
describe('ShiftBoardComponent', () => {
  let fixture: ComponentFixture<ShiftBoardComponent>;
  let component: ShiftBoardComponent;
  let httpMock: HttpTestingController;
  let navigated: unknown[][];

  async function setup(): Promise<void> {
    navigated = [];
    await TestBed.configureTestingModule({
      imports: [ShiftBoardComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Router, useValue: { navigate: (c: unknown[]) => { navigated.push(c); return Promise.resolve(true); } } },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ siteId: '3' }) } } },
        { provide: TranslateService, useValue: { instant: (key: string) => key, currentLang: () => 'en' } },
      ],
    })
      .overrideComponent(ShiftBoardComponent, {
        remove: {
          imports: [
            TranslatePipe,
            EmployeeStripComponent,
            ShiftTimelineComponent,
            CandidatePreviewComponent,
            TemplatePickerComponent,
          ],
        },
        add: {
          imports: [
            MockTranslatePipe,
            StubStripComponent,
            StubTimelineComponent,
            StubPreviewComponent,
            StubPickerComponent,
          ],
        },
      })
      .compileComponents();

    fixture = TestBed.createComponent(ShiftBoardComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  }

  function flushRoles(): void {
    const req = httpMock.expectOne((r) => r.url.endsWith('/jobs'));
    req.flush({ items: ROLES, totalCount: 2 });
  }

  function flushDay(): void {
    const req = httpMock.expectOne((r) => r.url.endsWith('/shifts/daily'));
    expect(req.request.params.get('siteId')).toBe('3');
    req.flush(dayDto());
    fixture.detectChanges();
  }

  function dirtyPreflight(): Record<string, unknown> {
    return {
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
    };
  }

  afterEach(() => httpMock.verify());

  it('should load the day schedule with site id and date on init', async () => {
    await setup();
    flushRoles();
    flushDay();

    expect(component.siteId()).toBe(3);
    expect(component.schedule()?.siteName).toBe('Cairo HQ');
    expect(component.schedule()?.totalCost).toBe(450);
    expect(component.schedule()?.week.length).toBe(2);
    expect(component.timelineBlocks().length).toBe(1);
    expect(component.timelineBlocks()[0]).toMatchObject({
      id: 11,
      start: 540,
      end: 720,
      locked: false,
      statusColor: '#22c55e',
    });
  });

  it('should reload the day when navigating weeks or selecting a day', async () => {
    await setup();
    flushRoles();
    flushDay();

    component.shiftWeek(7);
    const next = httpMock.expectOne((r) => r.url.endsWith('/shifts/daily'));
    expect(next.request.params.get('date')).not.toBe('2026-10-02');
    next.flush({ ...dayDto(), date: '2026-10-09', weekCalendarStrip: [], hourlyTimeline: [] });
    fixture.detectChanges();

    component.selectDay('2026-10-02');
    const back = httpMock.expectOne((r) => r.url.endsWith('/shifts/daily'));
    expect(back.request.params.get('date')).toBe('2026-10-02');
    back.flush(dayDto());
  });

  it('should post a block with TimeOnly times and reload the day', async () => {
    await setup();
    flushRoles();
    flushDay();

    component.rowRoleId.set(10);
    component.postBlock();

    const post = httpMock.expectOne((r) => r.url.endsWith('/shifts/blocks'));
    expect(post.request.method).toBe('POST');
    expect(post.request.body).toEqual({
      siteId: 3,
      date: component.selectedDate(),
      startTime: '09:00:00',
      endTime: '17:00:00',
      jobRoleId: 10,
      dispatchPolicy: 0,
    });
    post.flush({ shiftId: 12 });
    flushDay();
    expect(component.rowError()).toBeNull();
  });

  it('should reject posting without a role and send nothing', async () => {
    await setup();
    flushRoles();
    flushDay();

    component.postBlock();

    httpMock.expectNone((r) => r.url.endsWith('/shifts/blocks'));
    expect(component.rowError()).toBe('SHIFT_MANAGEMENT.BOARD.ROLE_REQUIRED');
  });

  it('should assign a dropped employee and reload on clean assign', async () => {
    await setup();
    flushRoles();
    flushDay();

    component.onStripDrop({ id: 11, data: SARA });

    const req = httpMock.expectOne((r) => r.url.endsWith('/shifts/blocks/11/assign'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body.employeeId).toBe(100);
    req.flush({ success: true, hasConflicts: false, warnings: [], wasOverridden: false, updatedCost: 500 });
    flushDay();
    expect(component.actionError()).toBeNull();
  });

  it('should surface conflicts and retry with override on confirm', async () => {
    await setup();
    flushRoles();
    flushDay();

    component.onStripDrop({ id: 11, data: SARA });

    const first = httpMock.expectOne((r) => r.url.endsWith('/shifts/blocks/11/assign'));
    first.flush({
      success: false,
      hasConflicts: true,
      warnings: [{ conflictType: 2, message: 'Overtime risk' }],
      wasOverridden: false,
      updatedCost: 500,
    });
    fixture.detectChanges();
    expect(component.conflict()?.warnings.length).toBe(1);

    component.overrideReason.set('Short shift');
    component.confirmConflict();

    const retry = httpMock.expectOne((r) => r.url.endsWith('/shifts/blocks/11/assign'));
    expect(retry.request.body).toEqual({
      employeeId: 100,
      confirmOverride: true,
      overrideReason: 'Short shift',
    });
    retry.flush({ success: true, hasConflicts: true, warnings: [], wasOverridden: true, updatedCost: 500 });
    flushDay();
  });

  it('should preview on strip click and assign when a bar is armed', async () => {
    await setup();
    flushRoles();
    flushDay();

    component.onStripClick(SARA);
    expect(component.previewEmployeeId()).toBe(100);

    component.previewEmployeeId.set(null);
    component.onTimelineAssign(11);
    expect(component.pendingAssign()).toBe(11);
    component.onStripClick(SARA);

    const req = httpMock.expectOne((r) => r.url.endsWith('/shifts/blocks/11/assign'));
    expect(req.request.body.employeeId).toBe(100);
    req.flush({ success: true, hasConflicts: false, warnings: [], wasOverridden: false, updatedCost: 500 });
    flushDay();
  });

  it('should unassign through the remove modal', async () => {
    await setup();
    flushRoles();
    flushDay();

    component.onTimelineDelete(11);
    expect(component.removeTarget()?.shiftId).toBe(11);
    component.unassignBlock();

    const req = httpMock.expectOne((r) => r.url.endsWith('/shifts/blocks/11/assign'));
    expect(req.request.method).toBe('DELETE');
    expect(req.request.params.get('action')).toBe('UnassignOnly');
    req.flush({ shiftBlockId: 11, actionTaken: 0, isDeleted: false, updatedCost: 450, siteCoverageStatus: 0 });
    flushDay();
  });

  it('should require confirmation before deleting a published block', async () => {
    await setup();
    flushRoles();
    const dto = dayDto();
    dto.hourlyTimeline[0].blocks[0] = { ...dto.hourlyTimeline[0].blocks[0], isPublished: true };
    const req = httpMock.expectOne((r) => r.url.endsWith('/shifts/daily'));
    req.flush(dto);
    fixture.detectChanges();

    component.onTimelineDelete(11);
    component.deleteBlock();
    httpMock.expectNone((r) => r.url.endsWith('/shifts/blocks/11/assign'));

    component.toggleConfirmPublished({ target: { checked: true } } as unknown as Event);
    component.deleteBlock();

    const del = httpMock.expectOne((r) => r.url.endsWith('/shifts/blocks/11/assign'));
    expect(del.request.params.get('action')).toBe('DeleteBlock');
    expect(del.request.params.get('confirmPublishedDeletion')).toBe('true');
    del.flush({ shiftBlockId: 11, actionTaken: 1, isDeleted: true, updatedCost: 0, siteCoverageStatus: 3 });
    flushDay();
  });

  it('should map published blocks to locked timeline bars', async () => {
    await setup();
    flushRoles();
    const dto = dayDto();
    dto.hourlyTimeline[0].blocks[0] = { ...dto.hourlyTimeline[0].blocks[0], isPublished: true };
    httpMock.expectOne((r) => r.url.endsWith('/shifts/daily')).flush(dto);
    fixture.detectChanges();

    expect(component.timelineBlocks()[0].locked).toBe(true);
  });

  it('should open the picker and reload the day after an apply', async () => {
    await setup();
    flushRoles();
    flushDay();

    expect(component.showPicker()).toBe(false);
    component.openPicker();
    expect(component.showPicker()).toBe(true);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('app-template-picker')).not.toBeNull();

    component.onPickerApplied();
    expect(component.showPicker()).toBe(false);
    flushDay();
  });

  it('should close the picker without reloading', async () => {
    await setup();
    flushRoles();
    flushDay();

    component.openPicker();
    component.closePicker();
    expect(component.showPicker()).toBe(false);
    httpMock.expectNone((r) => r.url.endsWith('/shifts/daily'));
  });

  it('should save the day as template with the entered name', async () => {
    await setup();
    flushRoles();
    flushDay();

    component.openSaveModal();
    expect(component.showSaveModal()).toBe(true);
    component.saveName.set('Morning rush');
    component.confirmSave();

    const req = httpMock.expectOne((r) =>
      r.url.endsWith(`/sites/3/dates/${component.selectedDate()}/save-as-template`),
    );
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ name: 'Morning rush' });
    req.flush({ id: 7, name: 'Morning rush', totalBlockCount: 4 });
    fixture.detectChanges();

    expect(component.savedTemplateName()).toBe('Morning rush');
    expect(component.showSaveModal()).toBe(true);
  });

  it('should send null name when the name is blank', async () => {
    await setup();
    flushRoles();
    flushDay();

    component.openSaveModal();
    component.saveName.set('   ');
    component.confirmSave();

    const req = httpMock.expectOne((r) => r.url.endsWith('/save-as-template'));
    expect(req.request.body).toEqual({ name: null });
    req.flush({ id: 8, name: 'Cairo HQ 2026-10-02', totalBlockCount: 2 });
    expect(component.savedTemplateName()).toBe('Cairo HQ 2026-10-02');
  });

  it('should show backend save errors inline without touching the board', async () => {
    await setup();
    flushRoles();
    flushDay();

    component.openSaveModal();
    component.confirmSave();

    const req = httpMock.expectOne((r) => r.url.endsWith('/save-as-template'));
    req.flush({ message: 'Day has no blocks.' }, { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();

    expect(component.saveError()).toBe('Day has no blocks.');
    expect(component.showSaveModal()).toBe(true);
    expect(component.schedule()?.blocks.length).toBe(1);
    httpMock.expectNone((r) => r.url.endsWith('/shifts/daily'));
  });

  it('should open the exception modal on dirty preflight and commit on confirm', async () => {
    await setup();
    flushRoles();
    flushDay();

    component.openPublish();

    const pre = httpMock.expectOne((r) => r.url.endsWith('/shifts/publish/preflight'));
    expect(pre.request.method).toBe('POST');
    expect(pre.request.body).toEqual({ siteId: 3, targetDates: [component.selectedDate()] });
    pre.flush(dirtyPreflight());
    fixture.detectChanges();

    expect(component.showPublishModal()).toBe(true);
    expect(component.preflight()?.unqualifiedCount).toBe(1);

    component.justification.set('Holiday cover');
    component.confirmPublish();

    const commit = httpMock.expectOne((r) => r.url.endsWith('/shifts/publish/commit'));
    expect(commit.request.body).toEqual({
      siteId: 3,
      targetDates: ['2026-10-02'],
      exceptionResolutions: { 11: 1, 12: 1 },
      overtimeJustification: 'Holiday cover',
    });
    commit.flush({
      success: true,
      publishedImmediatelyCount: 1,
      pendingHrApprovalCount: 0,
      skippedCount: 1,
      ids: [11],
      message: 'Published 1 shift, skipped 1.',
    });
    flushDay();

    expect(component.publishSuccess()?.publishedImmediatelyCount).toBe(1);
  });

  it('should commit directly with success feedback on clean preflight', async () => {
    await setup();
    flushRoles();
    flushDay();

    component.openPublish();

    const pre = httpMock.expectOne((r) => r.url.endsWith('/shifts/publish/preflight'));
    pre.flush({ ...dirtyPreflight(), unqualifiedAssignees: [], overtimeViolations: [], unqualifiedCount: 0, overtimeCount: 0, hasExceptions: false, canPublishImmediately: true, totalUnpublishedShiftsScanned: 1 });

    const commit = httpMock.expectOne((r) => r.url.endsWith('/shifts/publish/commit'));
    expect(commit.request.body).toEqual({ siteId: 3, targetDates: ['2026-10-02'] });
    commit.flush({
      success: true,
      publishedImmediatelyCount: 1,
      pendingHrApprovalCount: 0,
      skippedCount: 0,
      ids: [11],
      message: 'Published 1 shift.',
    });
    flushDay();

    expect(component.showPublishModal()).toBe(true);
    expect(component.publishSuccess()?.message).toBe('Published 1 shift.');
  });

  it('should leave the board untouched on publish cancel', async () => {
    await setup();
    flushRoles();
    flushDay();

    component.openPublish();

    const pre = httpMock.expectOne((r) => r.url.endsWith('/shifts/publish/preflight'));
    pre.flush(dirtyPreflight());
    fixture.detectChanges();
    expect(component.showPublishModal()).toBe(true);

    component.closePublishModal();

    expect(component.showPublishModal()).toBe(false);
    expect(component.publishSuccess()).toBeNull();
    expect(component.schedule()?.blocks.length).toBe(1);
    httpMock.expectNone((r) => r.url.endsWith('/shifts/publish/commit'));
    httpMock.expectNone((r) => r.url.endsWith('/shifts/daily'));
  });
});
