import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA, Pipe, type PipeTransform } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { Router, ActivatedRoute, convertToParamMap } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ShiftTemplateEditorComponent } from './shift-template-editor.component';
import type { ShiftTemplateDetails } from '../../data-access/models/shift-template.models';
import type {
  ShiftsJobRoleLookup,
  ShiftsSiteEmployee,
  ShiftsSiteLookup,
} from '../../data-access/models/shifts-lookups.models';

@Pipe({ name: 'translate', standalone: true })
class MockTranslatePipe implements PipeTransform {
  transform(value: string): string {
    return value;
  }
}

const SITES: ShiftsSiteLookup[] = [
  { id: 1, siteName: 'Cairo' },
  { id: 2, siteName: 'Giza' },
];

const ROLES: ShiftsJobRoleLookup[] = [{ id: 10, title: 'Cashier' }];

const EMPLOYEES: ShiftsSiteEmployee[] = [
  { employeeId: 100, fullName: 'Sara', roleName: 'Cashier' },
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
        remove: { imports: [TranslatePipe] },
        add: { imports: [MockTranslatePipe] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(ShiftTemplateEditorComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  }

  function flushLookups(): void {
    httpMock.expectOne((r) => r.url.endsWith('/sites')).flush(SITES);
    httpMock.expectOne((r) => r.url.endsWith('/job-roles')).flush(ROLES);
    fixture.detectChanges();
  }

  function flushDetails(): void {
    const req = httpMock.expectOne((r) => r.url.endsWith('/shift-templates/3'));
    expect(req.request.method).toBe('GET');
    req.flush(DETAILS);
    httpMock.expectOne((r) => r.url.endsWith('/sites/1/employees')).flush(EMPLOYEES);
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

  it('should reject an overlapping block', async () => {
    await setup(null);
    flushLookups();
    fillValidRow();
    component.postBlock();

    fillValidRow();
    component.postBlock();

    expect(component.blocks().length).toBe(1);
    expect(component.rowError()).toBe('SHIFT_TEMPLATES.EDITOR.BLOCK_OVERLAP');
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
});
