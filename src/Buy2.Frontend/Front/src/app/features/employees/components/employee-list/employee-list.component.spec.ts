import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import { of, Subject } from 'rxjs';
import type { EmployeeApiResponse } from '../../services/employee.service';
import { EmployeeService } from '../../services/employee.service';
import { EmployeeListComponent } from './employee-list.component';

describe('EmployeeListComponent', () => {
  let component: EmployeeListComponent;
  let fixture: ComponentFixture<EmployeeListComponent>;
  let deleteResponse: Subject<void>;
  const employeeService = {
    getEmployees: vi.fn(() => of([sampleEmployee()])),
    getJobRoles: vi.fn(() => of([{ id: 2, title: 'Software Developer', departmentId: 1, requiredQualificationsJson: '[]', createdAt: '2026-01-01T08:00:00Z' }])),
    getRoles: vi.fn(() => of([{ id: 1, name: 'SuperAdmin', permissionsJson: '{}', createdAt: '2026-01-01T08:00:00Z' }])),
    getSites: vi.fn(() => of([])),
    createEmployee: vi.fn(),
    updateEmployee: vi.fn(),
    deleteEmployee: vi.fn(),
  };

  beforeEach(() => {
    deleteResponse = new Subject<void>();
    employeeService.deleteEmployee.mockReturnValue(deleteResponse.asObservable());
    employeeService.getEmployees.mockClear();
    employeeService.getJobRoles.mockClear();
    employeeService.getRoles.mockClear();
    employeeService.getSites.mockClear();
    employeeService.createEmployee.mockClear();
    employeeService.updateEmployee.mockClear();
    employeeService.deleteEmployee.mockClear();

    TestBed.configureTestingModule({
      imports: [EmployeeListComponent],
      providers: [provideTranslateService(), { provide: EmployeeService, useValue: employeeService }],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', {
      EMPLOYEE_MANAGEMENT: {
        TITLE: 'Employee Management', ADD_EMPLOYEE: 'Add Employee', EDIT_EMPLOYEE: 'Edit Employee', DELETE_EMPLOYEE: 'Delete Employee', UPDATE: 'Update', CANCEL: 'Cancel', DELETE: 'Delete', DELETE_CONFIRMATION_TITLE: 'Delete Employee?', DELETE_CONFIRMATION_MESSAGE: 'Delete {{name}}?', EMPLOYEE_ID: 'Employee ID', EMPLOYEE_NAME: 'Employee Name', JOIN_DATE: 'Join Date', JOB_TITLE: 'Job Title', EMAIL: 'Email', ADMIN_ACCESS: 'Admin Access', ACTIONS: 'Actions', NO_EMPLOYEES: 'No employees available', DISCARD: 'Discard', SAVE: 'Save', SUCCESS_TITLE: 'Hold on tight!', SUCCESS_ACTION: 'Got it', CREATE_SUCCESS_MESSAGE: 'Created', UPDATE_SUCCESS_MESSAGE: 'Updated', DELETE_SUCCESS_MESSAGE: 'Deleted', DELETE_FAILED: 'Delete failed',
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
    expect(table.textContent).toContain('0001');
    expect(table.textContent).toContain('Ahmed Ali');
    expect(table.textContent).toContain('Software Developer');
  });

  it('clicking Edit opens the modal for the selected employee', () => {
    const editButton = fixture.nativeElement.querySelector(
      'button[aria-label="Edit Employee"]',
    ) as HTMLButtonElement;
    editButton.click();
    fixture.detectChanges();

    expect(component.showEditModal()).toBe(true);
    expect(component.editingEmployee()).toEqual(sampleEmployee());
    expect(fixture.nativeElement.textContent).toContain('Edit Employee');
  });

  it('clicking Delete opens the confirmation and Cancel does not delete', () => {
    const deleteButton = fixture.nativeElement.querySelector(
      'button[aria-label="Delete Employee"]',
    ) as HTMLButtonElement;
    deleteButton.click();
    component.closeDeleteModal();

    expect(component.showDeleteModal()).toBe(false);
    expect(employeeService.deleteEmployee).not.toHaveBeenCalled();
  });

  it('prevents duplicate delete requests while deletion is pending', () => {
    component.openDeleteModal(component.employees()[0]);
    component.confirmDelete();
    component.confirmDelete();

    expect(component.isDeleting()).toBe(true);
    expect(employeeService.deleteEmployee).toHaveBeenCalledWith(1);
    expect(employeeService.deleteEmployee).toHaveBeenCalledTimes(1);
  });

  it('shows the delete success confirmation and refreshes after it is confirmed', () => {
    component.openDeleteModal(component.employees()[0]);
    component.confirmDelete();
    deleteResponse.next();
    deleteResponse.complete();

    expect(component.showSuccessModal()).toBe(true);
    expect(component.successMessage()).toBe('DELETE_SUCCESS_MESSAGE');

    component.confirmSuccess();

    expect(component.showDeleteModal()).toBe(false);
    expect(employeeService.getEmployees).toHaveBeenCalledTimes(2);
  });

  it('keeps the employee available and exposes an error when delete fails', () => {
    component.openDeleteModal(component.employees()[0]);
    component.confirmDelete();
    deleteResponse.error(new Error('Network error'));

    expect(component.employees()).toHaveLength(1);
    expect(component.showDeleteModal()).toBe(true);
    expect(component.deleteError()).toBe('EMPLOYEE_MANAGEMENT.DELETE_FAILED');
  });
});

function sampleEmployee(): EmployeeApiResponse {
  return {
    id: 1,
    firstName: 'Ahmed',
    lastName: 'Ali',
    email: 'a.ali@buy2.com',
    phoneNumber: '+966598432423',
    jobRoleId: 2,
    roleId: 1,
    siteId: 1,
    createdAt: '2026-01-15T08:00:00Z',
  };
}
