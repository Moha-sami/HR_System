import { Component, inject, input, output, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { JobService } from '../../services/job.service';
import { environment } from '../../../../../environments/environment';

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

interface Department {
  id: number;
  name: string;
}

interface JobForm {
  jobTitle: string;
  jobDescription: string;
  departmentId: number | null;
  departmentName: string;     // for display
  seniorityLevel: string;
  qualifications: string[];
  experienceYearsMin: number;
  workModel: string;
  onlineWorkdays: string[];
  offlineWorkdays: string[];
  isActive: boolean;
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
  private readonly route = inject(ActivatedRoute);
  private readonly http = inject(HttpClient);

  readonly close = output<void>();
  readonly id = input<string>(); // For edit mode

  isEditMode = false;
  editJobId: number | null = null;

  activeTab: 'information' | 'schedule' | 'metrics' | 'tasks' = 'information';

  form: JobForm = {
    jobTitle: '',
    jobDescription: '',
    departmentId: null,
    departmentName: '',
    seniorityLevel: '',
    qualifications: [],
    experienceYearsMin: 0,
    workModel: 'OnSite',
    onlineWorkdays: [],
    offlineWorkdays: [],
    isActive: true,
    metrics: [],
    fixedTasks: [],
  };

  metricForm: PerformanceMetric = {
    name: '', description: '', measure: '', target: '', weight: ''
  };

  searchQualification = '';
  qualificationInput = '';
  showQualificationsDropdown = false;
  showDepartmentDropdown = false;
  searchDepartment = '';
  taskForm: FixedTask = {
    name: '', description: '', steps: [], repeat: 'Daily',
    submissionTime: '', submissionTimeAmPm: 'AM'
  };
  taskStepInput = '';

  departments: Department[] = [];
  filteredDepartments: Department[] = [];
  availableQualifications: string[] = [];

  readonly seniorityLevels = ['Junior', 'Mid', 'Senior', 'Lead', 'Executive'];
  readonly workModels = ['OnSite', 'Remote', 'Hybrid'];
  readonly weekdays = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

  showSuccessModal = signal(false);

  ngOnInit(): void {
    // Load departments from real API
    this.http.get<Department[]>(`${environment.baseUrl}/departments`).subscribe({
      next: (data) => {
        this.departments = data;
        this.filteredDepartments = data;
      },
      error: (err) => console.error('Failed to load departments', err)
    });

    // Check route param for edit mode
    const routeId = this.route.snapshot.paramMap.get('id') ?? this.id();
    if (routeId) {
      this.isEditMode = true;
      this.editJobId = +routeId;
      this.loadJob(+routeId);
    }
  }

  loadJob(id: number): void {
    this.jobService.getJob(id).subscribe({
      next: (job) => {
        this.form.jobTitle = job.title;
        this.form.jobDescription = job.description || '';
        this.form.departmentId = job.departmentId;
        this.form.departmentName = job.departmentName || '';
        this.form.seniorityLevel = job.seniorityLevel || '';
        this.form.qualifications = [...job.requiredQualifications];
        this.form.experienceYearsMin = job.experienceYearsMin;
        this.form.workModel = job.workModel || 'OnSite';
        this.form.onlineWorkdays = [...(job.onlineWorkdays || [])];
        this.form.offlineWorkdays = [...(job.offlineWorkdays || [])];
        this.form.isActive = job.isActive;
      },
      error: (err) => console.error('Failed to load job:', err)
    });
  }

  isFormValid(): boolean {
    return !!this.form.jobTitle && !!this.form.seniorityLevel && !!this.form.workModel;
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
    this.filteredDepartments = !term
      ? this.departments
      : this.departments.filter(d => d.name.toLowerCase().includes(term));
  }

  selectDepartment(dept: Department): void {
    this.form.departmentId = dept.id;
    this.form.departmentName = dept.name;
    this.showDepartmentDropdown = false;
  }

  createNewDepartment(): void {
    const name = this.searchDepartment.trim();
    if (!name) return;
    // For create: use newDepartmentName field (API supports it)
    this.form.departmentId = null;
    this.form.departmentName = name;
    this.showDepartmentDropdown = false;
    this.searchDepartment = '';
  }

  // ===== Qualifications =====
  toggleQualificationsDropdown(): void {
    this.showQualificationsDropdown = !this.showQualificationsDropdown;
  }

  addQualification(): void {
    const q = this.qualificationInput.trim();
    if (q && !this.form.qualifications.includes(q)) {
      this.form.qualifications.push(q);
    }
    this.qualificationInput = '';
  }

  removeQualification(qual: string): void {
    this.form.qualifications = this.form.qualifications.filter(q => q !== qual);
  }

  get filteredQualifications(): string[] {
    const search = this.searchQualification.toLowerCase().trim();
    return !search ? this.availableQualifications
      : this.availableQualifications.filter(q => q.toLowerCase().includes(search));
  }

  toggleQualification(qual: string): void {
    if (this.form.qualifications.includes(qual)) {
      this.form.qualifications = this.form.qualifications.filter(q => q !== qual);
    } else {
      this.form.qualifications.push(qual);
    }
  }

  createNewQualification(): void {
    const name = this.searchQualification.trim();
    if (name && !this.form.qualifications.includes(name)) {
      this.form.qualifications.push(name);
      this.searchQualification = '';
    }
  }

  // ===== Workdays toggles =====
  toggleOnlineWorkday(day: string): void {
    const idx = this.form.onlineWorkdays.indexOf(day);
    if (idx > -1) this.form.onlineWorkdays.splice(idx, 1);
    else this.form.onlineWorkdays.push(day);
  }

  toggleOfflineWorkday(day: string): void {
    const idx = this.form.offlineWorkdays.indexOf(day);
    if (idx > -1) this.form.offlineWorkdays.splice(idx, 1);
    else this.form.offlineWorkdays.push(day);
  }

  // ===== Metrics =====
  addPerformanceMetric(): void {
    if (this.metricForm.name) {
      this.form.metrics.push({ ...this.metricForm });
      this.metricForm = { name: '', description: '', measure: '', target: '', weight: '' };
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
      this.form.fixedTasks.push({ ...this.taskForm, steps: [...this.taskForm.steps] });
      this.taskForm = { name: '', description: '', steps: [], repeat: 'Daily', submissionTime: '', submissionTimeAmPm: 'AM' };
    }
  }

  removeFixedTask(index: number): void {
    this.form.fixedTasks.splice(index, 1);
  }

  // ===== Submit =====
  onSubmit(): void {
    if (!this.isFormValid()) return;

    if (this.isEditMode && this.editJobId) {
      // PUT /jobs/{id}
      const updatePayload: any = {
        title: this.form.jobTitle,
        departmentId: this.form.departmentId ?? 0,
        seniorityLevel: this.form.seniorityLevel,
        description: this.form.jobDescription,
        requiredQualifications: this.form.qualifications,
        experienceYearsMin: this.form.experienceYearsMin,
        workModel: this.form.workModel,
        onlineWorkdays: this.form.onlineWorkdays,
        offlineWorkdays: this.form.offlineWorkdays,
        isActive: this.form.isActive,
      };

      this.jobService.updateJob(this.editJobId, updatePayload).subscribe({
        next: () => this.showSuccessModal.set(true),
        error: (err: unknown) => console.error('Update job failed:', err),
      });
    } else {
      // POST /jobs
      const createPayload: any = {
        title: this.form.jobTitle,
        ...(this.form.departmentId
          ? { departmentId: this.form.departmentId }
          : { newDepartmentName: this.form.departmentName }),
        seniorityLevel: this.form.seniorityLevel,
        description: this.form.jobDescription,
        requiredQualifications: this.form.qualifications,
        experienceYearsMin: this.form.experienceYearsMin,
        workModel: this.form.workModel,
        onlineWorkdays: this.form.onlineWorkdays,
        offlineWorkdays: this.form.offlineWorkdays,
      };

      this.jobService.createJob(createPayload).subscribe({
        next: () => this.showSuccessModal.set(true),
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
