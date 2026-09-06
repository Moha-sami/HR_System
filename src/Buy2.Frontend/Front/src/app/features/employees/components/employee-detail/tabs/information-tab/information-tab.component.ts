import { Component, inject, OnInit, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { EmployeeDetailService } from '../../../../services/employee-detail.service';
import type {
  DayOfWeekCode,
  GenderCode,
  SalaryTypeCode,
} from '../../../../models/view-employee/information-tab.models';

interface PersonalForm {
  firstName: string;
  lastName: string;
  phoneNumber: string;
  /** yyyy-MM-dd for the date input; empty means unchanged. */
  birthdate: string;
  gender: number | null;
  email: string;
}

interface JobForm {
  jobRoleId: number | null;
  directManagerId: number | null;
  seniorityLevel: string;
  experienceYears: number;
  jobType: string;
  attendanceType: string;
  onlineWorkdays: string[];
  offlineWorkdays: string[];
  qualifications: string[];
}

interface PayrollForm {
  salaryType: SalaryTypeCode;
  payoutPeriod: string;
  payoutDay: number | null;
  workWeekStartDay: DayOfWeekCode;
  workWeekEndDay: DayOfWeekCode;
  paymentAmount: number | null;
  overtimeThresholdHours: number | null;
  overtimeRateMultiplier: number | null;
  assignedWorkSiteIds: number[];
}

const EMPTY_PERSONAL: PersonalForm = {
  firstName: '',
  lastName: '',
  phoneNumber: '',
  birthdate: '',
  gender: null,
  email: '',
};

const EMPTY_JOB: JobForm = {
  jobRoleId: null,
  directManagerId: null,
  seniorityLevel: '',
  experienceYears: 0,
  jobType: '',
  attendanceType: '',
  onlineWorkdays: [],
  offlineWorkdays: [],
  qualifications: [],
};

const EMPTY_PAYROLL: PayrollForm = {
  salaryType: 1,
  payoutPeriod: '',
  payoutDay: null,
  workWeekStartDay: 0,
  workWeekEndDay: 4,
  paymentAmount: null,
  overtimeThresholdHours: null,
  overtimeRateMultiplier: null,
  assignedWorkSiteIds: [],
};

@Component({
  selector: 'app-information-tab',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './information-tab.component.html',
})
export class InformationTabComponent implements OnInit {
  private readonly employeeDetailService = inject(EmployeeDetailService);

  readonly saved = output<void>();

  // Personal Info section
  readonly personalEditing = signal(false);
  readonly personalSaving = signal(false);
  readonly personalSaveError = signal<string | null>(null);
  readonly personalForm = signal<PersonalForm>({ ...EMPTY_PERSONAL });

  // Job Details section
  readonly jobEditing = signal(false);
  readonly jobSaving = signal(false);
  readonly jobSaveError = signal<string | null>(null);
  readonly jobForm = signal<JobForm>({
    ...EMPTY_JOB,
    onlineWorkdays: [],
    offlineWorkdays: [],
    qualifications: [],
  });

  // Payroll section (embedded in this tab; there is no separate payroll tab)
  readonly payrollEditing = signal(false);
  readonly payrollSaving = signal(false);
  readonly payrollLoading = signal(false);
  readonly payrollSaveError = signal<string | null>(null);
  readonly payrollForm = signal<PayrollForm>({ ...EMPTY_PAYROLL, assignedWorkSiteIds: [] });

  // Lookups (loaded eagerly on tab init)
  readonly jobs = signal<readonly { id: number; title: string }[]>([]);
  readonly managers = signal<
    readonly { id: number; employeeCode: string; employeeName: string; jobTitle: string }[]
  >([]);
  readonly sites = signal<readonly { id: number; siteName: string }[]>([]);
  readonly lookupsLoading = signal(false);
  readonly lookupsError = signal<string | null>(null);

  readonly newQualification = signal('');

  readonly weekDays = ['Saturday', 'Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'];
  readonly weekDayValues: DayOfWeekCode[] = [6, 0, 1, 2, 3, 4, 5];
  readonly attendanceOptions = ['OnSite', 'Remote', 'Hybrid'];

  // Computed getters for employee data
  readonly employee = this.employeeDetailService.detailEmployee;

  ngOnInit(): void {
    this.loadLookups();
    this.loadFormData();
  }

  // Lookups
  loadLookups(): void {
    this.lookupsLoading.set(true);
    this.lookupsError.set(null);

    this.employeeDetailService.getJobsLookup(1, 100).subscribe({
      next: (res) => this.jobs.set(res.items),
      error: () => this.lookupsError.set('EMPLOYEE_DETAIL.MESSAGES.LOOKUP_ERROR'),
    });

    this.employeeDetailService.getEmployeesLookup(1, 50).subscribe({
      next: (res) => this.managers.set(res.items),
      error: () => this.lookupsError.set('EMPLOYEE_DETAIL.MESSAGES.LOOKUP_ERROR'),
    });

    this.employeeDetailService.getSitesLookup().subscribe({
      next: (rows) => {
        this.sites.set(rows);
        this.lookupsLoading.set(false);
      },
      error: () => {
        this.lookupsError.set('EMPLOYEE_DETAIL.MESSAGES.LOOKUP_ERROR');
        this.lookupsLoading.set(false);
      },
    });
  }

  // Form data loading
  loadFormData(): void {
    const emp = this.employee();
    if (!emp) return;

    const sourceName = emp.personalInfo?.name || emp.fullName || '';
    const [first, ...rest] = sourceName.split(' ');
    this.personalForm.set({
      firstName: first || '',
      lastName: rest.join(' ') || '',
      phoneNumber: emp.personalInfo?.phoneNumber || emp.phone || '',
      birthdate: emp.personalInfo?.birthdate ? emp.personalInfo.birthdate.split('T')[0] : '',
      gender: emp.personalInfo?.gender ?? null,
      email: emp.personalInfo?.email || emp.email || '',
    });

    this.jobForm.set({
      jobRoleId: emp.jobDetails.jobRoleId ?? null,
      directManagerId: emp.jobDetails.directManagerId ?? null,
      seniorityLevel: emp.jobDetails.seniorityLevel || '',
      experienceYears: emp.jobDetails.experienceYears || 0,
      jobType: emp.jobDetails.jobType || '',
      attendanceType: emp.jobDetails.attendanceType || '',
      onlineWorkdays: [...(emp.jobDetails.onlineWorkdays || [])],
      offlineWorkdays: [...(emp.jobDetails.offlineWorkdays || [])],
      qualifications: [...(emp.jobDetails.qualifications || [])],
    });
  }

  loadPayrollForm(): void {
    const emp = this.employee();
    if (!emp) return;

    this.payrollLoading.set(true);
    this.payrollSaveError.set(null);

    this.employeeDetailService.getEmployeePayroll(emp.id).subscribe({
      next: (profile) => {
        this.payrollForm.set({
          salaryType: (profile.salaryType === 2 ? 2 : 1) as SalaryTypeCode,
          payoutPeriod: profile.payoutPeriod || '',
          payoutDay: profile.payoutDay || null,
          workWeekStartDay: profile.workWeekStartDay as DayOfWeekCode,
          workWeekEndDay: profile.workWeekEndDay as DayOfWeekCode,
          paymentAmount: profile.paymentAmount || null,
          overtimeThresholdHours: profile.overtimeThresholdHours || null,
          // Backend stores the rate; the update contract names it multiplier.
          overtimeRateMultiplier: profile.overtimeHourlyRate || null,
          assignedWorkSiteIds: [...(profile.workSiteIds || [])],
        });
        this.payrollLoading.set(false);
      },
      error: () => {
        this.payrollLoading.set(false);
        this.payrollSaveError.set('EMPLOYEE_DETAIL.MESSAGES.SAVE_ERROR');
      },
    });
  }

  // Personal Info section handlers
  togglePersonalEdit(): void {
    this.personalEditing.update((v) => !v);
    this.personalSaveError.set(null);
    if (this.personalEditing()) {
      this.loadFormData();
    }
  }

  onPersonalSave(): void {
    const emp = this.employee();
    if (!emp || this.personalSaving()) return;

    const form = this.personalForm();
    if (form.birthdate) {
      const today = new Date().toISOString().split('T')[0];
      if (form.birthdate > today) {
        this.personalSaveError.set('EMPLOYEE_DETAIL.MESSAGES.SAVE_ERROR');
        return;
      }
    }

    this.personalSaving.set(true);
    this.personalSaveError.set(null);

    this.employeeDetailService
      .updatePersonalInfo(emp.id, {
        firstName: form.firstName,
        lastName: form.lastName,
        phoneNumber: form.phoneNumber,
        birthdate: form.birthdate ? `${form.birthdate}T00:00:00.000Z` : undefined,
        gender: (form.gender as GenderCode | null) ?? undefined,
        email: form.email || undefined,
      })
      .subscribe({
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
    if (this.jobEditing()) {
      this.loadFormData();
    }
  }

  onJobSave(): void {
    const emp = this.employee();
    if (!emp || this.jobSaving()) return;

    this.jobSaving.set(true);
    this.jobSaveError.set(null);

    const form = this.jobForm();
    this.employeeDetailService
      .updateJobDetails(emp.id, {
        jobRoleId: form.jobRoleId ?? undefined,
        directManagerId: form.directManagerId ?? undefined,
        seniorityLevel: form.seniorityLevel || undefined,
        experienceYears: form.experienceYears,
        jobType: form.jobType || undefined,
        attendanceType: form.attendanceType || undefined,
        onlineWorkdays: form.onlineWorkdays,
        offlineWorkdays: form.offlineWorkdays,
        qualifications: form.qualifications,
      })
      .subscribe({
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

  toggleJobOnlineDay(day: string): void {
    this.jobForm.update((form) => ({
      ...form,
      onlineWorkdays: form.onlineWorkdays.includes(day)
        ? form.onlineWorkdays.filter((d) => d !== day)
        : [...form.onlineWorkdays, day],
    }));
  }

  toggleJobOfflineDay(day: string): void {
    this.jobForm.update((form) => ({
      ...form,
      offlineWorkdays: form.offlineWorkdays.includes(day)
        ? form.offlineWorkdays.filter((d) => d !== day)
        : [...form.offlineWorkdays, day],
    }));
  }

  addQualification(): void {
    const qual = this.newQualification().trim();
    if (!qual) return;
    this.jobForm.update((form) =>
      form.qualifications.includes(qual)
        ? form
        : { ...form, qualifications: [...form.qualifications, qual] },
    );
    this.newQualification.set('');
  }

  removeQualification(qual: string): void {
    this.jobForm.update((form) => ({
      ...form,
      qualifications: form.qualifications.filter((q) => q !== qual),
    }));
  }

  // Payroll section handlers
  togglePayrollEdit(): void {
    this.payrollEditing.update((v) => !v);
    this.payrollSaveError.set(null);
    if (this.payrollEditing()) {
      this.loadPayrollForm();
    }
  }

  onPayrollSave(): void {
    const emp = this.employee();
    if (!emp || this.payrollSaving()) return;

    this.payrollSaving.set(true);
    this.payrollSaveError.set(null);

    const form = this.payrollForm();
    this.employeeDetailService
      .updatePayrollProfile(emp.id, {
        salaryType: form.salaryType,
        payoutPeriod: form.payoutPeriod || undefined,
        payoutDay: form.payoutDay ?? undefined,
        workWeekStartDay: form.workWeekStartDay,
        workWeekEndDay: form.workWeekEndDay,
        paymentAmount: form.paymentAmount ?? undefined,
        overtimeThresholdHours: form.overtimeThresholdHours ?? undefined,
        overtimeRateMultiplier: form.overtimeRateMultiplier ?? undefined,
        assignedWorkSiteIds: form.assignedWorkSiteIds,
      })
      .subscribe({
        next: () => {
          this.payrollSaving.set(false);
          this.payrollEditing.set(false);
          this.saved.emit();
        },
        error: () => {
          this.payrollSaving.set(false);
          this.payrollSaveError.set('EMPLOYEE_DETAIL.MESSAGES.SAVE_ERROR');
        },
      });
  }

  cancelPayrollEdit(): void {
    this.payrollEditing.set(false);
    this.payrollSaveError.set(null);
  }

  togglePayrollSite(siteId: number): void {
    this.payrollForm.update((form) => ({
      ...form,
      assignedWorkSiteIds: form.assignedWorkSiteIds.includes(siteId)
        ? form.assignedWorkSiteIds.filter((id) => id !== siteId)
        : [...form.assignedWorkSiteIds, siteId],
    }));
  }

  readonly formatGender = (gender: number | null): string => {
    if (gender === 1) return 'Male';
    if (gender === 2) return 'Female';
    return '—';
  };

  readonly formatSalaryType = (salaryType: string | null): string => {
    if (!salaryType) return '—';
    return salaryType;
  };

  readonly dayName = (day: number | null): string => {
    const names = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
    return day === null || day === undefined ? '—' : (names[day] ?? '—');
  };
}
