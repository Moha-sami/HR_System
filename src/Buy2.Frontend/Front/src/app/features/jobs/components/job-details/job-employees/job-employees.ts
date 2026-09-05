import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { JobService } from '../../../services/job.service';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-job-employees',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './job-employees.html',
  styleUrl: './job-employees.css',
})
export class JobEmployees implements OnInit {
  private route = inject(ActivatedRoute);
  private jobService = inject(JobService);

  employees: any[] = [];
  pageNumber = 1;
  pageSize = 10;
  totalCount = 0;

  ngOnInit() {
    this.route.parent?.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.loadEmployees(+id);
      }
    });
  }

  loadEmployees(id: number) {
    this.jobService.getJobEmployees(id, this.pageNumber, this.pageSize).subscribe({
      next: (res) => {
        this.employees = res.items.map((e: any) => ({
          id: e.employeeCode !== 'N/A' ? e.employeeCode : e.id.toString(),
          name: e.fullName,
          email: e.email,
          joinDate: e.joinDate.split('T')[0]
        }));
        this.totalCount = res.totalCount;
      },
      error: (err) => console.error('Failed to load employees', err)
    });
  }
}
