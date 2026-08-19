import { Component, inject, input, output, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { JobService, Job } from '../../services/job.service';

interface JobForm {
  jobTitle: string;
  roleName: string;
  jobDescription: string;
  department: string;
  qualifications: string[];
  experienceLevel: string;
  reportingManager: string;
  workDays: string;
  shiftType: string;
  startTime: string;
  endTime: string;
  isRemote: boolean;
  kpis: string[];
  evaluationPeriod: string;
  performanceGoals: string;
  fixedTasks: string[];
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
  readonly jobId = input<string>(); // For edit mode

  isEditMode = false;

  activeTab: 'information' | 'schedule' | 'metrics' | 'tasks' = 'information';

  form: JobForm = {
    jobTitle: '',
    roleName: '',
    jobDescription: '',
    department: '',
    qualifications: [],
    experienceLevel: '',
    reportingManager: '',
    workDays: '',
    shiftType: '',
    startTime: '',
    endTime: '',
    isRemote: false,
    kpis: [],
    evaluationPeriod: '',
    performanceGoals: '',
    fixedTasks: [],
  };

  qualificationInput = '';
  searchQualification = '';
  showQualificationsDropdown = false;
  kpiInput = '';
  fixedTaskInput = '';

  departments: any[] = [];
  availableQualifications: any[] = [];

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
    const id = this.jobId();
    if (id) {
      this.isEditMode = true;
      this.loadJob(Number(id));
    }
    
    this.jobService.getDepartments().subscribe({
      next: (data) => this.departments = data,
      error: (err) => console.error('Failed to load departments', err)
    });

    this.jobService.getQualifications().subscribe({
      next: (data) => this.availableQualifications = data,
      error: (err) => console.error('Failed to load qualifications', err)
    });
  }

  loadJob(id: number): void {
    this.jobService.getJob(id).subscribe({
      next: (job) => {
        this.form.jobTitle = job.jobName;
        this.form.jobDescription = job.jobDescription;
        this.form.department = job.department || '';
        this.form.qualifications = job.qualifications || [];
        // Note: roleName, experienceLevel, reportingManager are not in the Job interface
        // You can add them to the Job interface if needed
      },
      error: (err) => console.error('Failed to load job:', err)
    });
  }

  isFormValid(): boolean {
    const info = this.form;
    return !!(
      info.jobTitle &&
      info.roleName &&
      info.department &&
      info.experienceLevel
    );
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

  addQualification(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const value = select.value;
    if (value && !this.form.qualifications.includes(value)) {
      this.form.qualifications.push(value);
    }
    // Reset select to default
    select.value = '';
  }

  removeQualification(qual: string): void {
    this.form.qualifications = this.form.qualifications.filter(q => q !== qual);
  }

  // ===== KPIs =====
  addKpi(): void {
    const value = this.kpiInput.trim();
    if (value && !this.form.kpis.includes(value)) {
      this.form.kpis.push(value);
    }
    this.kpiInput = '';
  }

  removeKpi(kpi: string): void {
    this.form.kpis = this.form.kpis.filter(k => k !== kpi);
  }

  // ===== Fixed Tasks =====
  addFixedTask(): void {
    const value = this.fixedTaskInput.trim();
    if (value && !this.form.fixedTasks.includes(value)) {
      this.form.fixedTasks.push(value);
    }
    this.fixedTaskInput = '';
  }

  removeFixedTask(task: string): void {
    this.form.fixedTasks = this.form.fixedTasks.filter(t => t !== task);
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
      const id = Number(this.jobId());
      this.jobService.updateJob(id, newJob).subscribe({
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
