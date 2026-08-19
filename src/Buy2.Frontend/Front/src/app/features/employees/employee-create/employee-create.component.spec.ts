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
  };

  beforeEach(async () => {
    createResponse = new Subject<EmployeeApiResponse>();
    employeeService.createEmployee.mockReturnValue(createResponse.asObservable());
    employeeService.createEmployee.mockClear();

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

  it('emits created after a successful response', () => {
    const created = vi.fn();
    component.created.subscribe(created);
    setValidForm();
    component.submit();

    createResponse.next({ id: 9, ...component.preparedPayload()! });
    createResponse.complete();

    expect(component.isSubmitting()).toBe(false);
    expect(created).toHaveBeenCalledTimes(1);
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

  function setValidForm(): void {
    component.employeeForm.setValue({
      firstName: ' Mona ', lastName: ' Hassan ', email: ' mona.hassan@buy2.com ', phoneNumber: '+966599876543', jobRoleId: 1, roleId: 1, siteId: 1,
    });
  }
});
