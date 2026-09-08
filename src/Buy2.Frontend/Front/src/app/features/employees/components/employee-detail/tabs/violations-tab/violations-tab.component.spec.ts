import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { of, type Observable } from 'rxjs';
import { ViolationsTabComponent } from './violations-tab.component';
import { EmployeeDetailService } from '../../../../services/employee-detail.service';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Router } from '@angular/router';
import { Pipe, type PipeTransform, signal } from '@angular/core';

@Pipe({ name: 'translate', standalone: true })
class MockTranslatePipe implements PipeTransform {
  transform(value: string): string {
    return value;
  }
}

describe('ViolationsTabComponent', () => {
  let component: ViolationsTabComponent;
  let fixture: ComponentFixture<ViolationsTabComponent>;
  let mockEmployeeDetailService: {
    loadViolations: (id: number, filters: Record<string, unknown>) => Observable<void>;
    exportViolations: (id: number, filters: Record<string, unknown>) => Observable<Blob>;
    detailEmployee: ReturnType<typeof signal<any>>;
    violations: ReturnType<typeof signal<any>>;
    violationsLoading: ReturnType<typeof signal<any>>;
    violationsError: ReturnType<typeof signal<any>>;
  };

  const mockEmployee = {
    id: 1,
    employeeCode: 'EMP-0001',
    fullName: 'John Doe',
  };

  const mockViolations = [
    {
      id: 1,
      employeeId: 1,
      type: 'Attendance',
      severity: 'High',
      description: 'Late arrival',
      status: 'Pending',
      reportedByName: 'Jane Smith',
      createdAt: '2024-01-15T10:30:00Z',
      actionType: null,
      actionDate: null,
      documentUrl: null,
    },
    {
      id: 2,
      employeeId: 1,
      type: 'Behavioral',
      severity: 'Medium',
      description: 'Inappropriate conduct',
      status: 'Resolved',
      reportedByName: 'John Manager',
      createdAt: '2024-01-10T14:00:00Z',
      actionType: 'Warning',
      actionDate: '2024-01-12T09:00:00Z',
      documentUrl: null,
    },
  ];

  beforeEach(async () => {
    mockEmployeeDetailService = {
      loadViolations: () => of(void 0) as Observable<void>,
      exportViolations: () => of(new Blob()) as Observable<Blob>,
      detailEmployee: signal(mockEmployee),
      violations: signal(mockViolations),
      violationsLoading: signal(false),
      violationsError: signal(null),
    };

    await TestBed.configureTestingModule({
      imports: [ViolationsTabComponent],
      providers: [
        { provide: EmployeeDetailService, useValue: mockEmployeeDetailService },
        { provide: TranslateService, useValue: { instant: (key: string) => key, onLangChange: of({}) } },
        { provide: Router, useValue: { navigate: () => Promise.resolve(true) } },
      ],
    })
      .overrideComponent(ViolationsTabComponent, {
        remove: { imports: [TranslatePipe] },
        add: { imports: [MockTranslatePipe] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(ViolationsTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have default sort field as createdAt and direction desc', () => {
    expect(component.sortField()).toBe('createdAt');
    expect(component.sortDirection()).toBe('desc');
  });

  it('should toggle sort direction when clicking same field', () => {
    component.onSort('createdAt');
    expect(component.sortDirection()).toBe('asc');

    component.onSort('createdAt');
    expect(component.sortDirection()).toBe('desc');
  });

  it('should change sort field and reset direction to asc', () => {
    component.onSort('type');
    expect(component.sortField()).toBe('type');
    expect(component.sortDirection()).toBe('asc');
  });

  it('should format date correctly', () => {
    const formatted = component.formatDate('2024-01-15T10:30:00Z');
    expect(formatted).toContain('Jan');
    expect(formatted).toContain('2024');
  });

  it('should return correct severity class', () => {
    expect(component.getSeverityClass('Low')).toContain('bg-green-100');
    expect(component.getSeverityClass('Medium')).toContain('bg-yellow-100');
    expect(component.getSeverityClass('High')).toContain('bg-orange-100');
    expect(component.getSeverityClass('Critical')).toContain('bg-red-100');
    expect(component.getSeverityClass('Unknown')).toContain('bg-gray-100');
  });

  it('should return correct status class', () => {
    expect(component.getStatusClass('Pending')).toContain('bg-gray-100');
    expect(component.getStatusClass('Approved')).toContain('bg-blue-100');
    expect(component.getStatusClass('Rejected')).toContain('bg-red-100');
    expect(component.getStatusClass('Resolved')).toContain('bg-green-100');
    expect(component.getStatusClass('UnderInvestigation')).toContain('bg-purple-100');
    expect(component.getStatusClass('Unknown')).toContain('bg-gray-100');
  });

  it('should detect active filters', () => {
    component.filters.set({ type: 'Attendance' });
    expect(component.hasActiveFilters()).toBe(true);

    component.filters.set({ severityLevel: 'High' });
    expect(component.hasActiveFilters()).toBe(true);

    component.filters.set({ dateFrom: '2024-01-01' });
    expect(component.hasActiveFilters()).toBe(true);

    component.filters.set({ dateTo: '2024-12-31' });
    expect(component.hasActiveFilters()).toBe(true);

    component.filters.set({});
    expect(component.hasActiveFilters()).toBe(false);
  });

  it('should sort violations by createdAt desc by default', () => {
    const sorted = component.sortedViolations();
    expect(sorted[0].id).toBe(1); // Later date first
  });

  it('should sort violations by type', () => {
    component.sortField.set('type');
    component.sortDirection.set('asc');
    const sorted = component.sortedViolations();
    expect(sorted[0].type).toBe('Attendance'); // A comes before B
  });
});
