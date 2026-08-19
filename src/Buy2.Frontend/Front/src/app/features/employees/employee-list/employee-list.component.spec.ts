import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { EmployeeService } from '../services/employee.service';
import { EmployeeListComponent } from './employee-list.component';

describe('EmployeeListComponent', () => {
  let component: EmployeeListComponent;
  let fixture: ComponentFixture<EmployeeListComponent>;
  const employeeService = {
    getEmployees: vi.fn(() => of([{ id: 1, firstName: 'Ahmed', lastName: 'Ali', email: 'a.ali@buy2.com', phoneNumber: '+966598432423', jobRoleId: 2, roleId: 1, siteId: 1, createdAt: '2026-01-15T08:00:00Z' }])),
    getJobRoles: vi.fn(() => of([{ id: 2, title: 'Software Developer', departmentId: 1, requiredQualificationsJson: '[]', createdAt: '2026-01-01T08:00:00Z' }])),
    getRoles: vi.fn(() => of([{ id: 1, name: 'SuperAdmin', permissionsJson: '{}', createdAt: '2026-01-01T08:00:00Z' }])),
    getSites: vi.fn(() => of([])),
    createEmployee: vi.fn(),
  };

  beforeEach(() => {
    employeeService.getEmployees.mockClear();
    employeeService.getJobRoles.mockClear();
    employeeService.getRoles.mockClear();

    TestBed.configureTestingModule({
      imports: [EmployeeListComponent],
      providers: [provideTranslateService(), { provide: EmployeeService, useValue: employeeService }],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', {
      EMPLOYEE_MANAGEMENT: {
        TITLE: 'Employee Management', ADD_EMPLOYEE: 'Add Employee', EMPLOYEE_ID: 'Employee ID', EMPLOYEE_NAME: 'Employee Name', JOIN_DATE: 'Join Date', JOB_TITLE: 'Job Title', EMAIL: 'Email', ADMIN_ACCESS: 'Admin Access', ACTIONS: 'Actions', NO_EMPLOYEES: 'No employees available', DISCARD: 'Discard', SAVE: 'Save', SUCCESS_TITLE: 'Hold on tight!', SUCCESS_MESSAGE: 'The employee has been added successfully.', SUCCESS_ACTION: 'Got it',
      },
    });
    translate.use('en').subscribe();

    fixture = TestBed.createComponent(EmployeeListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads employee data through EmployeeService and renders it in the table', () => {
    const table = fixture.nativeElement.querySelector('app-table') as HTMLElement;

    expect(employeeService.getEmployees).toHaveBeenCalledTimes(1);
    expect(employeeService.getJobRoles).toHaveBeenCalledTimes(1);
    expect(employeeService.getRoles).toHaveBeenCalledTimes(1);
    expect(table.textContent).toContain('0001');
    expect(table.textContent).toContain('Ahmed Ali');
    expect(table.textContent).toContain('Software Developer');
  });

  it('renders the Add Employee button and opens the form modal when clicked', () => {
    const addButton = fixture.nativeElement.querySelector('app-button button') as HTMLButtonElement;

    expect(addButton.textContent).toContain('Add Employee');
    addButton.click();
    fixture.detectChanges();

    expect(component.showCreateModal()).toBe(true);
    expect(fixture.nativeElement.querySelector('app-modal app-employee-create')).toBeTruthy();
  });

  it('closes the form modal and reloads the list after success is confirmed', () => {
    component.openCreateModal();
    component.onEmployeeCreated();
    component.confirmSuccess();

    expect(component.showSuccessModal()).toBe(false);
    expect(component.showCreateModal()).toBe(false);
    expect(employeeService.getEmployees).toHaveBeenCalledTimes(2);
  });
});
