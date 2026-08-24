import { Component, inject, input, output, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { JobService, Job } from '../../services/job.service';

interface PerformanceMetric {
  name: string;
  description: string;
  measure: string;
  target: string;
  weight: string;
}

interface FixedTask {
  name: string;
  description: string;
  steps: string[];
  repeat: string;
  submissionTime: string;
  submissionTimeAmPm: string;
}

interface JobForm {
  jobTitle: string;
  jobDescription: string;
  department: string;
  qualifications: string[];
  experienceLevel: string;
  reportingManager: string;
  scheduleType: string;
  checkInFrom: string;
  checkInFromAmPm: string;
  checkInTo: string;
  checkInToAmPm: string;
  checkOutFrom: string;
  checkOutFromAmPm: string;
  checkOutTo: string;
  checkOutToAmPm: string;
  hoursPerDay: string;
  metrics: PerformanceMetric[];
  fixedTasks: FixedTask[];
}

@Component({
  selector: 'app-job-create',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './job-create.component.html',
  styleUrls: ['./job-create.component.css'],
})
export class JobCreateComponent implements OnInit {
  private readonly jobService = inject(JobService);
  private readonly router = inject(Router);

  readonly close = output<void>();
  readonly id = input<string>(); // For edit mode

  isEditMode = false;

  activeTab: 'information' | 'schedule' | 'metrics' | 'tasks' = 'information';

  form: JobForm = {
    jobTitle: '',
    jobDescription: '',
    department: '',
    qualifications: [],
    experienceLevel: '',
    reportingManager: '',
    scheduleType: 'fixed',
    checkInFrom: '',
    checkInFromAmPm: 'AM',
    checkInTo: '',
    checkInToAmPm: 'AM',
    checkOutFrom: '',
    checkOutFromAmPm: 'AM',
    checkOutTo: '',
    checkOutToAmPm: 'AM',
    hoursPerDay: '',
    metrics: [],
    fixedTasks: [],
  };

  metricForm: PerformanceMetric = {
    name: '',
    description: '',
    measure: '',
    target: '',
    weight: ''
  };

  searchQualification = '';
  showQualificationsDropdown = false;
  showDepartmentDropdown = false;
  searchDepartment = '';
  kpiInput = '';
  taskForm: FixedTask = {
    name: '',
    description: '',
    steps: [],
    repeat: 'Daily',
    submissionTime: '',
    submissionTimeAmPm: 'AM'
  };

  taskStepInput = '';

  departments: any[] = [];
  availableQualifications: any[] = [];
  filteredDepartments: any[] = [];

  readonly managers = [
    'Ahmed Hassan',
    'Sara Mohamed',
    'Khalid Ali',
    'Nora Ahmed',
    'Omar Ibrahim',
    'Layla Saleh'
  ];

  showSuccessModal = signal(false);

  ngOnInit(): void {
    const jobId = this.id();
    if (jobId) {
      this.isEditMode = true;
      this.loadJob(jobId);
    }

    this.jobService.getDepartments().subscribe({
      next: (data) => {
        this.departments = data;
        this.filteredDepartments = data;
      },
      error: (err) => console.error('Failed to load departments', err)
    });

    this.jobService.getQualifications().subscribe({
      next: (data) => this.availableQualifications = data,
      error: (err) => console.error('Failed to load qualifications', err)
    });
  }

  loadJob(id: string): void {
    this.jobService.getJob(id).subscribe({
      next: (job) => {
        this.form.jobTitle = job.jobName;
        this.form.jobDescription = job.jobDescription;
        this.form.department = job.department || '';
        this.form.qualifications = job.qualifications || [];
      },
      error: (err) => console.error('Failed to load job:', err)
    });
  }

  isFormValid(): boolean {
    return !!this.form.jobTitle;
  }

  nextTab(): void {
    const tabs = ['information', 'schedule', 'metrics', 'tasks'];
    const currentIndex = tabs.indexOf(this.activeTab);
    if (currentIndex < tabs.length - 1) {
      this.activeTab = tabs[currentIndex + 1] as any;
    }
  }

  prevTab(): void {
    const tabs = ['information', 'schedule', 'metrics', 'tasks'];
    const currentIndex = tabs.indexOf(this.activeTab);
    if (currentIndex > 0) {
      this.activeTab = tabs[currentIndex - 1] as any;
    }
  }

  // ===== Department Dropdown =====
  toggleDepartmentDropdown(): void {
    this.showDepartmentDropdown = !this.showDepartmentDropdown;
    if (this.showDepartmentDropdown) {
      this.filteredDepartments = this.departments;
      this.searchDepartment = '';
    }
  }

  filterDepartments(search: string): void {
    const term = search.toLowerCase().trim();
    if (!term) {
      this.filteredDepartments = this.departments;
    } else {
      this.filteredDepartments = this.departments.filter(d =>
        (d.name || d).toLowerCase().includes(term)
      );
    }
  }

  selectDepartment(dept: any): void {
    this.form.department = dept.name || dept;
    this.showDepartmentDropdown = false;
  }

  createNewDepartment(): void {
    const name = this.searchDepartment.trim();
    if (name) {
      this.jobService.createDepartment({ name }).subscribe({
        next: (newDept) => {
          this.departments.push(newDept);
          this.filteredDepartments = this.departments;
          this.form.department = newDept.name;
          this.showDepartmentDropdown = false;
          this.searchDepartment = '';
        },
        error: (err) => console.error('Failed to create department', err)
      });
    }
  }

  // ===== Qualifications =====
  toggleQualificationsDropdown(): void {
    this.showQualificationsDropdown = !this.showQualificationsDropdown;
  }

  toggleQualification(qual: any): void {
    const name = qual.name ? qual.name : qual;
    if (this.form.qualifications.includes(name)) {
      this.form.qualifications = this.form.qualifications.filter(q => q !== name);
    } else {
      this.form.qualifications.push(name);
    }
  }

  get filteredQualifications(): any[] {
    const search = this.searchQualification.toLowerCase().trim();
    if (!search) return this.availableQualifications;
    return this.availableQualifications.filter(q => {
      const name = q.name ? q.name : q;
      return name.toLowerCase().includes(search);
    });
  }

  createNewQualification(): void {
    const name = this.searchQualification.trim();
    if (name) {
      // Check if already exists
      const exists = this.availableQualifications.some(q =>
        (q.name || q).toLowerCase() === name.toLowerCase()
      );

      if (!exists) {
        this.jobService.createQualification({ name }).subscribe({
          next: (newQual) => {
            this.availableQualifications.push(newQual);
            this.form.qualifications.push(newQual.name);
            this.searchQualification = '';
            this.showQualificationsDropdown = false;
          },
          error: (err) => console.error('Failed to create qualification', err)
        });
      } else {
        // If exists, just select it
        const existing = this.availableQualifications.find(q =>
          (q.name || q).toLowerCase() === name.toLowerCase()
        );
        if (existing) {
          const existingName = existing.name || existing;
          if (!this.form.qualifications.includes(existingName)) {
            this.form.qualifications.push(existingName);
          }
          this.searchQualification = '';
          this.showQualificationsDropdown = false;
        }
      }
    }
  }

  removeQualification(qual: string): void {
    this.form.qualifications = this.form.qualifications.filter(q => q !== qual);
  }

  // ===== Metrics =====
  addPerformanceMetric(): void {
    if (this.metricForm.name) {
      this.form.metrics.push({ ...this.metricForm });
      this.metricForm = {
        name: '',
        description: '',
        measure: '',
        target: '',
        weight: ''
      };
    }
  }

  removePerformanceMetric(index: number): void {
    this.form.metrics.splice(index, 1);
  }

  // ===== Fixed Tasks =====
  addTaskStep(): void {
    const value = this.taskStepInput.trim();
    if (value) {
      this.taskForm.steps.push(value);
      this.taskStepInput = '';
    }
  }

  addFixedTask(): void {
    if (this.taskForm.name) {
      this.form.fixedTasks.push({
        ...this.taskForm,
        steps: [...this.taskForm.steps]
      });
      this.taskForm = {
        name: '',
        description: '',
        steps: [],
        repeat: 'Daily',
        submissionTime: '',
        submissionTimeAmPm: 'AM'
      };
    }
  }

  removeFixedTask(index: number): void {
    this.form.fixedTasks.splice(index, 1);
  }

  // ===== Submit =====
  onSubmit(): void {
    if (!this.isFormValid()) return;

    const newJob: Omit<Job, 'id'> = {
      jobName: this.form.jobTitle,
      jobDescription: this.form.jobDescription || `Department: ${this.form.department}`,
      department: this.form.department,
      qualifications: this.form.qualifications,
      numberOfEmployees: 0,
    };

    if (this.isEditMode) {
      const jobId = this.id()!;
      this.jobService.updateJob(jobId, newJob).subscribe({
        next: () => {
          this.showSuccessModal.set(true);
        },
        error: (err: unknown) => console.error('Update job failed:', err),
      });
    } else {
      this.jobService.createJob(newJob).subscribe({
        next: () => {
          this.showSuccessModal.set(true);
        },
        error: (err: unknown) => console.error('Create job failed:', err),
      });
    }
  }

  closePanel(): void {
    this.close.emit();
    this.router.navigate(['/jobs']);
  }

  onSuccessClose(): void {
    this.showSuccessModal.set(false);
    this.close.emit();
    this.router.navigate(['/jobs']);
  }
}
