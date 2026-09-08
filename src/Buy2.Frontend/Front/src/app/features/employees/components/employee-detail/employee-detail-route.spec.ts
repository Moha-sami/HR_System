import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { vi } from 'vitest';
import { EmployeeDetailComponent } from './employee-detail.component';
import { EmployeeDetailService } from '../../services/employee-detail.service';
import { environment } from '../../../../../environments/environment';

const base = `${environment.baseUrl}/employees`;

describe('Employee Detail route ID validation', () => {
  let params: BehaviorSubject<ReturnType<typeof convertToParamMap>>;
  let service: EmployeeDetailService;
  let http: HttpTestingController;

  beforeEach(() => {
    params = new BehaviorSubject(convertToParamMap({ id: '12' }));
    TestBed.configureTestingModule({
      imports: [EmployeeDetailComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: params } }],
    }).overrideComponent(EmployeeDetailComponent, {
      set: { template: '@if (employee(); as emp) { <span>{{ emp.fullName }}</span> }', imports: [] },
    });
    service = TestBed.inject(EmployeeDetailService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('loads valid employee 12 and accepts the positive Int32 maximum', () => {
    const fixture = TestBed.createComponent(EmployeeDetailComponent);
    http.expectOne(`${base}/12`).flush({ id: 12, fullName: 'Employee A' });
    expect(fixture.componentInstance.employeeId()).toBe(12);
    expect(service.detailEmployee()?.id).toBe(12);
    params.next(convertToParamMap({ id: '2147483647' }));
    http.expectOne(`${base}/2147483647`).flush({ id: 2147483647 });
    expect(fixture.componentInstance.employeeId()).toBe(2147483647);
  });

  for (const value of ['0', '-2', '1.5', 'abc', '2147483648', 'NaN', 'Infinity', '', ' ', '0xC', '1e1', null]) {
    it(`rejects ${String(value)} and clears previously loaded employee context without a load`, () => {
      const fixture = TestBed.createComponent(EmployeeDetailComponent);
      http.expectOne(`${base}/12`).flush({ id: 12, fullName: 'Employee A' });
      fixture.detectChanges();
      expect(fixture.nativeElement.textContent).toContain('Employee A');
      const load = vi.spyOn(service, 'loadDetailEmployee');
      const clear = vi.spyOn(service, 'clearDetailEmployee');
      params.next(convertToParamMap(value === null ? {} : { id: value }));
      expect(load).not.toHaveBeenCalled();
      expect(clear).toHaveBeenCalledOnce();
      expect(fixture.componentInstance.employeeId()).toBeNull();
      expect(service.detailEmployee()).toBeNull();
      fixture.detectChanges();
      expect(fixture.nativeElement.textContent).not.toContain('Employee A');
      http.expectNone(() => true);
    });
  }

  it('invalidates a pending profile response and permits a subsequent valid employee', () => {
    const fixture = TestBed.createComponent(EmployeeDetailComponent);
    const pending = http.expectOne(`${base}/12`);
    params.next(convertToParamMap({ id: 'test' }));
    pending.flush({ id: 12, fullName: 'Employee A' });
    expect(service.detailEmployee()).toBeNull();
    expect(service.detailLoading()).toBe(false);
    expect(service.detailError()).toBeNull();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('Employee A');
    params.next(convertToParamMap({ id: '13' }));
    http.expectOne(`${base}/13`).flush({ id: 13, fullName: 'Employee B' });
    expect(service.detailEmployee()?.id).toBe(13);
  });

  it('invalidates pending Performance stores through the central context clear', () => {
    TestBed.createComponent(EmployeeDetailComponent);
    http.expectOne(`${base}/12`).flush({ id: 12, fullName: 'Employee A' });
    service.loadPerformanceOverview(12);
    service.loadPerformanceMetricDetail(12, 3);
    service.loadEmployeePerformanceTasks(12);
    const overview = http.expectOne(`${base}/12/performance/overview`);
    const metric = http.expectOne(`${base}/12/performance/metrics/3`);
    const tasks = http.expectOne(`${base}/12/tasks`);
    params.next(convertToParamMap({ id: '0' }));
    overview.flush({ employeeId: 12 });
    metric.flush('old error', { status: 404, statusText: 'Not Found' });
    tasks.flush([{ id: 1, employeeId: 12 }]);
    expect(service.performanceOverview()).toBeNull();
    expect(service.performanceMetricDetail()).toBeNull();
    expect(service.performanceTasks()).toBeNull();
    expect(service.performanceOverviewLoading()).toBe(false);
    expect(service.performanceMetricDetailLoading()).toBe(false);
    expect(service.performanceTasksLoading()).toBe(false);
    expect(service.performanceMetricDetailError()).toBeNull();
    http.expectNone(() => true);
  });
});
