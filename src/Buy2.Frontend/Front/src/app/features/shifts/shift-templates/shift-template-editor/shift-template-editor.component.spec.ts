import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA, Component, Pipe, input, output, type PipeTransform } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { Router, ActivatedRoute, convertToParamMap } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ShiftTemplateEditorComponent, type WorkingBlock } from './shift-template-editor.component';
import { EmployeeStripComponent } from '../employee-strip/employee-strip.component';
import { ShiftTimelineComponent } from '../../ui/shift-timeline/shift-timeline.component';
import type { ShiftTemplateDetails } from '../../data-access/models/shift-template.models';
import type {
  ShiftCandidateEmployee,
  ShiftsJobRoleLookup,
  ShiftsSiteLookup,
} from '../../data-access/models/shifts-lookups.models';

@Pipe({ name: 'translate', standalone: true })
class MockTranslatePipe implements PipeTransform {
  transform(value: string): string {
    return value;
  }
}

/** Strip stub: assignment state is driven through onStripPage, not gestures. */
@Component({ selector: 'app-employee-strip', standalone: true, template: '' })
class StubStripComponent {
  readonly siteIds = input<number[]>([]);
  readonly assignedIds = input<number[]>([]);
  readonly pageLoaded = output<{ page: number; items: ShiftCandidateEmployee[] }>();
}

/** Timeline stub: bar gestures are covered in the timeline's own spec. */
@Component({ selector: 'app-shift-timeline', standalone: true, template: '' })
class StubTimelineComponent {
  readonly blocks = input<unknown[]>([]);
  readonly rangeStart = input<number | null>(null);
  readonly rangeEnd = input<number | null>(null);
  readonly editBlock = output<string | number>();
  readonly assignBlock = output<string | number>();
  readonly deleteBlock = output<string | number>();
  readonly dropOnBlock = output<{ id: string | number; data: unknown }>();
}

const SITES: ShiftsSiteLookup[] = [
  { id: 1, siteName: 'Cairo' },
  { id: 2, siteName: 'Giza' },
];

const ROLES: ShiftsJobRoleLookup[] = [{ id: 10, title: 'Cashier' }];

const EMPLOYEES: ShiftCandidateEmployee[] = [
  {
    id: 100, employeeCode: 'E100', fullName: 'Sara', roleTitle: 'Cashier',
    jobRoleId: 10, weeklyCompletedHours: 20, ratingScore: 4.6,
    riskStatusToken: 'TopPerformer', isPreferredForSite: true,
  },
  {
    id: 101, employeeCode: 'E101', fullName: 'Omar', roleTitle: 'Guard',
    jobRoleId: 11, weeklyCompletedHours: 40, ratingScore: 3.5,
    riskStatusToken: 'OvertimeRisk', isPreferredForSite: false,
  },
];

const DETAILS: ShiftTemplateDetails = {
  id: 3,
  name: 'Morning',
  startTime: '09:00 AM',
  endTime: '05:00 PM',
  creationDate: '2024-01-01',
  lastUpdated: '2024-02-01',
  numberOfAssignedSites: 1,
  sites: [{ siteId: 1, siteName: 'Cairo' }],
  shiftBlocks: [
    {
      id: 11,
      startTime: '09:00 AM',
      endTime: '12:00 PM',
      jobRoleId: 10,
      jobRoleTitle: 'Cashier',
      assignedUserId: null,
      assignedUserName: null,
    },
  ],
};

/** Ticket #327: shared create/edit form — prefill, block builder, save. */
describe('ShiftTemplateEditorComponent', () => {
  let fixture: ComponentFixture<ShiftTemplateEditorComponent>;
  let component: ShiftTemplateEditorComponent;
  let httpMock: HttpTestingController;
  let navigated: unknown[][];

  async function setup(idParam: string | null): Promise<void> {
    navigated = [];
    await TestBed.configureTestingModule({
      imports: [ShiftTemplateEditorComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Router, useValue: { navigate: (c: unknown[]) => { navigated.push(c); return Promise.resolve(true); } } },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap(idParam === null ? {} : { id: idParam }) } } },
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
      ],
    })
      .overrideComponent(ShiftTemplateEditorComponent, {
        remove: { imports: [TranslatePipe, EmployeeStripComponent, ShiftTimelineComponent] },
        add: { imports: [MockTranslatePipe, StubStripComponent, StubTimelineComponent] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(ShiftTemplateEditorComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  }

  function flushLookups(): void {
    httpMock.expectOne((r) => r.url.endsWith('/sites')).flush(SITES);
    httpMock.expectOne((r) => r.url.endsWith('/jobs')).flush({ items: ROLES });
    fixture.detectChanges();
  }

  function flushDetails(): void {
    const req = httpMock.expectOne((r) => r.url.endsWith('/shift-templates/3'));
    expect(req.request.method).toBe('GET');
    req.flush(DETAILS);
    // The strip feeds the picker through pageLoaded; no per-site fan-out.
    component.onStripPage({ page: 1, items: EMPLOYEES });
    fixture.detectChanges();
  }

  /** Feeds the picker from the strip in create mode. */
  function feedStrip(): void {
    component.onStripPage({ page: 1, items: EMPLOYEES });
    fixture.detectChanges();
  }

  /** Prefills the builder row with a valid in-range block. */
  function fillValidRow(): void {
    component.rowStartHm.set('01:00');
    component.rowStartMer.set('PM');
    component.rowEndHm.set('02:00');
    component.rowEndMer.set('PM');
    component.rowRoleId.set(10);
    component.rowEmployeeId.set(null);
  }

  /** Makes the whole form valid: name + one site + one block. */
  function fillValidForm(): void {
    component.name.set('Morning');
    component.selectedSiteIds.set([1]);
    fillValidRow();
    component.postBlock();
    expect(component.blocks().length).toBe(1);
  }

  afterEach(() => httpMock.verify());

  it('should load lookups without fetching a template in create mode', async () => {
    await setup(null);
    flushLookups();

    expect(component.isEdit()).toBe(false);
    expect(component.templateId()).toBeNull();
    expect(component.sites()).toEqual(SITES);
    expect(component.roles()).toEqual(ROLES);
  });

  it('should prefill name, sites, times, and blocks in edit mode', async () => {
    await setup('3');
    flushLookups();
    flushDetails();

    expect(component.isEdit()).toBe(true);
    expect(component.name()).toBe('Morning');
    expect(component.selectedSiteIds()).toEqual([1]);
    expect(component.startHm()).toBe('09:00');
    expect(component.startMer()).toBe('AM');
    expect(component.endHm()).toBe('05:00');
    expect(component.endMer()).toBe('PM');
    expect(component.blocks().length).toBe(1);
    expect(component.blocks()[0].id).toBe(11);
    expect(component.blocks()[0].start).toBe(540);
    expect(component.blocks()[0].end).toBe(720);
    expect(component.employees()).toEqual(EMPLOYEES);
  });

  it('should add a block through the builder row', async () => {
    await setup(null);
    flushLookups();
    fillValidRow();
    component.postBlock();

    expect(component.rowError()).toBeNull();
    expect(component.blocks().length).toBe(1);
    expect(component.blockLabel(component.blocks()[0])).toBe('01:00 PM - 02:00 PM');
  });
  it('should allow an overlapping block', async () => {
    await setup(null);
    flushLookups();
    fillValidRow();
    component.postBlock();

    fillValidRow();
    component.postBlock();

    expect(component.blocks().length).toBe(2);
    expect(component.rowError()).toBeNull();
  });

  it('should remove a block after the delete modal confirms', async () => {
    await setup(null);
    flushLookups();
    fillValidRow();
    component.postBlock();

    component.askDeleteBlock(component.blocks()[0]);
    expect(component.showDeleteBlockModal()).toBe(true);
    component.confirmDeleteBlock();

    expect(component.blocks().length).toBe(0);
    expect(component.showDeleteBlockModal()).toBe(false);
  });

  it('should not send any request when the form is invalid', async () => {
    await setup(null);
    flushLookups();

    component.save();

    expect(component.formErrors().length).toBeGreaterThan(0);
    expect(component.formErrors()).toContain('SHIFT_TEMPLATES.EDITOR.NAME_REQUIRED');
  });

  it('should POST the payload and navigate back in create mode', async () => {
    await setup(null);
    flushLookups();
    fillValidForm();

    component.save();

    const req = httpMock.expectOne((r) => r.url.endsWith('/shift-templates'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body.name).toBe('Morning');
    expect(req.request.body.siteIds).toEqual([1]);
    expect(req.request.body.startTime).toBe('09:00 AM');
    expect(req.request.body.endTime).toBe('05:00 PM');
    expect(req.request.body.shiftBlocks.length).toBe(1);
    expect(req.request.body.shiftBlocks[0].jobRoleId).toBe(10);
    req.flush({ ...DETAILS, id: 9 });
    fixture.detectChanges();

    expect(navigated).toEqual([['/scheduling/shift-templates']]);
  });

  it('should PUT the full-replace payload with block ids in edit mode', async () => {
    await setup('3');
    flushLookups();
    flushDetails();

    component.save();

    const req = httpMock.expectOne((r) => r.url.endsWith('/shift-templates/3'));
    expect(req.request.method).toBe('PUT');
    expect(req.request.body.shiftBlocks.length).toBe(1);
    expect(req.request.body.shiftBlocks[0].id).toBe(11);
    req.flush(DETAILS);
    fixture.detectChanges();

    expect(navigated).toEqual([['/scheduling/shift-templates']]);
  });

  it('should unassign through the remove control', async () => {
    await setup(null);
    flushLookups();
    feedStrip();
    fillValidForm();

    component.editBlock(component.blocks()[0]);
    component.rowEmployeeId.set(100);
    component.postBlock();

    expect(component.blocks()[0].assignedUserId).toBe(100);

    component.unassignBlock(component.blocks()[0]);

    expect(component.blocks()[0].assignedUserId).toBeNull();
    expect(component.blocks()[0].assignedUserName).toBeNull();
    expect(component.assignedIds()).toEqual([]);
  });

  it('should assign a strip card dropped on a timeline bar and move it between blocks', async () => {
    await setup(null);
    flushLookups();
    feedStrip();
    fillValidForm();
    component.rowStartHm.set('02:00');
    component.rowStartMer.set('PM');
    component.rowEndHm.set('03:00');
    component.rowEndMer.set('PM');
    component.rowRoleId.set(10);
    component.postBlock();
    expect(component.blocks().length).toBe(2);

    const [first, second] = component.blocks();
    component.onStripDrop({ id: first.clientId, data: EMPLOYEES[0] });
    expect(component.blocks()[0].assignedUserId).toBe(100);
    expect(component.blocks()[0].assignedUserName).toBe('Sara');
    expect(component.assignedIds()).toEqual([100]);

    component.onStripDrop({ id: second.clientId, data: EMPLOYEES[0] });
    expect(component.blocks()[0].assignedUserId).toBeNull();
    expect(component.blocks()[1].assignedUserId).toBe(100);
    expect(component.assignedIds()).toEqual([100]);
  });

  it('should ignore non-employee drop payloads', async () => {
    await setup(null);
    flushLookups();
    feedStrip();
    fillValidForm();

    component.onStripDrop({ id: component.blocks()[0].clientId, data: null });
    component.onStripDrop({ id: component.blocks()[0].clientId, data: { id: 'x' } });

    expect(component.blocks()[0].assignedUserId).toBeNull();
    expect(component.assignedIds()).toEqual([]);
  });

  it('should feed mapped blocks to the shared timeline', async () => {
    await setup('3');
    flushLookups();
    flushDetails();

    expect(fixture.nativeElement.querySelector('app-shift-timeline')).not.toBeNull();
    expect(component.timelineBlocks()).toEqual([
      {
        id: component.blocks()[0].clientId,
        start: 540,
        end: 720,
        label: 'Cashier',
        assigned: false,
        assigneeName: null,
      },
    ]);
  });

  it('should reload the block into the builder through the timeline edit output', async () => {
    await setup('3');
    flushLookups();
    flushDetails();

    component.onTimelineEdit(component.blocks()[0].clientId);

    expect(component.editingClientId()).toBe(component.blocks()[0].clientId);
    expect(component.rowRoleId()).toBe(10);
  });

  it('should focus the employee picker through the timeline assign output', async () => {
    await setup('3');
    flushLookups();
    flushDetails();

    component.onTimelineAssign(component.blocks()[0].clientId);
    fixture.detectChanges();

    expect(component.editingClientId()).toBe(component.blocks()[0].clientId);
    expect(document.activeElement?.getAttribute('aria-label')).toBe('Block employee');
  });

  it('should open the delete modal through the timeline delete output', async () => {
    await setup('3');
    flushLookups();
    flushDetails();

    component.onTimelineDelete(component.blocks()[0].clientId);

    expect(component.showDeleteBlockModal()).toBe(true);
  });
});
