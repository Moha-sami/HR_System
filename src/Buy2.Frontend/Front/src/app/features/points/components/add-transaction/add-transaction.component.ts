import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, HostListener, inject, signal, type OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';

import { ButtonComponent } from '../../../../shared/components/button/button.component';
import { ModalComponent } from '../../../../shared/components/modal/modal.component';
import { ModalBodyComponent } from '../../../../shared/components/modal/modal-body.component';
import type { PointsEmployee } from '../../models/points-transaction';
import { PointsManagementService } from '../../service/points-management.service';

const MIN_POINTS = 50;
const MAX_POINTS = 10000;
const MIN_COMMENTS = 3;
const MAX_COMMENTS = 1000;

@Component({
  selector: 'app-add-transaction',
  standalone: true,
  imports: [
    FormsModule,
    TranslatePipe,
    ButtonComponent,
    ModalComponent,
    ModalBodyComponent,
  ],
  templateUrl: './add-transaction.component.html',
  styleUrl: './add-transaction.component.css',
})
export class AddTransactionComponent implements OnDestroy {
  private readonly router = inject(Router);
  private readonly pointsService = inject(PointsManagementService);
  private readonly destroy$ = new Subject<void>();
  private readonly employeeSearchSubject = new Subject<string>();
  private employeesRequestId = 0;

  readonly employees = signal<PointsEmployee[]>([]);
  readonly employeeSearch = signal('');
  readonly selectedEmployeeId = signal<number | null>(null);
  readonly transactionType = signal<'Add' | 'Deduct'>('Add');
  readonly pointsValue = signal<number | null>(null);
  readonly comments = signal('');
  readonly showEmployeeDropdown = signal(false);
  readonly showSuccessModal = signal(false);
  readonly submitting = signal(false);
  readonly submitError = signal<string | null>(null);

  readonly canSubmit = computed(() => {
    const points = Number(this.pointsValue());
    const comments = this.comments().trim();
    return (
      this.selectedEmployeeId() != null &&
      Number.isFinite(points) &&
      points >= MIN_POINTS &&
      points <= MAX_POINTS &&
      comments.length >= MIN_COMMENTS &&
      comments.length <= MAX_COMMENTS &&
      !this.submitting()
    );
  });

  constructor() {
    this.employeeSearchSubject
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe((search) => {
        this.loadEmployees(search);
      });

    this.loadEmployees('');
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    this.showEmployeeDropdown.set(false);
  }

  openEmployeeDropdown(event: Event): void {
    event.stopPropagation();
    this.showEmployeeDropdown.set(true);
  }

  onEmployeeSearchChange(value: string): void {
    this.employeeSearch.set(value);

    const selectedId = this.selectedEmployeeId();
    if (selectedId != null) {
      const selected = this.employees().find((employee) => employee.id === selectedId);
      if (!selected || value !== selected.employeeName) {
        this.selectedEmployeeId.set(null);
      }
    }

    this.showEmployeeDropdown.set(true);
    this.employeeSearchSubject.next(value);
  }

  selectEmployee(employee: PointsEmployee, event: Event): void {
    event.stopPropagation();
    this.selectedEmployeeId.set(employee.id);
    this.employeeSearch.set(employee.employeeName);
    this.showEmployeeDropdown.set(false);
  }

  employeeFullName(employee: PointsEmployee): string {
    return employee.employeeName;
  }

  onDiscard(): void {
    void this.router.navigate(['/points']);
  }

  onSubmit(): void {
    const employeeId = this.selectedEmployeeId();
    const pointsValue = this.pointsValue();

    if (!this.canSubmit() || employeeId == null || pointsValue == null) {
      return;
    }

    this.submitting.set(true);
    this.submitError.set(null);

    this.pointsService
      .createTransaction({
        employeeId,
        pointsValue: Number(pointsValue),
        transactionType: this.transactionType(),
        comments: this.comments(),
      })
      .subscribe({
        next: (result) => {
          this.submitting.set(false);
          if (!result.isSuccess) {
            this.submitError.set(result.errorMessage || 'POINTS.FORM.SUBMIT_ERROR');
            return;
          }
          this.showSuccessModal.set(true);
        },
        error: (err: unknown) => {
          this.submitting.set(false);
          this.submitError.set(this.extractErrorMessage(err));
        },
      });
  }

  onSuccessClose(): void {
    this.showSuccessModal.set(false);
    void this.router.navigate(['/points']);
  }

  private loadEmployees(search: string): void {
    const requestId = ++this.employeesRequestId;

    this.pointsService.getEmployees(search).subscribe({
      next: (employees) => {
        if (requestId !== this.employeesRequestId) {
          return;
        }
        this.employees.set(employees);
      },
      error: () => {
        if (requestId !== this.employeesRequestId) {
          return;
        }
        this.employees.set([]);
      },
    });
  }

  private extractErrorMessage(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      if (typeof err.error === 'string' && err.error.trim()) {
        return err.error;
      }
      if (typeof err.error?.errorMessage === 'string' && err.error.errorMessage.trim()) {
        return err.error.errorMessage;
      }
      if (err.error?.errors && typeof err.error.errors === 'object') {
        const messages = Object.values(err.error.errors).flat();
        if (messages.length > 0) {
          return messages.join(' | ');
        }
      }
    }

    return 'POINTS.FORM.SUBMIT_ERROR';
  }
}
