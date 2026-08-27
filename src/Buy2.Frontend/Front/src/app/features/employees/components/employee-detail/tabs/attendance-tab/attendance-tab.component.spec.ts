import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { of, type Observable } from 'rxjs';
import { AttendanceTabComponent } from './attendance-tab.component';
import { EmployeeDetailService } from '../../../../services/employee-detail.service';
import { TranslatePipe } from '@ngx-translate/core';
import { signal } from '@angular/core';

describe('AttendanceTabComponent', () => {
  let component: AttendanceTabComponent;
  let fixture: ComponentFixture<AttendanceTabComponent>;
  let mockEmployeeDetailService: {
    loadAttendanceCalendar: (id: number, month: number, year: number) => Observable<void>;
    detailEmployee: ReturnType<typeof signal<any>>;
    attendanceCalendar: ReturnType<typeof signal<any>>;
    attendanceLoading: ReturnType<typeof signal<any>>;
    attendanceError: ReturnType<typeof signal<any>>;
  };

  beforeEach(async () => {
    mockEmployeeDetailService = {
      loadAttendanceCalendar: () => of(void 0) as Observable<void>,
      detailEmployee: signal(null),
      attendanceCalendar: signal(null),
      attendanceLoading: signal(false),
      attendanceError: signal(null),
    };

    await TestBed.configureTestingModule({
      imports: [AttendanceTabComponent],
      providers: [{ provide: EmployeeDetailService, useValue: mockEmployeeDetailService }],
    })
      .overrideComponent(AttendanceTabComponent, {
        remove: { imports: [TranslatePipe] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(AttendanceTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have dayHeaders defined', () => {
    expect(component.dayHeaders).toEqual(['SUN', 'MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT']);
  });

  it('should have AttendanceDayStatus exposed to template', () => {
    expect(component.AttendanceDayStatus).toBeDefined();
  });

  it('should have currentMonth and currentYear initialized', () => {
    const now = new Date();
    expect(component.currentMonth()).toBe(now.getMonth() + 1);
    expect(component.currentYear()).toBe(now.getFullYear());
  });
});
