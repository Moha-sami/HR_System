import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { EmployeeDetailComponent } from './employee-detail.component';
import { EmployeeService } from '../../services/employee.service';
import { TranslatePipe } from '@ngx-translate/core';

describe('EmployeeDetailComponent', () => {
  let component: EmployeeDetailComponent;
  let fixture: ComponentFixture<EmployeeDetailComponent>;
  let mockEmployeeService: { getEmployeeProfile: (id: number) => ReturnType<typeof of> };

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
      qualifications: ['BS Computer Science', 'AWS Certified'],
    },
  };

  beforeEach(async () => {
    mockEmployeeService = { getEmployeeProfile: () => of(mockEmployee) };

    await TestBed.configureTestingModule({
      imports: [EmployeeDetailComponent],
      providers: [
        { provide: EmployeeService, useValue: mockEmployeeService },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: { get: () => '1' } },
            paramMap: of({ get: () => '1' }),
            firstChild: { url: of([]) },
          },
        },
      ],
    })
      .overrideComponent(EmployeeDetailComponent, {
        remove: { imports: [TranslatePipe] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(EmployeeDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have correct tabs configuration', () => {
    expect(component.tabs.length).toBe(5);
    expect(component.tabs[0].id).toBe('personal');
    expect(component.tabs[1].id).toBe('job');
    expect(component.tabs[2].id).toBe('payroll');
    expect(component.tabs[3].id).toBe('attendance');
    expect(component.tabs[4].id).toBe('documents');
  });

  it('should format gender correctly', () => {
    expect(component.formatGender(1)).toBe('Male');
    expect(component.formatGender(2)).toBe('Female');
    expect(component.formatGender(null)).toBe('—');
    expect(component.formatGender(0)).toBe('—');
  });
});
