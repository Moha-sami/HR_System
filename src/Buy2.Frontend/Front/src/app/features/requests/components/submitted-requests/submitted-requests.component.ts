import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FormsModule } from '@angular/forms';
import { RequestService } from '../../services/request.service';
import type { SubmittedRequest, PaginatedResponse } from '../../models/request';

@Component({
  selector: 'app-submitted-requests',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslatePipe, FormsModule, DatePipe],
  templateUrl: './submitted-requests.component.html',
  styleUrls: ['./submitted-requests.component.css']
})
export class SubmittedRequestsComponent implements OnInit {
  private requestService = inject(RequestService);
  private cdr = inject(ChangeDetectorRef);
  private router = inject(Router);

  requestsData: PaginatedResponse<SubmittedRequest> | null = null;
  
  currentPage = 1;
  pageSize = 12;
  Math = Math;

  ngOnInit(): void {
    this.loadRequests();
  }

  loadRequests(): void {
    this.requestService.getSubmittedRequests(this.currentPage, this.pageSize).subscribe({
      next: (data) => {
        this.requestsData = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error loading submitted requests', err)
    });
  }

  get totalPages(): number {
    return this.requestsData?.totalPages || 1;
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadRequests();
    }
  }

  onPageSizeChange(event: any): void {
    this.pageSize = event.target.value;
    this.currentPage = 1;
    this.loadRequests();
  }

  getPaginationPages(): number[] {
    const total = this.totalPages;
    const current = this.currentPage;
    if (total <= 7) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }
    const pages: number[] = [1];
    if (current > 3) pages.push(-1);
    for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) {
      pages.push(i);
    }
    if (current < total - 2) pages.push(-1);
    pages.push(total);
    return pages;
  }

  viewDecision(id: number): void {
    this.router.navigate(['/requests/decision', id]);
  }
}
