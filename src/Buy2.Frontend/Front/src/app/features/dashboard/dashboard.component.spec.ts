import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA, signal } from '@angular/core';
import { DashboardComponent } from './dashboard.component';
import { AdminDashboardComponent } from './admin-dashboard.component';
import { EmployeeDashboardComponent } from './employee-dashboard.component';
import { AuthService } from '../../core/auth/auth.service';

describe('DashboardComponent', () => {
  let fixture: ComponentFixture<DashboardComponent>;
  let currentUser: ReturnType<typeof signal<{ userId: number; email: string; role: string }>>;

  async function setup(): Promise<void> {
    currentUser = signal({ userId: 1, email: 'a@corp.io', role: 'Manager' });
    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [{ provide: AuthService, useValue: { currentUser } }],
    })
      .overrideComponent(DashboardComponent, {
        remove: { imports: [AdminDashboardComponent, EmployeeDashboardComponent] },
        add: { schemas: [NO_ERRORS_SCHEMA] },
      })
      .compileComponents();
    fixture = TestBed.createComponent(DashboardComponent);
    fixture.detectChanges();
  }

  it('renders the admin view for non-employee roles', async () => {
    await setup();

    expect(fixture.nativeElement.querySelector('app-admin-dashboard')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('app-employee-dashboard')).toBeNull();
  });

  it('renders the personal view for employees', async () => {
    await setup();

    currentUser.set({ userId: 7, email: 'e@corp.io', role: 'Employee' });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-employee-dashboard')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('app-admin-dashboard')).toBeNull();
  });
});
