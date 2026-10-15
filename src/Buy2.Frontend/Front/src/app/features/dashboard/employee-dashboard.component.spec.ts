import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA, Pipe, type PipeTransform, signal } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { EmployeeDashboardComponent } from './employee-dashboard.component';
import { AuthService } from '../../core/auth/auth.service';

@Pipe({ name: 'translate', standalone: true })
class MockTranslatePipe implements PipeTransform {
  transform(value: string): string {
    return value;
  }
}

const POINTS = {
  currentBalance: 250,
  totalPointsRedeemed: 100,
  totalRewardsRedeemed: 2,
  totalRewardsCostPoints: 60,
};

const ATTENDANCE = {
  summary: {
    attendanceRate: 88.2,
    punctualityScore: 95.4,
    averageLatenessMinutes: 12.6,
    recordedHours: 140.5,
    targetHours: 160,
  },
  days: [],
};

const SUBMITTED_PAGE = (items: unknown[], totalCount: number, pageSize: number) => ({
  items,
  totalCount,
  pageNumber: 1,
  pageSize,
  totalPages: 1,
  hasPreviousPage: false,
  hasNextPage: false,
});

const MY_REQUEST = {
  id: 11,
  employeeId: 7,
  employeeName: 'You',
  employeeCode: null,
  requestType: 'Leave',
  category: 'HR',
  submittedAt: '2026-10-10T08:00:00Z',
  startDate: null,
  endDate: null,
  managerName: null,
  managerStatus: 'Approved',
  hrStatus: 'Pending',
  status: 'Approved',
  reason: '',
  attachmentsCount: 0,
};

describe('EmployeeDashboardComponent', () => {
  let fixture: ComponentFixture<EmployeeDashboardComponent>;
  let component: EmployeeDashboardComponent;
  let httpMock: HttpTestingController;

  async function setup(): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [EmployeeDashboardComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
        { provide: AuthService, useValue: { currentUser: signal({ userId: 7, email: 'you@corp.io', role: 'Employee' }) } },
      ],
    })
      .overrideComponent(EmployeeDashboardComponent, {
        remove: { imports: [TranslatePipe] },
        add: { imports: [MockTranslatePipe] },
      })
      .compileComponents();
    fixture = TestBed.createComponent(EmployeeDashboardComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  }

  function flushAll(): void {
    httpMock
      .expectOne((r) => r.url.split('?')[0].endsWith('/employees/7/points/summary'))
      .flush(POINTS);
    const calendar = httpMock.expectOne(
      (r) => r.url.split('?')[0].endsWith('/employees/7/attendance/calendar'),
    );
    expect(calendar.request.params.get('month')).toBe(String(new Date().getMonth() + 1));
    calendar.flush(ATTENDANCE);
    httpMock
      .expectOne(
        (r) =>
          r.url.split('?')[0].endsWith('/requests/submitted') &&
          r.params.get('pageSize') === '1',
      )
      .flush(SUBMITTED_PAGE([], 3, 1));
    httpMock
      .expectOne(
        (r) =>
          r.url.split('?')[0].endsWith('/requests/submitted') &&
          r.params.get('pageSize') === '5',
      )
      .flush(SUBMITTED_PAGE([MY_REQUEST], 1, 5));
    fixture.detectChanges();
  }

  afterEach(() => httpMock.verify());

  it('renders personal KPIs, attendance figures, requests and points', async () => {
    await setup();
    flushAll();

    expect(component.points()?.currentBalance).toBe(250);
    expect(component.pendingCount()).toBe(3);
    expect(component.myRequests().length).toBe(1);

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('DASHBOARD.TITLE');
    expect(text).toContain('250');
    expect(text).toContain('DASHBOARD.EMPLOYEE.STATUS.APPROVED');
    expect(text).toContain('DASHBOARD.EMPLOYEE.PUNCTUALITY');
    expect(text).toContain('DASHBOARD.EMPLOYEE.REWARDS_COST');
  });

  it('scopes both request queries to the signed-in employee', async () => {
    await setup();

    const pendingReq = httpMock.expectOne(
      (r) => r.url.split('?')[0].endsWith('/requests/submitted') && r.params.get('pageSize') === '1',
    );
    expect(pendingReq.request.params.get('search')).toBe('7');
    expect(pendingReq.request.params.get('overallStatus')).toBe('Pending');
    pendingReq.flush(SUBMITTED_PAGE([], 3, 1));

    const mineReq = httpMock.expectOne(
      (r) => r.url.split('?')[0].endsWith('/requests/submitted') && r.params.get('pageSize') === '5',
    );
    expect(mineReq.request.params.get('search')).toBe('7');
    expect(mineReq.request.params.get('overallStatus')).toBeNull();
    mineReq.flush(SUBMITTED_PAGE([MY_REQUEST], 1, 5));

    httpMock
      .expectOne((r) => r.url.split('?')[0].endsWith('/employees/7/points/summary'))
      .flush(POINTS);
    httpMock
      .expectOne((r) => r.url.split('?')[0].endsWith('/employees/7/attendance/calendar'))
      .flush(ATTENDANCE);
    fixture.detectChanges();
  });

  it('shows the error state when any dashboard query fails', async () => {
    await setup();

    httpMock
      .expectOne((r) => r.url.split('?')[0].endsWith('/employees/7/attendance/calendar'))
      .flush(ATTENDANCE);
    httpMock
      .expectOne(
        (r) =>
          r.url.split('?')[0].endsWith('/requests/submitted') &&
          r.params.get('pageSize') === '1',
      )
      .flush(SUBMITTED_PAGE([], 3, 1));
    httpMock
      .expectOne(
        (r) =>
          r.url.split('?')[0].endsWith('/requests/submitted') &&
          r.params.get('pageSize') === '5',
      )
      .flush(SUBMITTED_PAGE([MY_REQUEST], 1, 5));
    httpMock
      .expectOne((r) => r.url.split('?')[0].endsWith('/employees/7/points/summary'))
      .error(new ProgressEvent('error'));

    expect(component.error()).toBe(true);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('DASHBOARD.ERROR');
  });
});
