import { Component, DestroyRef, inject, output, signal } from '@angular/core';
import {
  FormArray,
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
  type AbstractControl,
  type ValidationErrors,
} from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonComponent } from '@app/shared/components/button/button.component';
import { ModalBodyComponent } from '@app/shared/components/modal/modal-body.component';
import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { ModalFooterComponent } from '@app/shared/components/modal/modal-footer.component';
import { ModalHeaderComponent } from '@app/shared/components/modal/modal-header.component';
import type { Job } from '@app/core/models/job';
import type { Role } from '@app/core/models/role.models';
import type {
  BulkOnboardEmployeeItem,
  BulkOnboardResult,
} from '../../models/bulk-onboard/bulk-onboard.models';
import { EmployeeService } from '../../services/employee.service';

interface EmployeeRowControls {
  firstName: FormControl<string>;
  lastName: FormControl<string>;
  email: FormControl<string>;
  jobRoleId: FormControl<number | null>;
  roleId: FormControl<number | null>;
}

type EmployeeRowForm = FormGroup<EmployeeRowControls>;

function nonWhitespace(control: AbstractControl): ValidationErrors | null {
  return typeof control.value === 'string' && control.value.trim().length > 0
    ? null
    : { whitespace: true };
}

@Component({
  selector: 'app-employee-bulk-onboard',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    TranslatePipe,
    ButtonComponent,
    ModalComponent,
    ModalHeaderComponent,
    ModalBodyComponent,
    ModalFooterComponent,
  ],
  templateUrl: './employee-bulk-onboard.component.html',
})
export class EmployeeBulkOnboardComponent {
  private readonly fb = inject(FormBuilder);
  private readonly employeeService = inject(EmployeeService);
  private readonly destroyRef = inject(DestroyRef);

  readonly closed = output<void>();
  readonly employeesCreated = output<number>();
  readonly completed = output<void>();

  readonly roles = signal<readonly Role[]>([]);
  readonly jobs = signal<readonly Job[]>([]);
  readonly lookupsLoading = signal(true);
  readonly lookupError = signal(false);
  readonly isSubmitting = signal(false);
  readonly submitError = signal(false);
  readonly resultSummary = signal<{ createdCount: number; failedCount: number } | null>(null);
  readonly serverErrors = signal<Map<EmployeeRowForm, string>>(new Map());

  readonly form = this.fb.group({
    employees: this.fb.array<EmployeeRowForm>([]),
  });

  constructor() {
    this.addEmployee();
    this.loadLookups();
  }

  get employees(): FormArray<EmployeeRowForm> {
    return this.form.controls.employees;
  }

  addEmployee(): void {
    if (this.isSubmitting()) return;
    this.employees.push(this.createEmployeeRow());
  }

  removeEmployee(index: number): void {
    if (this.isSubmitting()) return;
    this.employees.removeAt(index);
    if (this.employees.length === 0) this.addEmployee();
  }

  close(): void {
    if (!this.isSubmitting()) this.closed.emit();
  }

  submit(): void {
    if (this.isSubmitting()) return;

    if (this.form.invalid || this.lookupsLoading() || this.lookupError()) {
      this.form.markAllAsTouched();
      return;
    }

    const snapshot = this.employees.controls.map((row) => this.toPayload(row));
    if (snapshot.length === 0) return;

    this.isSubmitting.set(true);
    this.submitError.set(false);
    this.resultSummary.set(null);

    this.employeeService.bulkOnboardEmployees({ employees: snapshot }).subscribe({
      next: (result) => this.handleResult(result, snapshot),
      error: () => {
        this.isSubmitting.set(false);
        this.submitError.set(true);
      },
    });
  }

  isInvalid(control: AbstractControl): boolean {
    return control.invalid && (control.touched || control.dirty);
  }

  serverError(row: EmployeeRowForm): string | null {
    return this.serverErrors().get(row) ?? null;
  }

  private loadLookups(): void {
    this.lookupsLoading.set(true);
    this.lookupError.set(false);

    forkJoin({
      roles: this.employeeService.getBulkOnboardingRoles(),
      jobs: this.employeeService.getBulkOnboardingJobs(),
    }).subscribe({
      next: ({ roles, jobs }) => {
        this.roles.set(roles.filter((role) => role.isActive !== false));
        this.jobs.set(jobs);
        this.lookupsLoading.set(false);
      },
      error: () => {
        this.lookupError.set(true);
        this.lookupsLoading.set(false);
      },
    });
  }

  private createEmployeeRow(value?: BulkOnboardEmployeeItem): EmployeeRowForm {
    const row = this.fb.group<EmployeeRowControls>({
      firstName: this.fb.nonNullable.control(value?.firstName ?? '', [
        Validators.required,
        nonWhitespace,
      ]),
      lastName: this.fb.nonNullable.control(value?.lastName ?? '', [
        Validators.required,
        nonWhitespace,
      ]),
      email: this.fb.nonNullable.control(value?.email ?? '', [
        Validators.required,
        Validators.email,
      ]),
      jobRoleId: this.fb.control<number | null>(value?.jobRoleId ?? null, Validators.required),
      roleId: this.fb.control<number | null>(value?.roleId ?? null, Validators.required),
    });

    row.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      if (!this.serverErrors().has(row)) return;
      const errors = new Map(this.serverErrors());
      errors.delete(row);
      this.serverErrors.set(errors);
      this.resultSummary.set(null);
    });

    return row;
  }

  private toPayload(row: EmployeeRowForm): BulkOnboardEmployeeItem {
    const value = row.getRawValue();
    return {
      firstName: value.firstName.trim(),
      lastName: value.lastName.trim(),
      email: value.email.trim(),
      jobRoleId: value.jobRoleId!,
      roleId: value.roleId!,
    };
  }

  private handleResult(
    result: BulkOnboardResult,
    snapshot: readonly BulkOnboardEmployeeItem[],
  ): void {
    this.isSubmitting.set(false);

    if (result.createdCount > 0) this.employeesCreated.emit(result.createdCount);

    if (result.failedCount === 0 && result.createdCount > 0) {
      this.completed.emit();
      return;
    }

    const failedRows: Array<{ value: BulkOnboardEmployeeItem; error: string }> = [];
    for (const failure of result.failedRows) {
      const value = snapshot[failure.rowIndex - 1];
      if (value) failedRows.push({ value, error: failure.errorMessage });
    }

    this.employees.clear();
    const errors = new Map<EmployeeRowForm, string>();
    for (const failedRow of failedRows) {
      const row = this.createEmployeeRow(failedRow.value);
      this.employees.push(row);
      errors.set(row, failedRow.error);
    }

    if (this.employees.length === 0) this.addEmployee();
    this.serverErrors.set(errors);
    this.resultSummary.set({
      createdCount: result.createdCount,
      failedCount: result.failedCount,
    });
  }
}
