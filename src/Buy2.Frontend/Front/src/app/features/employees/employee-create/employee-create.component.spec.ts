import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { of, Subject } from 'rxjs';
import type { EmployeeApiResponse } from '../services/employee.service';
import { EmployeeService } from '../services/employee.service';
import { EmployeeCreateComponent } from './employee-create.component';

describe('EmployeeCreateComponent', () => {
  let component: EmployeeCreateComponent;
  let fixture: ComponentFixture<EmployeeCreateComponent>;
  let createResponse: Subject<EmployeeApiResponse>;
  const employeeService = {
    getJobRoles: vi.fn(() => of([])),
    getRoles: vi.fn(() => of([])),
    getSites: vi.fn(() => of([])),
    createEmployee: vi.fn(),
    updateEmployee: vi.fn(),
  };

  beforeEach(async () => {
    createResponse = new Subject<EmployeeApiResponse>();
    employeeService.createEmployee.mockReturnValue(createResponse.asObservable());
    employeeService.updateEmployee.mockReturnValue(createResponse.asObservable());
    employeeService.createEmployee.mockClear();
    employeeService.updateEmployee.mockClear();

    await TestBed.configureTestingModule({
      imports: [EmployeeCreateComponent],
      providers: [provideTranslateService(), { provide: EmployeeService, useValue: employeeService }],
    }).compileComponents();

    fixture = TestBed.createComponent(EmployeeCreateComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('does not call createEmployee when the form is invalid', () => {
    component.submit();

    expect(employeeService.createEmployee).not.toHaveBeenCalled();
    expect(component.employeeForm.controls.firstName.touched).toBe(true);
  });

  it('calls createEmployee with a trimmed payload for a valid form', () => {
    setValidForm();
    component.submit();

    expect(employeeService.createEmployee).toHaveBeenCalledWith(expect.objectContaining({
      firstName: 'Mona', lastName: 'Hassan', email: 'mona.hassan@buy2.com', phoneNumber: '+966599876543', jobRoleId: 1, roleId: 1, siteId: 1,
    }));
  });

  it('prevents duplicate submission and exposes loading state while the request is pending', () => {
    setValidForm();
    component.submit();
    component.submit();

    expect(component.isSubmitting()).toBe(true);
    expect(employeeService.createEmployee).toHaveBeenCalledTimes(1);
  });

  it('emits saved after a successful response', () => {
    const saved = vi.fn();
    component.saved.subscribe(saved);
    setValidForm();
    component.submit();

    createResponse.next({ id: 9, ...component.preparedPayload()! });
    createResponse.complete();

    expect(component.isSubmitting()).toBe(false);
    expect(saved).toHaveBeenCalledTimes(1);
  });

  it('retains entered values and exposes a translated error key when creation fails', () => {
    setValidForm();
    component.submit();
    createResponse.error(new Error('Network error'));

    expect(component.isSubmitting()).toBe(false);
    expect(component.submitError()).toBe('EMPLOYEE_MANAGEMENT.CREATE_FAILED');
    expect(component.employeeForm.controls.firstName.value).toBe(' Mona ');
    expect(component.employeeForm.controls.roleId.value).toBe(1);
  });

  it('pre-populates the form and PATCHes the selected employee', () => {
    fixture.componentRef.setInput('employee', sampleEmployee());
    fixture.detectChanges();

    expect(component.employeeForm.controls.firstName.value).toBe('Ahmed');
    expect(component.employeeForm.controls.jobRoleId.value).toBe(2);

    component.submit();

    expect(employeeService.updateEmployee).toHaveBeenCalledWith(
      1,
      expect.objectContaining({ firstName: 'Ahmed', jobRoleId: 2, createdAt: '2026-01-15T08:00:00Z' }),
    );
    expect(employeeService.createEmployee).not.toHaveBeenCalled();
  });

  it('does not PATCH an invalid edit form', () => {
    fixture.componentRef.setInput('employee', sampleEmployee());
    fixture.detectChanges();
    component.employeeForm.controls.email.setValue('not-an-email');

    component.submit();

    expect(employeeService.updateEmployee).not.toHaveBeenCalled();
    expect(component.employeeForm.controls.email.touched).toBe(true);
  });

  it('prevents duplicate PATCH requests while an update is pending', () => {
    fixture.componentRef.setInput('employee', sampleEmployee());
    fixture.detectChanges();

    component.submit();
    component.submit();

    expect(component.isSubmitting()).toBe(true);
    expect(employeeService.updateEmployee).toHaveBeenCalledTimes(1);
  });

  it('keeps the edit form values and exposes the update error when PATCH fails', () => {
    fixture.componentRef.setInput('employee', sampleEmployee());
    fixture.detectChanges();
    component.submit();
    createResponse.error(new Error('Network error'));

    expect(component.employeeForm.controls.firstName.value).toBe('Ahmed');
    expect(component.submitError()).toBe('EMPLOYEE_MANAGEMENT.UPDATE_FAILED');
  });

  function setValidForm(): void {
    component.employeeForm.setValue({
      firstName: ' Mona ', lastName: ' Hassan ', email: ' mona.hassan@buy2.com ', phoneNumber: '+966599876543', jobRoleId: 1, roleId: 1, siteId: 1,
    });
  }

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
});
