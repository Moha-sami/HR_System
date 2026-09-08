import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import { EmployeeDetailComponent } from './employee-detail.component';
import { EMPLOYEES_ROUTES } from '../../employees.routes';
import { EmployeeDetailService } from '../../services/employee-detail.service';
import type { EmployeeProfileDto } from '../../models/view-employee/employee-profile';

@Component({ standalone: true, template: '' })
class EmptyChild {}

describe('Employee Detail navigation semantics', () => {
  beforeEach(() => {
    const employee: EmployeeProfileDto = {
      id: 1, employeeCode: 'E1', fullName: 'Employee', phone: '', email: '', location: '', profilePhotoUrl: null,
      stats: { totalPoints: 0, totalTasks: 0, totalGifts: 0 }, personalInfo: { birthdate: null, gender: null },
      jobDetails: { title: '', department: '', seniorityLevel: '', experienceYears: 0, directManagerName: null, jobType: '', qualifications: [] },
    };
    TestBed.configureTestingModule({ providers: [
      provideTranslateService(),
      provideRouter([{ path: 'employees', children: EMPLOYEES_ROUTES.map(route => route.path === ':id'
        ? { ...route, children: route.children?.map(child => child.component ? { ...child, component: EmptyChild } : child) }
        : route) }]),
      { provide: EmployeeDetailService, useValue: {
        detailEmployee: signal(employee), detailLoading: signal(false), detailError: signal(null), loadDetailEmployee: () => {},
      } },
    ] });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', { EMPLOYEE_DETAIL: { NAVIGATION_LABEL: 'Employee details', TABS: {
      INFORMATION: 'Information', PERFORMANCE: 'Performance', PAYROLL: 'Payroll', ATTENDANCE: 'Attendance',
      DOCUMENTS: 'Documents', VIOLATIONS: 'Violations', POINTS_REWARDS: 'Points & Rewards',
    } } });
    translate.use('en');
  });

  it('renders named navigation and native links without tab-widget semantics', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/employees/1/information', EmployeeDetailComponent);
    await TestBed.inject(Router).navigateByUrl('/employees/1/information');
    harness.detectChanges();
    const nav = harness.routeNativeElement!.querySelector('nav')!;
    expect(nav.getAttribute('aria-label')).toBe('Employee details');
    expect(nav.querySelector('[role="tablist"], [role="tab"], [aria-selected], [aria-controls]')).toBeNull();
    const links = Array.from(nav.querySelectorAll('a'));
    expect(links.map(link => link.getAttribute('href'))).toEqual([
      'information', 'performance', 'payroll', 'attendance', 'documents', 'violations', 'points-rewards',
    ].map(path => `/employees/1/${path}`));
    for (const link of links) {
      expect(link.textContent?.trim()).toBeTruthy();
      expect(link.tabIndex).toBe(0);
      expect(link.hasAttribute('tabindex')).toBe(false);
      link.focus();
      expect(document.activeElement).toBe(link);
    }
  });

  it('keeps the correct current link through overview, nested metric, and return navigation', async () => {
    const harness = await RouterTestingHarness.create();
    for (const [path, current] of [
      ['information', 'information'], ['performance', 'performance'],
      ['performance/metrics/3', 'performance'], ['information', 'information'],
    ]) {
      await harness.navigateByUrl(`/employees/1/${path}`, EmployeeDetailComponent);
      harness.detectChanges();
      const links = harness.routeNativeElement!.querySelectorAll('nav a[aria-current="page"]');
      expect(links.length).toBe(1);
      expect(links[0].getAttribute('href')).toBe(`/employees/1/${current}`);
      expect(links[0].classList.contains('text-primary-600')).toBe(true);
    }
  });
});
