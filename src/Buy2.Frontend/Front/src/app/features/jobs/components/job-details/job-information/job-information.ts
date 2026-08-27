import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { TaskModal } from '../../task-modal/task-modal';

@Component({
  selector: 'app-job-information',
  standalone: true,
  imports: [CommonModule, TaskModal],
  templateUrl: './job-information.html',
  styleUrl: './job-information.css',
})
export class JobInformation {
  private router = inject(Router);

  showTaskModal = signal(false);

  jobDetails = {
    jobTitle: 'Mobile Developer',
    jobDescription: 'Lorem Ipsum is simply dummy text of the printing and Lorem Ipsum is simply dummy text of the printing and',
    department: 'Developement',
    qualifications: 'Management, POS',
    seniorityLevel: 'Senior',
    reportingManager: 'Nayef Fahd'
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

  viewTask(taskId: number) {
    this.showTaskModal.set(true);
  }

  closeModal() {
    this.showTaskModal.set(false);
  }
}
