import { Pagination } from '../../../../shared/components/pagination/pagination';
import { Component, signal } from '@angular/core';

@Component({
  selector: 'app-pagination-docs',
  imports: [Pagination],
  templateUrl: './pagination-docs.html',
  styleUrl: './pagination-docs.css',
})
export class PaginationDocs {
  currentPage = signal(1);
  totalPages = signal(10);

  onPageChange(page: number): void {
  this.currentPage.set(page);
  }
}
