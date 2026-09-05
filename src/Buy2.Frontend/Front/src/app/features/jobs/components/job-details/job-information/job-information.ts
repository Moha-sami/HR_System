import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { TaskModal } from '../../task-modal/task-modal';
import { JobService } from '../../../services/job.service';

@Component({
  selector: 'app-job-information',
  standalone: true,
  imports: [CommonModule, TaskModal],
  templateUrl: './job-information.html',
  styleUrl: './job-information.css',
})
export class JobInformation implements OnInit {
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private jobService = inject(JobService);

  showTaskModal = signal(false);

  jobDetails = {
    jobTitle: '',
    jobDescription: '',
    department: '',
    qualifications: '',
    seniorityLevel: '',
    reportingManager: 'N/A' // Not in API
  };

  jobSchedule = {
    scheduleType: 'Fixed',
    checkIn: '9 - 11 am',
    checkOut: '5 - 7 pm',
    hoursOfWork: '8'
  };

  performanceMetrics = [
    { id: 1, name: 'Deadlines', description: 'Punctuality score', measure: '30', target: '30', weight: '40' }
  ];

  fixedTasks = [
    { id: 1, name: 'Deadlines', description: 'Punctuality score' }
  ];

  editTask(taskId: number) {
    this.router.navigate(['/jobs/edit-task', 1, taskId]);
  }

  ngOnInit() {
    this.route.parent?.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.jobService.getJob(+id).subscribe({
          next: (job) => {
            this.jobDetails = {
              jobTitle: job.title || '',
              jobDescription: job.description || '',
              department: job.departmentName || '',
              qualifications: job.requiredQualifications?.join(', ') || '',
              seniorityLevel: job.seniorityLevel || '',
              reportingManager: 'N/A'
            };
            this.jobSchedule.scheduleType = job.workModel || '';
          },
          error: (err) => console.error('Error fetching job details', err)
        });
      }
    });
  }

  viewTask(taskId: number) {
    this.showTaskModal.set(true);
  }

  closeModal() {
    this.showTaskModal.set(false);
  }
}
