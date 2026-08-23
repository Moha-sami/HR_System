import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { InformationTabComponent } from './information-tab.component';
import { EmployeeService } from '../../../../services/employee.service';
import { TranslatePipe } from '@ngx-translate/core';

describe('InformationTabComponent', () => {
  let component: InformationTabComponent;
  let fixture: ComponentFixture<InformationTabComponent>;
  let mockEmployeeService: { updatePersonalInfo: (id: number, dto: Record<string, unknown>) => ReturnType<typeof of>; updateJobDetails: (id: number, dto: Record<string, unknown>) => ReturnType<typeof of> };

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
    mockEmployeeService = { updatePersonalInfo: () => of(void 0), updateJobDetails: () => of(void 0) };

    await TestBed.configureTestingModule({
      imports: [InformationTabComponent],
      providers: [
        { provide: EmployeeService, useValue: mockEmployeeService },
      ],
    })
      .overrideComponent(InformationTabComponent, {
        remove: { imports: [TranslatePipe] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(InformationTabComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('employeeId', 1);
    fixture.componentRef.setInput('employee', mockEmployee);
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