import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { of, type Observable } from 'rxjs';
import { InformationTabComponent } from './information-tab.component';
import { EmployeeDetailService } from '../../../../services/employee-detail.service';
import { TranslatePipe } from '@ngx-translate/core';
import { signal } from '@angular/core';

describe('InformationTabComponent', () => {
  let component: InformationTabComponent;
  let fixture: ComponentFixture<InformationTabComponent>;
  let mockEmployeeDetailService: {
    updatePersonalInfo: (id: number, dto: Record<string, unknown>) => Observable<void>;
    updateJobDetails: (id: number, dto: Record<string, unknown>) => Observable<void>;
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
    personalInfo: { birthdate: '1990-01-15', gender: 1 },
    jobDetails: {
      title: 'Software Engineer',
      department: 'Engineering',
      seniorityLevel: 'Senior',
      experienceYears: 5,
      directManagerName: 'Jane Smith',
      jobType: 'Full-time',
      attendanceType: 'On-site',
      qualifications: ['BS Computer Science', 'AWS Certified'],
    },
  };

  beforeEach(async () => {
    mockEmployeeDetailService = {
      updatePersonalInfo: () => of(void 0) as Observable<void>,
      updateJobDetails: () => of(void 0) as Observable<void>,
      detailEmployee: signal(mockEmployee),
    };

    await TestBed.configureTestingModule({
      imports: [InformationTabComponent],
      providers: [{ provide: EmployeeDetailService, useValue: mockEmployeeDetailService }],
    })
      .overrideComponent(InformationTabComponent, {
        remove: { imports: [TranslatePipe] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(InformationTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should start with both sections in view mode', () => {
    expect(component.personalEditing()).toBe(false);
    expect(component.jobEditing()).toBe(false);
  });

  it('should toggle personal info edit mode', () => {
    component.togglePersonalEdit();
    expect(component.personalEditing()).toBe(true);
  });

  it('should toggle job details edit mode', () => {
    component.toggleJobEdit();
    expect(component.jobEditing()).toBe(true);
  });

  it('should format gender correctly', () => {
    expect(component.formatGender(1)).toBe('Male');
    expect(component.formatGender(2)).toBe('Female');
    expect(component.formatGender(null)).toBe('—');
    expect(component.formatGender(0)).toBe('—');
  });
});
