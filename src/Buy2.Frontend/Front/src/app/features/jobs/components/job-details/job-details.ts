import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-job-details',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './job-details.html',
  styleUrl: './job-details.css',
})
export class JobDetails {
  jobTitle = 'UX / UX Designer';
  totalPoints = 2500;
  totalTasks = 179;
  totalGifts = 39;
}
