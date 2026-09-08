import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { expectTypeOf } from 'vitest';
import type { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import type { EmployeePerformanceOverview, PerformanceFilters } from '../models/view-employee/employee-performance';
import type { EmployeePerformanceMetricDetail } from '../models/view-employee/employee-performance-metric';
import { EmployeeDetailService } from './employee-detail.service';

const base = `${environment.baseUrl}/employees`;
const range = { from: '2026-09-01T00:00:00Z', to: '2026-09-08T00:00:00Z', period: 'thisMonth' };
const overview: EmployeePerformanceOverview = {
  employeeId: 1, dateRangeResolved: range, overallWeightedScore: 80,
  ratingLabel: 'Good Performance',
  tasksSummary: { totalTasks: 0, todoCount: 0, inProgressCount: 0, completedCount: 0,
    overdueCount: 0, deadlineCompliancePercentage: 0 },
  achievements: [], chartTrendPoints: [], submissionsDetail: [],
};
const metric: EmployeePerformanceMetricDetail = {
  employeeId: 1, metricId: 2, metricName: 'Sales', metricDescription: '', weight: 1,
  targetScore: 90, unit: '%', allTimeAverageScore: 80, periodAverageScore: 80,
  periodRatingLabel: 'Good Performance', dateRangeResolved: range, monthlyTrends: [], submissions: [],
};

describe('EmployeeDetailService Performance', () => {
  let service: EmployeeDetailService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(EmployeeDetailService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('limits the filter model to the backend keys and numeric days', () => {
    expectTypeOf<keyof PerformanceFilters>().toEqualTypeOf<'period' | 'days' | 'from' | 'to'>();
    expectTypeOf<PerformanceFilters['days']>().toEqualTypeOf<number | undefined>();
  });

  for (const kind of ['overview', 'metric'] as const) {
    const url = `${base}/1/performance/${kind === 'overview' ? 'overview' : 'metrics/2'}`;
    const response = kind === 'overview' ? overview : metric;
    const read = (s: EmployeeDetailService, filters?: PerformanceFilters): Observable<EmployeePerformanceOverview | EmployeePerformanceMetricDetail> => kind === 'overview'
      ? s.getPerformanceOverview(1, filters) : s.getPerformanceMetricDetail(1, 2, filters);
    const load = (s: EmployeeDetailService, days: number) => kind === 'overview'
      ? s.loadPerformanceOverview(1, { days }) : s.loadPerformanceMetricDetail(1, 2, { days });
    const data = (s: EmployeeDetailService) => kind === 'overview' ? s.performanceOverview() : s.performanceMetricDetail();
    const loading = (s: EmployeeDetailService) => kind === 'overview' ? s.performanceOverviewLoading() : s.performanceMetricDetailLoading();
    const error = (s: EmployeeDetailService) => kind === 'overview' ? s.performanceOverviewError() : s.performanceMetricDetailError();
    const clear = (s: EmployeeDetailService) => kind === 'overview' ? s.clearPerformanceOverview() : s.clearPerformanceMetricDetail();

    it(`uses the exact ${kind} GET URL without default filters`, () => {
      read(service).subscribe(value => expect(value).toEqual(response));
      const req = http.expectOne(url);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.keys()).toEqual([]);
      req.flush(response);
    });

    it(`serializes only supported ${kind} filters, even with extra runtime properties`, () => {
      const filters = { period: ' thisMonth ', days: 30, from: range.from, to: range.to,
        month: 9, year: 2026, dateFrom: range.from, dateTo: range.to,
        page: 1, pageSize: 10, sort: 'score', search: 'Sales' };
      read(service, filters).subscribe();
      const req = http.expectOne(r => r.url === url);
      expect(req.request.params.keys().sort()).toEqual(['days', 'from', 'period', 'to']);
      expect(req.request.params.get('period')).toBe('thisMonth');
      expect(req.request.params.get('days')).toBe('30');
      expect(req.request.params.get('from')).toBe(range.from);
      expect(req.request.params.get('to')).toBe(range.to);
      expect(filters.days).toBe(30);
      req.flush(response);
    });

    it(`omits empty ${kind} filters but sends numeric zero for backend validation`, () => {
      read(service, { period: ' ', from: '', to: undefined, days: 0 }).subscribe();
      const req = http.expectOne(r => r.url === url);
      expect(req.request.params.keys()).toEqual(['days']);
      expect(req.request.params.get('days')).toBe('0');
      req.flush(response);
    });

    it(`ignores old ${kind} success while newer filters are loading`, () => {
      load(service, 30);
      const old = http.expectOne(`${url}?days=30`);
      load(service, 90);
      const latest = http.expectOne(`${url}?days=90`);
      old.flush(response);
      expect(data(service)).toBeNull();
      expect(loading(service)).toBe(true);
      latest.flush(response);
      expect(data(service)).toEqual(response);
      expect(loading(service)).toBe(false);
    });

    it(`ignores old ${kind} errors while a newer request is pending`, () => {
      load(service, 30);
      const old = http.expectOne(`${url}?days=30`);
      load(service, 90);
      const latest = http.expectOne(`${url}?days=90`);
      old.flush('old error', { status: 404, statusText: 'Not Found' });
      expect(error(service)).toBeNull();
      expect(loading(service)).toBe(true);
      latest.flush(response);
    });

    it(`prevents late ${kind} success from overwriting a newer result`, () => {
      load(service, 30);
      const old = http.expectOne(`${url}?days=30`);
      load(service, 90);
      const latest = http.expectOne(`${url}?days=90`);
      latest.flush(response);
      old.flush({ ...response, employeeId: 999 });
      expect(data(service)).toEqual(response);
    });

    it(`invalidates pending ${kind} responses on clear`, () => {
      load(service, 30);
      const req = http.expectOne(`${url}?days=30`);
      clear(service);
      req.flush(response);
      expect(data(service)).toBeNull();
      expect(loading(service)).toBe(false);
      expect(error(service)).toBeNull();
    });

    for (const status of [400, 401, 404]) {
      it(`preserves ${kind} HTTP ${status} separately and resets error on retry`, () => {
        load(service, 30);
        http.expectOne(`${url}?days=30`).flush({ reason: 'failed' }, { status, statusText: 'Error' });
        expect(error(service)?.status).toBe(status);
        expect(error(service)?.error).toEqual({ reason: 'failed' });
        expect(data(service)).toBeNull();
        expect(loading(service)).toBe(false);
        expect(kind === 'overview' ? service.performanceMetricDetailError() : service.performanceOverviewError()).toBeNull();
        load(service, 90);
        expect(error(service)).toBeNull();
        http.expectOne(`${url}?days=90`).flush(response);
      });
    }
  }

  it('protects overview against an older employee response', () => {
    service.loadPerformanceOverview(1);
    const old = http.expectOne(`${base}/1/performance/overview`);
    service.loadPerformanceOverview(2);
    http.expectOne(`${base}/2/performance/overview`).flush({ ...overview, employeeId: 2 });
    old.flush(overview);
    expect(service.performanceOverview()?.employeeId).toBe(2);
  });

  it('protects metric details against older metric and employee responses', () => {
    service.loadPerformanceMetricDetail(1, 2);
    const first = http.expectOne(`${base}/1/performance/metrics/2`);
    service.loadPerformanceMetricDetail(1, 3);
    const second = http.expectOne(`${base}/1/performance/metrics/3`);
    first.flush(metric);
    expect(service.performanceMetricDetail()).toBeNull();
    expect(service.performanceMetricDetailLoading()).toBe(true);
    service.loadPerformanceMetricDetail(2, 4);
    http.expectOne(`${base}/2/performance/metrics/4`).flush({ ...metric, employeeId: 2, metricId: 4 });
    second.flush({ ...metric, metricId: 3 });
    expect(service.performanceMetricDetail()?.employeeId).toBe(2);
    expect(service.performanceMetricDetail()?.metricId).toBe(4);
  });

  it('keeps same-employee overview and detail requests independent', () => {
    service.loadPerformanceOverview(1);
    service.loadPerformanceMetricDetail(1, 2);
    http.expectOne(`${base}/1/performance/overview`).flush(overview);
    expect(service.performanceMetricDetailLoading()).toBe(true);
    http.expectOne(`${base}/1/performance/metrics/2`).flush(metric);
    expect(service.performanceOverview()).toEqual(overview);
    expect(service.performanceMetricDetail()).toEqual(metric);
  });

  for (const reset of ['employee change', 'profile clear'] as const) {
    it(`invalidates both pending stores on ${reset}`, () => {
      service.loadPerformanceOverview(1);
      service.loadPerformanceMetricDetail(1, 2);
      const oldOverview = http.expectOne(`${base}/1/performance/overview`);
      const oldMetric = http.expectOne(`${base}/1/performance/metrics/2`);
      if (reset === 'employee change') {
        service.loadDetailEmployee(2);
        http.expectOne(`${base}/2`).flush('missing', { status: 404, statusText: 'Not Found' });
      } else {
        service.clearDetailEmployee();
      }
      oldOverview.flush(overview);
      oldMetric.flush(metric);
      expect(service.performanceOverview()).toBeNull();
      expect(service.performanceMetricDetail()).toBeNull();
      expect(service.performanceOverviewLoading()).toBe(false);
      expect(service.performanceMetricDetailLoading()).toBe(false);
    });
  }
});
