import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { JobService } from '../../../services/job.service';
import { ActivatedRoute } from '@angular/router';

import { Pagination } from '../../../../../shared/components/pagination/pagination';

@Component({
  selector: 'app-job-employees',
  standalone: true,
  imports: [CommonModule, Pagination],
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

  totalPages = 0;

  ngOnInit() {
    this.route.parent?.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.jobId = +id;
        this.loadEmployees();
      }
    });
  }

  jobId: number = 0;

  loadEmployees() {
    this.jobService.getJobEmployees(this.jobId, this.pageNumber, this.pageSize).subscribe({
      next: (res) => {
        this.employees = res.items.map((e: any) => ({
          id: e.employeeCode && e.employeeCode !== 'N/A' ? e.employeeCode : (e.id ? e.id.toString() : ''),
          name: e.employeeName || e.fullName || 'N/A',
          email: e.email || 'N/A',
          joinDate: e.joinDate ? e.joinDate.split('T')[0] : 'N/A'
        }));
        this.totalCount = res.totalCount || 0;
        this.totalPages = res.totalPages || 0;
      },
      error: (err) => console.error('Failed to load employees', err)
    });
  }

  onPageChanged(page: number) {
    this.pageNumber = page;
    this.loadEmployees();
  }
}
