import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { JobService } from '../../services/job.service';

@Component({
  selector: 'app-job-details',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './job-details.html',
  styleUrl: './job-details.css',
})
export class JobDetails implements OnInit {
  private route = inject(ActivatedRoute);
  private jobService = inject(JobService);

  jobTitle = '';
  totalPoints = 2500;
  totalTasks = 179;
  totalGifts = 39;

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.jobService.getJob(+id).subscribe({
          next: (job) => {
            this.jobTitle = job.title;
            // totalPoints, etc. are mock for now
          },
          error: (err) => console.error('Error fetching job details', err)
        });
      }
    });
  }
}
