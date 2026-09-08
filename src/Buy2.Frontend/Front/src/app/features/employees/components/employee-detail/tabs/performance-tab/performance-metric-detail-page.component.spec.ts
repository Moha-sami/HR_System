import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import { EmployeeDetailService } from '../../../../services/employee-detail.service';
import type { EmployeeProfileDto } from '../../../../models/view-employee/information-tab.models';
import type { EmployeePerformanceMetricDetail } from '../../../../models/view-employee/employee-performance-metric';
import { environment } from '../../../../../../../environments/environment';
import { PerformanceMetricDetailPageComponent } from './performance-metric-detail-page.component';
import { LanguageService } from '../../../../../../core/services/language.service';

const base = `${environment.baseUrl}/employees`;
const result: EmployeePerformanceMetricDetail = {
  employeeId: 1, metricId: 3, metricName: 'Sales', metricDescription: '', weight: 0,
  targetScore: 0, unit: '%', allTimeAverageScore: 0, periodAverageScore: 0,
  periodRatingLabel: 'Needs Improvement', dateRangeResolved: { from: '', to: '', period: 'thisMonth' },
  monthlyTrends: [], submissions: [],
};

describe('Performance Metric Detail loading', () => {
  let params: BehaviorSubject<ReturnType<typeof convertToParamMap>>;
  beforeEach(() => {
    params = new BehaviorSubject(convertToParamMap({ metricId: '3' }));
    TestBed.configureTestingModule({
      imports: [PerformanceMetricDetailPageComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideTranslateService(), provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: params, snapshot: { paramMap: params.value } } }],
    });
  });
  afterEach(() => TestBed.inject(HttpTestingController).verify());
  function setup(id = 1) {
    const service = TestBed.inject(EmployeeDetailService);
    service.detailEmployee.set({ id } as EmployeeProfileDto);
    const fixture = TestBed.createComponent(PerformanceMetricDetailPageComponent);
    fixture.detectChanges();
    return { service, fixture, http: TestBed.inject(HttpTestingController) };
  }

  it('loads employee 1 metric 3 with thisMonth and renders valid zeros plus Back', () => {
    const { fixture, http } = setup();
    http.expectOne(`${base}/1/performance/metrics/3?period=thisMonth`).flush(result);
    fixture.detectChanges();
    for (const field of ['weight', 'all-time', 'score']) {
      expect(fixture.nativeElement.querySelector(`[data-testid="metric-${field}"]`).textContent.trim()).toBe('0');
    }
    expect(fixture.nativeElement.querySelector('[data-testid="metric-target"]').textContent.trim()).toBe('0 %');
    expect(fixture.nativeElement.querySelector('a').getAttribute('href')).toBe('/employees/1/performance');
  });

  for (const [score, visual] of [[80, 80], [120, 100], [-5, 0]]) {
    it(`renders score ${score} unchanged with visual ${visual}`, () => {
      const { fixture, http } = setup();
      http.expectOne(`${base}/1/performance/metrics/3?period=thisMonth`).flush({
        ...result, periodAverageScore: score, targetScore: 90, unit: 'units', weight: 2.5,
        allTimeAverageScore: 72, metricDescription: '  ',
      });
      fixture.detectChanges();
      const text = (field: string) => fixture.nativeElement.querySelector(`[data-testid="metric-${field}"]`).textContent.trim();
      expect(text('score')).toBe(String(score));
      expect(text('name')).toBe('Sales');
      expect(text('description')).toBe('EMPLOYEE_DETAIL.NOT_AVAILABLE');
      expect(text('target')).toBe('90 units');
      expect(text('weight')).toBe('2.5');
      expect(text('all-time')).toBe('72');
      const progress = fixture.nativeElement.querySelector('[role="progressbar"]');
      expect(progress.getAttribute('aria-valuenow')).toBe(String(visual));
      expect(progress.firstElementChild.style.width).toBe(`${visual}%`);
    });
  }

  it('renders Arabic rating text and the unknown-rating fallback', () => {
    const { fixture, http, service } = setup();
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('ar', { EMPLOYEE_DETAIL: {
      NOT_AVAILABLE: 'غير متاح', PERFORMANCE: { GOOD_PERFORMANCE: 'أداء جيد', KPI_DETAILS: 'تفاصيل مؤشر الأداء', WEIGHT: 'الوزن' },
    } });
    translate.use('ar');
    http.expectOne(`${base}/1/performance/metrics/3?period=thisMonth`).flush({ ...result, periodRatingLabel: 'Good Performance' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-testid="metric-rating"]').textContent.trim()).toBe('أداء جيد');
    expect(fixture.nativeElement.querySelector('[data-testid="metric-description"]').textContent.trim()).toBe('غير متاح');
    service.performanceMetricDetail.set({ ...result, periodRatingLabel: 'Unknown' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-testid="metric-rating"]').textContent.trim()).toBe('غير متاح');
  });

  it('shows a translated empty trend state while retaining the KPI summary', () => {
    const { fixture, http } = setup();
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', { EMPLOYEE_DETAIL: { PERFORMANCE: { NO_MONTHLY_TRENDS: 'No monthly performance data' } } });
    translate.use('en');
    http.expectOne(`${base}/1/performance/metrics/3?period=thisMonth`).flush(result);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No monthly performance data');
    expect(fixture.nativeElement.querySelector('[data-testid="metric-name"]').textContent).toContain('Sales');
    expect(fixture.nativeElement.querySelector('[aria-labelledby="metric-trend-title"] svg')).toBeNull();
  });

  it('retains gaps, duplicate months, order, real scores and counts with clamped geometry', () => {
    const { fixture, http } = setup();
    const monthlyTrends = [
      { year: 2025, month: 1, yearMonthLabel: 'ignore', averageScore: 80, submissionCount: 2 },
      { year: 2025, month: 3, yearMonthLabel: 'ignore', averageScore: 120, submissionCount: 4 },
      { year: 2025, month: 3, yearMonthLabel: 'ignore', averageScore: -10, submissionCount: 1 },
    ];
    http.expectOne(`${base}/1/performance/metrics/3?period=thisMonth`).flush({ ...result, monthlyTrends });
    fixture.detectChanges();
    const points = fixture.componentInstance.trendPoints();
    expect(points.map(p => p.month)).toEqual([1, 3, 3]);
    expect(points.map(p => p.averageScore)).toEqual([80, 120, -10]);
    expect(points.map(p => p.y)).toEqual([60, 20, 220]);
    expect(points.map(p => p.x)).toEqual([50, 150, 250]);
    const items = fixture.nativeElement.querySelectorAll('[aria-labelledby="metric-trend-title"] li');
    expect(items.length).toBe(3);
    expect(items[1].textContent).toContain('120');
    expect(items[1].textContent).toContain('4');
    fixture.componentInstance.filters.set({ days: 30 });
    fixture.detectChanges();
    http.expectOne(`${base}/1/performance/metrics/3?days=30`).flush({ ...result, monthlyTrends });
    fixture.detectChanges();
    expect(fixture.componentInstance.trendPoints().length).toBe(3);
  });

  it('renders a single point and localizes from year/month without reversing time', () => {
    const { fixture, http, service } = setup();
    const point = { year: 2026, month: 9, yearMonthLabel: 'DO NOT DISPLAY', averageScore: 80, submissionCount: 3 };
    http.expectOne(`${base}/1/performance/metrics/3?period=thisMonth`).flush({ ...result, monthlyTrends: [point] });
    TestBed.inject(LanguageService).currentLanguage.set('ar');
    fixture.detectChanges();
    expect(fixture.componentInstance.trendPoints()[0].monthLabel).toBe(
      new Date(Date.UTC(2026, 8, 1)).toLocaleDateString('ar-EG', { year: 'numeric', month: 'short', timeZone: 'UTC' }),
    );
    expect(fixture.nativeElement.textContent).not.toContain('DO NOT DISPLAY');
    expect(fixture.nativeElement.querySelectorAll('[aria-labelledby="metric-trend-title"] circle').length).toBe(1);
    service.performanceMetricDetail.set({ ...result, monthlyTrends: [point, { ...point, month: 11 }] });
    fixture.detectChanges();
    expect(fixture.componentInstance.trendPoints().map(p => p.month)).toEqual([9, 11]);
    expect(fixture.nativeElement.querySelector('[aria-labelledby="metric-trend-title"] [dir="ltr"]')).not.toBeNull();
  });

  it('retains every point and accessible entry in a large dataset', () => {
    const { fixture, http } = setup();
    const monthlyTrends = Array.from({ length: 120 }, (_, i) => ({
      year: 2016 + Math.floor(i / 12), month: i % 12 + 1, yearMonthLabel: '', averageScore: i, submissionCount: i + 1,
    }));
    http.expectOne(`${base}/1/performance/metrics/3?period=thisMonth`).flush({ ...result, monthlyTrends });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('[aria-labelledby="metric-trend-title"] li').length).toBe(120);
    expect(fixture.nativeElement.querySelectorAll('[aria-labelledby="metric-trend-title"] circle').length).toBe(120);
    expect(fixture.componentInstance.showTrendLabel(119)).toBe(true);
  });

  it('renders only supported submission columns, raw scores, fallbacks and localized dates', () => {
    const { fixture, http } = setup();
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', { EMPLOYEE_DETAIL: { NOT_AVAILABLE: 'Not Available' } });
    translate.use('en');
    const submittedAt = '2026-09-08T23:30:00Z';
    http.expectOne(`${base}/1/performance/metrics/3?period=thisMonth`).flush({ ...result, submissions: [
      { id: 2, score: 120, submittedAt, notes: ' ', evaluatorName: null },
      { id: 1, score: -5, submittedAt, notes: 'Real feedback', evaluatorName: 'Mona' },
    ] });
    fixture.detectChanges();
    const table = fixture.nativeElement.querySelector('table');
    expect(Array.from(table.querySelectorAll('th'), (th: any) => th.textContent.trim())).toEqual([
      'EMPLOYEE_DETAIL.PERFORMANCE.SCORE', 'EMPLOYEE_DETAIL.PERFORMANCE.DATE_OF_SUBMISSION',
      'EMPLOYEE_DETAIL.PERFORMANCE.FEEDBACK', 'EMPLOYEE_DETAIL.PERFORMANCE.EVALUATOR',
    ]);
    const rows = table.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
    expect(rows[0].cells[0].textContent.trim()).toBe('120');
    expect(rows[1].cells[0].textContent.trim()).toBe('-5');
    expect(rows[0].cells[2].textContent.trim()).toBe('Not Available');
    expect(rows[0].cells[3].textContent.trim()).toBe('Not Available');
    expect(rows[1].cells[2].textContent.trim()).toBe('Real feedback');
    expect(rows[1].cells[3].textContent.trim()).toBe('Mona');
    expect(table.querySelector('time').textContent).toBe(new Date(submittedAt).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric', timeZone: 'UTC' }));
    TestBed.inject(LanguageService).currentLanguage.set('ar');
    fixture.detectChanges();
    expect(table.querySelector('time').textContent).toBe(new Date(submittedAt).toLocaleDateString('ar-EG', { year: 'numeric', month: 'short', day: 'numeric', timeZone: 'UTC' }));
  });

  it('replaces submission rows with the selected-period response and shows translated empty state', () => {
    const { fixture, http } = setup();
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', { EMPLOYEE_DETAIL: { PERFORMANCE: { NO_SUBMISSIONS: 'No submissions available' } } });
    translate.use('en');
    http.expectOne(`${base}/1/performance/metrics/3?period=thisMonth`).flush({ ...result, submissions: [
      { id: 1, score: 80, submittedAt: '2026-09-08T00:00:00Z', notes: 'Old feedback', evaluatorName: null },
    ] });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('tbody tr').length).toBe(1);
    fixture.componentInstance.filters.set({ days: 30 });
    fixture.detectChanges();
    http.expectOne(`${base}/1/performance/metrics/3?days=30`).flush(result);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('table')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('No submissions available');
    expect(fixture.nativeElement.textContent).not.toContain('Old feedback');
    expect(fixture.nativeElement.querySelector('#metric-kpi-title')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('#metric-trend-title')).not.toBeNull();
  });

  for (const raw of ['0', 'test', '-1', '1.5', '2147483648']) {
    it(`rejects metric ${raw} without a request or silent redirect`, () => {
      params.next(convertToParamMap({ metricId: raw }));
      const { fixture, http } = setup();
      expect(fixture.componentInstance.invalidMetricId()).toBe(true);
      expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('INVALID_METRIC');
      expect(fixture.nativeElement.querySelector('a')).not.toBeNull();
      http.expectNone(() => true);
    });
  }

  for (const id of [0, NaN, -1, 1.5]) {
    it(`does not request an invalid employee ${id}`, () => {
      const { http } = setup(id);
      http.expectNone(() => true);
    });
  }

  it('uses days only and disables calendar navigation for rolling periods', () => {
    const { fixture, http } = setup();
    http.expectOne(`${base}/1/performance/metrics/3?period=thisMonth`).flush(result);
    fixture.componentInstance.selectPeriod({ target: { value: '30days' } } as unknown as Event);
    fixture.detectChanges();
    http.expectOne(`${base}/1/performance/metrics/3?days=30`).flush(result);
    expect(fixture.componentInstance.canNavigate()).toBe(false);
    fixture.componentInstance.navigatePeriod(-1);
    fixture.detectChanges();
    http.expectNone(() => true);
  });

  it('navigates to the previous UTC calendar month with from/to only', () => {
    const { fixture, http } = setup();
    http.expectOne(`${base}/1/performance/metrics/3?period=thisMonth`).flush(result);
    const now = new Date();
    fixture.componentInstance.navigatePeriod(-1);
    fixture.detectChanges();
    const req = http.expectOne(r => r.url === `${base}/1/performance/metrics/3`);
    expect(req.request.params.keys().sort()).toEqual(['from', 'to']);
    expect(req.request.params.get('from')).toBe(new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth() - 1, 1)).toISOString());
    expect(req.request.params.get('to')).toBe(new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), 1) - 1).toISOString());
    req.flush(result);
  });

  it('shows generic 404 error and retries only the current metric', () => {
    const { fixture, http, service } = setup();
    http.expectOne(`${base}/1/performance/metrics/3?period=thisMonth`).flush('private backend text', { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('private backend text');
    expect(fixture.nativeElement.textContent).toContain('METRIC_LOAD_ERROR');
    fixture.nativeElement.querySelector('[role="alert"] button').click();
    http.expectOne(`${base}/1/performance/metrics/3?period=thisMonth`).flush(result);
    expect(service.performanceOverviewError()).toBeNull();
  });

  it('ignores stale metric responses and clears pending data for invalid routes', () => {
    const { fixture, http, service } = setup();
    const old = http.expectOne(`${base}/1/performance/metrics/3?period=thisMonth`);
    params.next(convertToParamMap({ metricId: '4' }));
    fixture.detectChanges();
    const latest = http.expectOne(`${base}/1/performance/metrics/4?period=thisMonth`);
    old.flush(result);
    expect(service.performanceMetricDetailLoading()).toBe(true);
    expect(service.performanceMetricDetail()).toBeNull();
    params.next(convertToParamMap({ metricId: 'test' }));
    fixture.detectChanges();
    latest.flush({ ...result, metricId: 4 });
    expect(service.performanceMetricDetail()).toBeNull();
    expect(fixture.componentInstance.invalidMetricId()).toBe(true);
  });
});
