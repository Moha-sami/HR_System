import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { EmployeeDetailComponent } from './employee-detail.component';
import { EmployeeDetailService } from '../../services/employee-detail.service';
import { TranslatePipe } from '@ngx-translate/core';
import { signal } from '@angular/core';

describe('EmployeeDetailComponent', () => {
  let component: EmployeeDetailComponent;
  let fixture: ComponentFixture<EmployeeDetailComponent>;
  let mockEmployeeDetailService: {
    loadDetailEmployee: (id: number) => void;
    detailEmployee: ReturnType<typeof signal<any>>;
    detailLoading: ReturnType<typeof signal<boolean>>;
    detailError: ReturnType<typeof signal<string | null>>;
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
      qualifications: ['BS Computer Science', 'AWS Certified'],
    },
  };

  beforeEach(async () => {
    mockEmployeeDetailService = {
      loadDetailEmployee: () => {},
      detailEmployee: signal(mockEmployee),
      detailLoading: signal(false),
      detailError: signal(null),
    };

    await TestBed.configureTestingModule({
      imports: [EmployeeDetailComponent],
      providers: [
        { provide: EmployeeDetailService, useValue: mockEmployeeDetailService },
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
    expect(component.tabs[0].id).toBe('information');
    expect(component.tabs[1].id).toBe('payroll');
    expect(component.tabs[2].id).toBe('attendance');
    expect(component.tabs[3].id).toBe('documents');
    expect(component.tabs[4].id).toBe('violations');
  });

  it('should format gender correctly', () => {
    expect(component.formatGender(1)).toBe('Male');
    expect(component.formatGender(2)).toBe('Female');
    expect(component.formatGender(null)).toBe('—');
    expect(component.formatGender(0)).toBe('—');
  });
});
