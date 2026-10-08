import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FormsModule } from '@angular/forms';
import { RequestService } from '../../services/request.service';
import type { RequestHistoryItem, PaginatedResponse } from '../../models/request';

@Component({
  selector: 'app-requests-history',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslatePipe, FormsModule, DatePipe],
  templateUrl: './requests-history.component.html',
  styleUrls: ['./requests-history.component.css']
})
export class RequestsHistoryComponent implements OnInit {
  private requestService = inject(RequestService);
  private cdr = inject(ChangeDetectorRef);
  private router = inject(Router);

  historyData: PaginatedResponse<RequestHistoryItem> | null = null;

  currentPage = 1;
  pageSize = 12;
  Math = Math;
  showExportMenu = false;

  ngOnInit(): void {
    this.loadHistory();
    // Close export menu on outside click
    document.addEventListener('click', () => { this.showExportMenu = false; this.cdr.detectChanges(); });
  }

  loadHistory(): void {
    this.requestService.getRequestsHistory(this.currentPage, this.pageSize).subscribe({
      next: (data) => {
        this.historyData = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error loading requests history', err)
    });
  }

  get totalPages(): number {
    return this.historyData?.totalPages || 1;
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadHistory();
    }
  }

  onPageSizeChange(event: any): void {
    this.pageSize = event.target.value;
    this.currentPage = 1;
    this.loadHistory();
  }

  getPaginationPages(): number[] {
    const total = this.totalPages;
    const current = this.currentPage;
    if (total <= 7) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }
    const pages: number[] = [1];
    if (current > 3) pages.push(-1); // ellipsis
    for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) {
      pages.push(i);
    }
    if (current < total - 2) pages.push(-1); // ellipsis
    pages.push(total);
    return pages;
  }

  toggleExportMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.showExportMenu = !this.showExportMenu;
  }

  exportCSV(): void {
    this.requestService.exportRequestsHistory('csv').subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `requests_history_${new Date().toISOString().split('T')[0]}.csv`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => console.error('Error exporting CSV', err)
    });
  }

  exportExcel(): void {
    this.requestService.exportRequestsHistory('excel').subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `requests_history_${new Date().toISOString().split('T')[0]}.xlsx`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => console.error('Error exporting Excel', err)
    });
  }
}
