// import { Component, computed, effect, inject, input, output, signal } from '@angular/core';
// import { type AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
// import { TranslatePipe } from '@ngx-translate/core';
// import { forkJoin } from 'rxjs';
// import { EmployeeService } from '../../services/employee.service';
// import { InsertEmployee } from '../../models/insert-employee/insert-employee.models';
// import { Job } from '@app/core/models/job';
// import { Role } from '@app/core/models/role.models';
// import { Site } from '@app/core/models/site.models';


// function nonWhitespace(control: AbstractControl): Record<string, true> | null {
//   return typeof control.value === 'string' && control.value.trim().length > 0
//     ? null
//     : { whitespace: true };
// }

// @Component({
//   selector: 'app-employee-create',
//   standalone: true,
//   imports: [ReactiveFormsModule, TranslatePipe],
//   templateUrl: './employee-create.component.html',
//   styleUrl: './employee-create.component.css',
// })
// export class EmployeeCreateComponent {
//   private readonly formBuilder = inject(FormBuilder);
//   private readonly employeeService = inject(EmployeeService);

//   readonly employee = input<EmployeeApiResponse | null>(null);
//   readonly saved = output<void>();
//   readonly jobRoles = signal<readonly Job[]>([]);
//   readonly roles = signal<readonly Role[]>([]);
//   readonly sites = signal<readonly Site[]>([]);
//   readonly loadingOptions = signal(true);
//   readonly optionsLoadFailed = signal(false);
//   readonly preparedPayload = signal<CreateEmployeeRequest | null>(null);
//   readonly isSubmitting = signal(false);
//   readonly submitError = signal<string | null>(null);
//   readonly isEditMode = computed(() => this.employee() !== null);

//   readonly employeeForm = this.formBuilder.group({
//     firstName: this.formBuilder.nonNullable.control('', [
//       Validators.required,
//       Validators.maxLength(100),
//       nonWhitespace,
//     ]),
//     lastName: this.formBuilder.nonNullable.control('', [
//       Validators.required,
//       Validators.maxLength(100),
//       nonWhitespace,
//     ]),
//     email: this.formBuilder.nonNullable.control('', [
//       Validators.required,
//       Validators.email,
//       Validators.maxLength(254),
//     ]),
//     phoneNumber: this.formBuilder.nonNullable.control('', [
//       Validators.required,
//       Validators.pattern(/^\+?[0-9][0-9\s-]{6,19}$/),
//     ]),
//     jobRoleId: this.formBuilder.control<number | null>(null, Validators.required),
//     roleId: this.formBuilder.control<number | null>(null, Validators.required),
//     siteId: this.formBuilder.control<number | null>(null, Validators.required),
//   });

//   constructor() {
//     this.loadOptions();
//     effect(() => {
//       const employee = this.employee();
//       if (!employee) {
//         this.employeeForm.reset();
//         return;
//       }

//       this.employeeForm.reset({
//         firstName: employee.firstName,
//         lastName: employee.lastName,
//         email: employee.email,
//         phoneNumber: employee.phoneNumber,
//         jobRoleId: employee.jobRoleId,
//         roleId: employee.roleId,
//         siteId: employee.siteId,
//       });
//     });
//   }

//   isInvalid(control: AbstractControl): boolean {
//     return control.invalid && (control.touched || control.dirty);
//   }

//   submit(): void {
//     if (this.isSubmitting()) {
//       return;
//     }

//     this.submitError.set(null);
//     const payload = this.preparePayload();
//     if (!payload) {
//       return;
//     }

//     this.isSubmitting.set(true);
//     const employee = this.employee();
//     const request = employee
//       ? this.employeeService.updateEmployee(employee.id, {
//           ...payload,
//           createdAt: employee.createdAt,
//         })
//       : this.employeeService.createEmployee(payload);

//     request.subscribe({
//       next: () => {
//         this.isSubmitting.set(false);
//         this.saved.emit();
//       },
//       error: () => {
//         this.isSubmitting.set(false);
//         this.submitError.set(
//           employee ? 'EMPLOYEE_MANAGEMENT.UPDATE_FAILED' : 'EMPLOYEE_MANAGEMENT.CREATE_FAILED',
//         );
//       },
//     });
//   }

//   private preparePayload(): InsertEmployee | null {
//     this.preparedPayload.set(null);

//     if (this.employeeForm.invalid) {
//       this.employeeForm.markAllAsTouched();
//       return null;
//     }

//     const value = this.employeeForm.getRawValue();
//     const payload: InsertEmployee = {
//       firstName: value.firstName.trim(),
//       lastName: value.lastName.trim(),
//       email: value.email.trim(),
//       phoneNumber: value.phoneNumber.trim(),
//       jobRoleId: value.jobRoleId!,
//       roleId: value.roleId!,
//       siteId: value.siteId!,
//       createdAt: new Date().toISOString(),
//     };
//     this.preparedPayload.set(payload);
//     return payload;
//   }

//   private loadOptions(): void {
//     forkJoin({
//       jobRoles: this.employeeService.getJobRoles(),
//       roles: this.employeeService.getRoles(),
//       sites: this.employeeService.getSites(),
//     }).subscribe({
//       next: ({ jobRoles, roles, sites }) => {
//         this.jobRoles.set(jobRoles);
//         this.roles.set(roles);
//         this.sites.set(sites);
//         this.loadingOptions.set(false);
//       },
//       error: () => {
//         this.optionsLoadFailed.set(true);
//         this.loadingOptions.set(false);
//       },
//     });
//   }
// }
