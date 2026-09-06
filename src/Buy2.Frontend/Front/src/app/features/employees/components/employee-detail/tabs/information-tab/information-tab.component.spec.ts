import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { of, type Observable } from 'rxjs';
import { InformationTabComponent } from './information-tab.component';
import { EmployeeDetailService } from '../../../../services/employee-detail.service';
import { TranslatePipe } from '@ngx-translate/core';
import { Pipe, type PipeTransform, signal } from '@angular/core';

@Pipe({ name: 'translate', standalone: true })
class MockTranslatePipe implements PipeTransform {
  transform(value: string): string {
    return value;
  }
}

describe('InformationTabComponent', () => {
  let component: InformationTabComponent;
  let fixture: ComponentFixture<InformationTabComponent>;
  let mockEmployeeDetailService: {
    updatePersonalInfo: (id: number, dto: Record<string, unknown>) => Observable<void>;
    updateJobDetails: (id: number, dto: Record<string, unknown>) => Observable<void>;
    updatePayrollProfile: (id: number, dto: Record<string, unknown>) => Observable<void>;
    getEmployeePayroll: (id: number) => Observable<Record<string, unknown>>;
    getJobsLookup: () => Observable<Record<string, unknown>>;
    getEmployeesLookup: () => Observable<Record<string, unknown>>;
    getSitesLookup: () => Observable<readonly unknown[]>;
    detailEmployee: ReturnType<typeof signal<any>>;
  };

  const mockEmployee = {
    id: 1,
    employeeCode: 'EMP-0001',
    fullName: 'John Doe',
    phone: '+1234567890',
    email: 'john@example.com',
    location: 'New York',
    profilePhotoUrl: null,
    stats: { totalPoints: 100, totalTasks: 10, totalGifts: 2 },
    personalInfo: {
      name: 'John Doe',
      birthdate: '1990-01-15',
      email: 'john@example.com',
      phoneNumber: '+1234567890',
      gender: 1,
    },
    jobDetails: {
      title: 'Software Engineer',
      department: 'Engineering',
      seniorityLevel: 'Senior',
      experienceYears: 5,
      directManagerName: 'Jane Smith',
      jobType: 'Full-time',
      attendanceType: 'On-site',
      onlineWorkdays: ['Sunday'],
      offlineWorkdays: ['Monday'],
      qualifications: ['BS Computer Science', 'AWS Certified'],
      jobRoleId: 3,
      directManagerId: 7,
    },
    payroll: {
      salaryType: 'Fixed',
      paymentAmount: 5000,
      payoutPeriod: 'Monthly',
      payoutDay: 1,
      workWeekStartDay: 'Sunday',
      workWeekEndDay: 'Thursday',
      overtimeEnabled: true,
      overtimeThresholdHours: 40,
      overtimeRateMultiplier: 1.5,
      assignedWorkSiteIds: [2],
    },
  };

  beforeEach(async () => {
    mockEmployeeDetailService = {
      updatePersonalInfo: () => of(void 0) as Observable<void>,
      updateJobDetails: () => of(void 0) as Observable<void>,
      updatePayrollProfile: () => of(void 0) as Observable<void>,
      getEmployeePayroll: () =>
        of({
          employeeId: 1,
          isConfigured: true,
          salaryType: 1,
          payoutPeriod: 'Monthly',
          payoutDay: 1,
          workWeekStartDay: 0,
          workWeekEndDay: 4,
          paymentAmount: 5000,
          overtimeThresholdHours: 40,
          overtimeHourlyRate: 1.5,
          attendanceType: 'Hybrid',
          workSiteIds: [2],
          onlineWorkdays: [],
          offlineWorkdays: [],
        }) as unknown as Observable<Record<string, unknown>>,
      getJobsLookup: () =>
        of({ items: [{ id: 3, title: 'Software Engineer' }], totalCount: 1 }),
      getEmployeesLookup: () => of({ items: [], totalCount: 0 }),
      getSitesLookup: () => of([{ id: 2, siteName: 'Cairo Branch' }]),
      detailEmployee: signal(mockEmployee),
    };

    await TestBed.configureTestingModule({
      imports: [InformationTabComponent],
      providers: [{ provide: EmployeeDetailService, useValue: mockEmployeeDetailService }],
    })
      .overrideComponent(InformationTabComponent, {
        remove: { imports: [TranslatePipe] },
        add: { imports: [MockTranslatePipe] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(InformationTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should start with all sections in view mode', () => {
    expect(component.personalEditing()).toBe(false);
    expect(component.jobEditing()).toBe(false);
    expect(component.payrollEditing()).toBe(false);
  });

  it('should load lookups on init', () => {
    expect(component.jobs().length).toBe(1);
    expect(component.sites().length).toBe(1);
  });

  it('should prefill personal form from the API profile', () => {
    expect(component.personalForm().firstName).toBe('John');
    expect(component.personalForm().lastName).toBe('Doe');
    expect(component.personalForm().email).toBe('john@example.com');
  });

  it('should toggle personal info edit mode', () => {
    component.togglePersonalEdit();
    expect(component.personalEditing()).toBe(true);
  });

  it('should toggle job details edit mode', () => {
    component.toggleJobEdit();
    expect(component.jobEditing()).toBe(true);
  });

  it('should toggle payroll edit mode and load full payroll', () => {
    component.togglePayrollEdit();
    expect(component.payrollEditing()).toBe(true);
    expect(component.payrollForm().paymentAmount).toBe(5000);
  });

  it('should add and remove qualifications', () => {
    component.jobForm.set({ ...component.jobForm(), qualifications: [] });
    component.newQualification.set('POS System');
    component.addQualification();
    expect(component.jobForm().qualifications).toEqual(['POS System']);
    component.removeQualification('POS System');
    expect(component.jobForm().qualifications).toEqual([]);
  });

  it('should canonicalize stored workday casing on prefill', () => {
    mockEmployeeDetailService.detailEmployee.set({
      ...mockEmployee,
      jobDetails: {
        ...mockEmployee.jobDetails,
        onlineWorkdays: ['sunday', 'MONDAY'],
        offlineWorkdays: ['tuesday'],
      },
    });
    component.loadFormData();
    expect(component.jobForm().onlineWorkdays).toEqual(['Sunday', 'Monday']);
    expect(component.jobForm().offlineWorkdays).toEqual(['Tuesday']);
    expect(component.isOnlineDay('Sunday')).toBe(true);
    expect(component.isOfflineDay('Tuesday')).toBe(true);
    expect(component.isOnlineDay('Friday')).toBe(false);
  });

  it('should toggle workdays without duplicating case variants', () => {
    component.jobForm.set({ ...component.jobForm(), onlineWorkdays: ['sunday'] });
    component.toggleJobOnlineDay('Sunday');
    expect(component.jobForm().onlineWorkdays).toEqual([]);
    component.toggleJobOnlineDay('Sunday');
    expect(component.jobForm().onlineWorkdays).toEqual(['Sunday']);
  });

  it('should format gender correctly', () => {
    expect(component.formatGender(1)).toBe('Male');
    expect(component.formatGender(2)).toBe('Female');
    expect(component.formatGender(null)).toBe('—');
    expect(component.formatGender(0)).toBe('—');
  });
});
