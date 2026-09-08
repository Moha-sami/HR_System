import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import type { EmployeePerformanceTask } from '../models/view-employee/employee-performance-task';
import { EmployeeDetailService } from './employee-detail.service';

const base = `${environment.baseUrl}/employees`;
const task: EmployeePerformanceTask = {
  id: 3, employeeId: 1, title: 'Review report', description: null, status: 'InReview',
  priority: null, dueDate: null, completedAt: null, createdAt: '2026-09-08T00:00:00Z',
};

describe('EmployeeDetailService Performance tasks', () => {
  let service: EmployeeDetailService;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(EmployeeDetailService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('requests all tasks with the exact GET URL and no query parameters', () => {
    service.getEmployeePerformanceTasks(1).subscribe(data => expect(data).toEqual([task]));
    const req = http.expectOne(`${base}/1/tasks`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys()).toEqual([]);
    req.flush([task]);
  });

  it('keeps successful empty data distinct from unloaded/error state', () => {
    expect(service.performanceTasks()).toBeNull();
    service.loadEmployeePerformanceTasks(1);
    expect(service.performanceTasksLoading()).toBe(true);
    http.expectOne(`${base}/1/tasks`).flush([]);
    expect(service.performanceTasks()).toEqual([]);
    expect(service.performanceTasksLoading()).toBe(false);
    expect(service.performanceTasksError()).toBeNull();
  });

  for (const status of [400, 401, 404, 0]) {
    it(`preserves HTTP ${status} as an error and resets it on retry`, () => {
      service.loadEmployeePerformanceTasks(1);
      const req = http.expectOne(`${base}/1/tasks`);
      if (status === 0) req.error(new ProgressEvent('error'));
      else req.flush({ reason: 'failed' }, { status, statusText: 'Error' });
      expect(service.performanceTasksError()?.status).toBe(status);
      expect(service.performanceTasks()).toBeNull();
      expect(service.performanceTasksLoading()).toBe(false);
      service.loadEmployeePerformanceTasks(1);
      expect(service.performanceTasksError()).toBeNull();
      http.expectOne(`${base}/1/tasks`).flush([]);
    });
  }

  it('ignores stale employee success while a new request is pending', () => {
    service.loadEmployeePerformanceTasks(1);
    const old = http.expectOne(`${base}/1/tasks`);
    service.loadEmployeePerformanceTasks(2);
    const latest = http.expectOne(`${base}/2/tasks`);
    old.flush([task]);
    expect(service.performanceTasks()).toBeNull();
    expect(service.performanceTasksLoading()).toBe(true);
    latest.flush([{ ...task, employeeId: 2 }]);
    expect(service.performanceTasks()?.[0].employeeId).toBe(2);
  });

  it('ignores stale employee errors without stopping newer loading', () => {
    service.loadEmployeePerformanceTasks(1);
    const old = http.expectOne(`${base}/1/tasks`);
    service.loadEmployeePerformanceTasks(2);
    const latest = http.expectOne(`${base}/2/tasks`);
    old.flush('missing', { status: 404, statusText: 'Not Found' });
    expect(service.performanceTasksError()).toBeNull();
    expect(service.performanceTasksLoading()).toBe(true);
    latest.flush([]);
  });

  it('does not overwrite a newer result with a late same-employee response', () => {
    service.loadEmployeePerformanceTasks(1);
    const old = http.expectOne(`${base}/1/tasks`);
    service.loadEmployeePerformanceTasks(1);
    http.expectOne(`${base}/1/tasks`).flush([]);
    old.flush([task]);
    expect(service.performanceTasks()).toEqual([]);
  });

  for (const action of ['clear tasks', 'clear employee', 'switch employee']) {
    it(`invalidates loaded and pending tasks on ${action}`, () => {
      service.loadEmployeePerformanceTasks(1);
      http.expectOne(`${base}/1/tasks`).flush([task]);
      expect(service.performanceTasks()).toEqual([task]);
      service.loadEmployeePerformanceTasks(1);
      const old = http.expectOne(`${base}/1/tasks`);
      if (action === 'clear tasks') service.clearEmployeePerformanceTasks();
      else if (action === 'clear employee') service.clearDetailEmployee();
      else {
        service.loadDetailEmployee(2);
        http.expectOne(`${base}/2`).flush('missing', { status: 404, statusText: 'Not Found' });
      }
      old.flush([task]);
      expect(service.performanceTasks()).toBeNull();
      expect(service.performanceTasksLoading()).toBe(false);
      expect(service.performanceTasksError()).toBeNull();
    });
  }

  it('keeps task requests independent from overview filters and metric detail', () => {
    service.loadEmployeePerformanceTasks(1);
    const tasks = http.expectOne(`${base}/1/tasks`);
    service.loadPerformanceOverview(1, { period: 'thisMonth' });
    const overview = http.expectOne(`${base}/1/performance/overview?period=thisMonth`);
    service.loadPerformanceOverview(1, { days: 30 });
    const newerOverview = http.expectOne(`${base}/1/performance/overview?days=30`);
    service.loadPerformanceMetricDetail(1, 2);
    const metric = http.expectOne(`${base}/1/performance/metrics/2`);
    expect(tasks.request.params.keys()).toEqual([]);
    expect(service.performanceTasksLoading()).toBe(true);
    tasks.flush([task]);
    expect(service.performanceOverviewLoading()).toBe(true);
    expect(service.performanceMetricDetailLoading()).toBe(true);
    service.clearPerformanceOverview();
    service.clearPerformanceMetricDetail();
    overview.flush(null);
    newerOverview.flush(null);
    metric.flush(null);
    expect(service.performanceTasks()).toEqual([task]);
    http.expectNone(`${base}/1/tasks`);
  });
});
