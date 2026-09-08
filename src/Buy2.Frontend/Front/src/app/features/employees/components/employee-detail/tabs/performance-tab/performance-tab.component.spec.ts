import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import { EmployeeDetailService } from '../../../../services/employee-detail.service';
import type { EmployeeProfileDto } from '../../../../models/view-employee/employee-profile';
import type { EmployeePerformanceTask, EmployeePerformanceTaskStatus } from '../../../../models/view-employee/employee-performance-task';
import { environment } from '../../../../../../../environments/environment';
import { PerformanceTabComponent } from './performance-tab.component';
import { LanguageService } from '../../../../../../core/services/language.service';
import type { EmployeePerformanceOverview } from '../../../../models/view-employee/employee-performance';

const base = `${environment.baseUrl}/employees`;
const task = (status: EmployeePerformanceTaskStatus, id = 1): EmployeePerformanceTask => ({
  id, employeeId: 1, title: `Task ${id}`, status, description: null,
  dueDate: null, completedAt: null, priority: null, createdAt: '2026-09-08T00:00:00Z',
});

describe('Performance task board', () => {
  beforeEach(() => TestBed.configureTestingModule({
    imports: [PerformanceTabComponent],
    providers: [provideHttpClient(), provideHttpClientTesting(), provideTranslateService()],
  }));
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  function setup() {
    const service = TestBed.inject(EmployeeDetailService);
    service.detailEmployee.set({ id: 1 } as EmployeeProfileDto);
    const fixture = TestBed.createComponent(PerformanceTabComponent);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne(`${base}/1/performance/overview?period=thisMonth`).flush(null);
    return { service, fixture, http, request: http.expectOne(`${base}/1/tasks`) };
  }

  for (const language of ['en', 'ar'] as const) {
    it(`preserves LTR chart chronology and accessible localized text in ${language}`, () => {
      const { fixture, request, service } = setup();
      request.flush([]);
      TestBed.inject(LanguageService).currentLanguage.set(language);
      const translate = TestBed.inject(TranslateService);
      const scoreLabel = language === 'ar' ? 'الدرجة' : 'Score';
      translate.setTranslation(language, { EMPLOYEE_DETAIL: { PERFORMANCE: { SCORE: scoreLabel } } });
      translate.use(language);
      const points = [
        { date: '2026-01-01T00:00:00Z', score: 80 },
        { date: '2026-02-01T00:00:00Z', score: 120 },
        { date: '2026-03-01T00:00:00Z', score: -10 },
      ];
      const overview: EmployeePerformanceOverview = {
        employeeId: 1, overallWeightedScore: 80, ratingLabel: 'Good Performance',
        dateRangeResolved: { from: '', to: '', period: 'thisMonth' },
        tasksSummary: { totalTasks: 0, todoCount: 0, inProgressCount: 0, completedCount: 0, overdueCount: 0, deadlineCompliancePercentage: 0 },
        achievements: [], submissionsDetail: [], chartTrendPoints: points,
      };
      service.performanceOverview.set(overview);
      fixture.nativeElement.setAttribute('dir', language === 'ar' ? 'rtl' : 'ltr');
      fixture.detectChanges();
      const list: HTMLUListElement = fixture.nativeElement.querySelector('[aria-labelledby="performance-history-title"] ul');
      expect(list.getAttribute('dir')).toBe('ltr');
      expect(fixture.nativeElement.getAttribute('dir')).toBe(language === 'ar' ? 'rtl' : 'ltr');
      const verifyPoints = () => {
        const items = list.querySelectorAll('li');
        expect(items.length).toBe(points.length);
        points.forEach((point, index) => {
          const accessible = items[index].querySelector('span.sr-only')!;
          const date = new Date(point.date).toLocaleDateString(language === 'ar' ? 'ar-EG' : 'en-US', {
            year: 'numeric', month: 'short', day: 'numeric', timeZone: 'UTC',
          });
          expect(accessible.textContent).toContain(date);
          expect(accessible.textContent).toContain(`${scoreLabel}: ${point.score}`);
          expect(accessible.closest('[aria-hidden="true"]')).toBeNull();
        });
      };
      verifyPoints();
      const bars = list.querySelectorAll<HTMLElement>('div[style]');
      expect(Array.from(bars, bar => bar.style.height)).toEqual(['80%', '100%', '0%']);
      // Grow the dataset enough to suppress some visible dates, retaining every text equivalent.
      points.push(...Array.from({ length: 9 }, (_, index) => ({
        date: new Date(Date.UTC(2026, index + 3, 1)).toISOString(), score: index,
      })));
      service.performanceOverview.set({ ...overview, chartTrendPoints: [...points] });
      fixture.detectChanges();
      expect(fixture.componentInstance.showChartDate(1, points.length)).toBe(false);
      expect(list.querySelectorAll('li')[1].querySelector('time')).toBeNull();
      verifyPoints();
    });
  }

  it('groups each explicit status once, retaining empty columns and omitting nullable content', () => {
    const { fixture, request } = setup();
    const statuses: EmployeePerformanceTaskStatus[] = ['Todo', 'InProgress', 'InReview', 'Completed', 'Overdue'];
    request.flush(statuses.map((status, index) => task(status, index + 1)));
    fixture.detectChanges();
    expect(fixture.componentInstance.taskColumns().map(column => column.tasks.length)).toEqual([1, 1, 1, 1, 1]);
    const board = fixture.nativeElement.querySelector('[aria-labelledby="employee-tasks-title"]');
    expect(board.querySelectorAll('article').length).toBe(5);
    expect(board.querySelectorAll('time').length).toBe(0);
    expect(board.textContent).not.toContain('null');
    expect(board.textContent).not.toContain('undefined');
    TestBed.inject(EmployeeDetailService).performanceTasks.set([task('InReview')]);
    fixture.detectChanges();
    expect(fixture.componentInstance.taskColumns().map(column => column.tasks.length)).toEqual([0, 0, 1, 0, 0]);
    expect(board.querySelectorAll('h4').length).toBe(5);
  });

  it('renders the translated successful empty state', () => {
    const { fixture, request } = setup();
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', { EMPLOYEE_DETAIL: { PERFORMANCE: { NO_EMPLOYEE_TASKS: 'No employee tasks' } } });
    translate.use('en');
    request.flush([]);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No employee tasks');
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
  });

  it('does not reload tasks for overview period changes or same-ID profile updates', () => {
    const { fixture, request, http, service } = setup();
    request.flush([]);
    fixture.componentInstance.filters.set({ days: 30 });
    fixture.detectChanges();
    http.expectOne(`${base}/1/performance/overview?days=30`).flush(null);
    service.detailEmployee.set({ id: 1, fullName: 'Updated name' } as EmployeeProfileDto);
    fixture.detectChanges();
    http.expectOne(`${base}/1/performance/overview?days=30`).flush(null);
    http.expectNone(`${base}/1/tasks`);
  });

  it('retries task errors independently', () => {
    const { fixture, request, http, service } = setup();
    request.flush('failed', { status: 500, statusText: 'Error' });
    fixture.detectChanges();
    expect(service.performanceOverviewError()).toBeNull();
    fixture.nativeElement.querySelector('[role="alert"] button').click();
    http.expectOne(`${base}/1/tasks`).flush([]);
    http.expectNone(req => req.url.includes('/performance/overview'));
  });

  it('localizes task labels and UTC due dates in Arabic', () => {
    const { fixture, request } = setup();
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('ar', { EMPLOYEE_DETAIL: { PERFORMANCE: { TODO: 'للتنفيذ', DUE_DATE: 'تاريخ الاستحقاق' } } });
    translate.use('ar');
    TestBed.inject(LanguageService).currentLanguage.set('ar');
    const dueDate = '2026-09-08T23:00:00Z';
    request.flush([{ ...task('Todo'), dueDate }]);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('للتنفيذ');
    expect(fixture.nativeElement.querySelector('time').textContent).toBe(
      new Date(dueDate).toLocaleDateString('ar-EG', { year: 'numeric', month: 'short', day: 'numeric', timeZone: 'UTC' }),
    );
  });

  it('loads new employee tasks and ignores pending old tasks', () => {
    const { fixture, request, http, service } = setup();
    service.detailEmployee.set({ id: 2 } as EmployeeProfileDto);
    fixture.detectChanges();
    http.expectOne(`${base}/2/performance/overview?period=thisMonth`).flush(null);
    const latest = http.expectOne(`${base}/2/tasks`);
    request.flush([task('Todo')]);
    expect(service.performanceTasks()).toBeNull();
    expect(service.performanceTasksLoading()).toBe(true);
    latest.flush([{ ...task('Completed'), employeeId: 2 }]);
    fixture.detectChanges();
    expect(fixture.componentInstance.taskColumns()[3].tasks[0].employeeId).toBe(2);
  });
});
