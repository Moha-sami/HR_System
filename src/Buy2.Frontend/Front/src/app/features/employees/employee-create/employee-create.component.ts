import { Component, inject, output, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import {
  type CreateEmployeeRequest,
  EmployeeService,
  type JobRoleApiResponse,
  type RoleApiResponse,
  type SiteApiResponse,
} from '../services/employee.service';

function nonWhitespace(control: AbstractControl): Record<string, true> | null {
  return typeof control.value === 'string' && control.value.trim().length > 0
    ? null
    : { whitespace: true };
}

@Component({
  selector: 'app-employee-create',
  standalone: true,
  imports: [ReactiveFormsModule, TranslatePipe],
  templateUrl: './employee-create.component.html',
  styleUrl: './employee-create.component.css',
})
export class EmployeeCreateComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly employeeService = inject(EmployeeService);

  readonly created = output<void>();
  readonly jobRoles = signal<readonly JobRoleApiResponse[]>([]);
  readonly roles = signal<readonly RoleApiResponse[]>([]);
  readonly sites = signal<readonly SiteApiResponse[]>([]);
  readonly loadingOptions = signal(true);
  readonly optionsLoadFailed = signal(false);
  readonly preparedPayload = signal<CreateEmployeeRequest | null>(null);
  readonly isSubmitting = signal(false);
  readonly submitError = signal<string | null>(null);

  readonly employeeForm = this.formBuilder.group({
    firstName: this.formBuilder.nonNullable.control('', [
      Validators.required,
      Validators.maxLength(100),
      nonWhitespace,
    ]),
    lastName: this.formBuilder.nonNullable.control('', [
      Validators.required,
      Validators.maxLength(100),
      nonWhitespace,
    ]),
    email: this.formBuilder.nonNullable.control('', [
      Validators.required,
      Validators.email,
      Validators.maxLength(254),
    ]),
    phoneNumber: this.formBuilder.nonNullable.control('', [
      Validators.required,
      Validators.pattern(/^\+?[0-9][0-9\s-]{6,19}$/),
    ]),
    jobRoleId: this.formBuilder.control<number | null>(null, Validators.required),
    roleId: this.formBuilder.control<number | null>(null, Validators.required),
    siteId: this.formBuilder.control<number | null>(null, Validators.required),
  });

  constructor() {
    this.loadOptions();
  }

  isInvalid(control: AbstractControl): boolean {
    return control.invalid && (control.touched || control.dirty);
  }

  submit(): void {
    if (this.isSubmitting()) {
      return;
    }

    this.submitError.set(null);
    const payload = this.preparePayload();
    if (!payload) {
      return;
    }

    this.isSubmitting.set(true);
    this.employeeService.createEmployee(payload).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.created.emit();
      },
      error: () => {
        this.isSubmitting.set(false);
        this.submitError.set('EMPLOYEE_MANAGEMENT.CREATE_FAILED');
      },
    });
  }

  private preparePayload(): CreateEmployeeRequest | null {
    this.preparedPayload.set(null);

    if (this.employeeForm.invalid) {
      this.employeeForm.markAllAsTouched();
      return null;
    }

    const value = this.employeeForm.getRawValue();
    const payload: CreateEmployeeRequest = {
      firstName: value.firstName.trim(),
      lastName: value.lastName.trim(),
      email: value.email.trim(),
      phoneNumber: value.phoneNumber.trim(),
      jobRoleId: value.jobRoleId!,
      roleId: value.roleId!,
      siteId: value.siteId!,
      createdAt: new Date().toISOString(),
    };
    this.preparedPayload.set(payload);
    return payload;
  }

  private loadOptions(): void {
    forkJoin({
      jobRoles: this.employeeService.getJobRoles(),
      roles: this.employeeService.getRoles(),
      sites: this.employeeService.getSites(),
    }).subscribe({
      next: ({ jobRoles, roles, sites }) => {
        this.jobRoles.set(jobRoles);
        this.roles.set(roles);
        this.sites.set(sites);
        this.loadingOptions.set(false);
      },
      error: () => {
        this.optionsLoadFailed.set(true);
        this.loadingOptions.set(false);
      },
    });
  }
}
