import { Component, inject, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { EmployeeDetailService } from '../../../../services/employee-detail.service';
import type {
  UpdatePersonalInfoRequestDto,
  UpdateJobDetailsRequestDto,
} from '../../../../models/view-employee/employee-profile';

@Component({
  selector: 'app-information-tab',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './information-tab.component.html',
})
export class InformationTabComponent {
  private readonly employeeDetailService = inject(EmployeeDetailService);

  readonly saved = output<void>();

  // Personal Info section
  readonly personalEditing = signal(false);
  readonly personalSaving = signal(false);
  readonly personalSaveError = signal<string | null>(null);
  readonly personalForm = signal<UpdatePersonalInfoRequestDto>({
    firstName: '',
    lastName: '',
    phoneNumber: '',
    dateOfBirth: '',
    address: '',
    emergencyContact: '',
    nationalId: '',
  });

  // Job Details section
  readonly jobEditing = signal(false);
  readonly jobSaving = signal(false);
  readonly jobSaveError = signal<string | null>(null);
  readonly jobForm = signal<UpdateJobDetailsRequestDto>({
    jobRoleId: 0,
    roleId: 0,
    siteId: 0,
    directManagerId: 0,
    seniorityLevel: '',
    experienceYears: 0,
    jobType: '',
    attendanceType: '',
    joinDate: '',
  });

  readonly newQualification = signal('');

  // Computed getters for employee data
  readonly employee = this.employeeDetailService.detailEmployee;

  // Form data loading
  loadFormData(): void {
    const emp = this.employee();
    if (emp) {
      const fullNameParts = emp.fullName.split(' ');
      this.personalForm.set({
        firstName: fullNameParts[0] || '',
        lastName: fullNameParts.slice(1).join(' ') || '',
        phoneNumber: emp.phone || '',
        dateOfBirth: emp.personalInfo?.birthdate ? emp.personalInfo.birthdate.split('T')[0] : '',
        address: '',
        emergencyContact: '',
        nationalId: '',
      });

      this.jobForm.set({
        jobRoleId: 0,
        roleId: 0,
        siteId: 0,
        directManagerId: 0,
        seniorityLevel: emp.jobDetails.seniorityLevel || '',
        experienceYears: emp.jobDetails.experienceYears || 0,
        jobType: emp.jobDetails.jobType || '',
        attendanceType: '',
        joinDate: '',
      });
    }
  }

  // Personal Info section handlers
  togglePersonalEdit(): void {
    this.personalEditing.update((v) => !v);
    this.personalSaveError.set(null);
    if (!this.personalEditing()) {
      this.loadFormData();
    }
  }

  onPersonalSave(): void {
    const emp = this.employee();
    if (!emp || this.personalSaving()) return;

    this.personalSaving.set(true);
    this.personalSaveError.set(null);

    const payload: UpdatePersonalInfoRequestDto = this.personalForm();

    this.employeeDetailService.updatePersonalInfo(emp.id, payload).subscribe({
      next: () => {
        this.personalSaving.set(false);
        this.personalEditing.set(false);
        this.saved.emit();
      },
      error: () => {
        this.personalSaving.set(false);
        this.personalSaveError.set('EMPLOYEE_DETAIL.MESSAGES.SAVE_ERROR');
      },
    });
  }

  cancelPersonalEdit(): void {
    this.personalEditing.set(false);
    this.loadFormData();
    this.personalSaveError.set(null);
  }

  // Job Details section handlers
  toggleJobEdit(): void {
    this.jobEditing.update((v) => !v);
    this.jobSaveError.set(null);
    if (!this.jobEditing()) {
      this.loadFormData();
    }
  }

  onJobSave(): void {
    const emp = this.employee();
    if (!emp || this.jobSaving()) return;

    this.jobSaving.set(true);
    this.jobSaveError.set(null);

    const payload: UpdateJobDetailsRequestDto = this.jobForm();

    this.employeeDetailService.updateJobDetails(emp.id, payload).subscribe({
      next: () => {
        this.jobSaving.set(false);
        this.jobEditing.set(false);
        this.saved.emit();
      },
      error: () => {
        this.jobSaving.set(false);
        this.jobSaveError.set('EMPLOYEE_DETAIL.MESSAGES.SAVE_ERROR');
      },
    });
  }

  cancelJobEdit(): void {
    this.jobEditing.set(false);
    this.loadFormData();
    this.jobSaveError.set(null);
  }

  addQualification(): void {
    const qual = this.newQualification().trim();
    if (qual) {
      this.newQualification.set('');
    }
  }

  readonly formatGender = (gender: number | null): string => {
    if (gender === 1) return 'Male';
    if (gender === 2) return 'Female';
    return '—';
  };
}
