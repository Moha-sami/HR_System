import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA, Pipe, type PipeTransform } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AdminDashboardComponent } from './admin-dashboard.component';

@Pipe({ name: 'translate', standalone: true })
class MockTranslatePipe implements PipeTransform {
  transform(value: string): string {
    return value;
  }
}

const COVERAGE = [
  {
    siteId: 1,
    siteName: 'Cairo HQ',
    regionName: 'Cairo',
    address: '12 Nile St',
    isSmartAssignmentEnabled: false,
    isSmartPostingEnabled: false,
    days: [
      { date: '2026-10-15', status: 0, totalShifts: 4, openShifts: 1, filledShifts: 3 },
      { date: '2026-10-16', status: 2, totalShifts: 2, openShifts: 2, filledShifts: 0 },
      { date: '2026-10-17', status: 3, totalShifts: 0, openShifts: 0, filledShifts: 0 },
    ],
  },
];

const PENDING = {
  items: [
    {
      id: 5,
      employeeId: 9,
      employeeName: 'Sara Adel',
      employeeCode: null,
      requestType: 'Leave',
      category: 'HR',
      submittedAt: '2026-10-14T09:00:00Z',
      startDate: null,
      endDate: null,
      managerName: null,
      managerStatus: 'Pending',
      hrStatus: 'Pending',
      status: 'Pending',
      reason: '',
      attachmentsCount: 0,
    },
  ],
  totalCount: 4,
  pageNumber: 1,
  pageSize: 5,
  totalPages: 1,
  hasPreviousPage: false,
  hasNextPage: false,
};

const JOBS = {
  items: [
    { id: 3, title: 'Security Guard', assignedEmployeesCount: 8 },
    { id: 4, title: 'Receptionist', assignedEmployeesCount: 4 },
  ],
  totalCount: 2,
  pageNumber: 1,
  pageSize: 50,
  totalPages: 1,
};

describe('AdminDashboardComponent', () => {
  let fixture: ComponentFixture<AdminDashboardComponent>;
  let component: AdminDashboardComponent;
  let httpMock: HttpTestingController;

  async function setup(): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [AdminDashboardComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
      ],
    })
      .overrideComponent(AdminDashboardComponent, {
        remove: { imports: [TranslatePipe] },
        add: { imports: [MockTranslatePipe] },
      })
      .compileComponents();
    fixture = TestBed.createComponent(AdminDashboardComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  }

  function flushAll(): void {
    httpMock
      .expectOne((r) => r.url.split('?')[0].endsWith('/sites/shifts-overview'))
      .flush(COVERAGE);
    httpMock
      .expectOne((r) => r.url.split('?')[0].endsWith('/requests/submitted'))
      .flush(PENDING);
    httpMock
      .expectOne((r) => r.url.split('?')[0].endsWith('/employees'))
      .flush({ items: [], totalCount: 12, page: 1, pageSize: 1 });
    httpMock.expectOne((r) => r.url.split('?')[0].endsWith('/jobs')).flush(JOBS);
    fixture.detectChanges();
  }

  afterEach(() => httpMock.verify());

  it('loads KPIs, coverage, docket and job ledger on init', async () => {
    await setup();
    flushAll();

    expect(component.headcount()).toBe(12);
    expect(component.pendingCount()).toBe(4);
    expect(component.totals().open).toBe(3);
    expect(component.coverage().length).toBe(1);
    expect(component.jobs().length).toBe(2);

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('DASHBOARD.TITLE');
    expect(text).toContain('Cairo HQ');
    expect(text).toContain('Sara Adel');
    expect(text).toContain('Security Guard');
    expect(text).toContain('DASHBOARD.DOCKET.REVIEW');
    expect(fixture.nativeElement.querySelector('a[href*="/requests/decision/5"]')).not.toBeNull();
  });

  it('normalizes numeric coverage statuses into the site overall status', async () => {
    await setup();
    flushAll();

    const site = component.coverage()[0];
    expect(site.days[0].status).toBe('Full');
    expect(site.days[1].status).toBe('Critical');
    expect(site.days[2].status).toBe('NoShifts');
    expect(component.siteStatuses().get(1)).toBe('Critical');
    expect(component.siteOpen(site)).toBe(3);
    expect(component.siteFilled(site)).toBe(3);
    expect(component.siteTotal(site)).toBe(6);
  });

  it('shows the error state and recovers on retry', async () => {
    await setup();

    httpMock.expectOne((r) => r.url.split('?')[0].endsWith('/requests/submitted')).flush(PENDING);
    httpMock
      .expectOne((r) => r.url.split('?')[0].endsWith('/employees'))
      .flush({ items: [], totalCount: 12, page: 1, pageSize: 1 });
    httpMock.expectOne((r) => r.url.split('?')[0].endsWith('/jobs')).flush(JOBS);
    httpMock
      .expectOne((r) => r.url.split('?')[0].endsWith('/sites/shifts-overview'))
      .error(new ProgressEvent('error'));

    expect(component.error()).toBe(true);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('DASHBOARD.ERROR');

    component.load();
    fixture.detectChanges();
    flushAll();

    expect(component.error()).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('Cairo HQ');
  });

  it('shows the empty docket state when nothing is pending', async () => {
    await setup();

    httpMock.expectOne((r) => r.url.split('?')[0].endsWith('/sites/shifts-overview')).flush([]);
    httpMock.expectOne((r) => r.url.split('?')[0].endsWith('/requests/submitted')).flush({ ...PENDING, items: [], totalCount: 0 });
    httpMock.expectOne((r) => r.url.split('?')[0].endsWith('/employees')).flush({ items: [], totalCount: 0, page: 1, pageSize: 1 });
    httpMock.expectOne((r) => r.url.split('?')[0].endsWith('/jobs')).flush({ ...JOBS, items: [] });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('DASHBOARD.DOCKET.EMPTY');
    expect(fixture.nativeElement.textContent).toContain('DASHBOARD.TABLE.EMPTY_COVERAGE');
  });

  it('requests pending approvals server-side with the Pending overall status', async () => {
    await setup();

    const pendingReq = httpMock.expectOne((r) => r.url.split('?')[0].endsWith('/requests/submitted'));
    expect(pendingReq.request.params.get('overallStatus')).toBe('Pending');
    expect(pendingReq.request.params.get('pageSize')).toBe('5');
    pendingReq.flush(PENDING);

    httpMock.expectOne((r) => r.url.split('?')[0].endsWith('/sites/shifts-overview')).flush([]);
    httpMock.expectOne((r) => r.url.split('?')[0].endsWith('/employees')).flush({ items: [], totalCount: 0, page: 1, pageSize: 1 });
    httpMock.expectOne((r) => r.url.split('?')[0].endsWith('/jobs')).flush({ ...JOBS, items: [] });
    fixture.detectChanges();
  });
});
