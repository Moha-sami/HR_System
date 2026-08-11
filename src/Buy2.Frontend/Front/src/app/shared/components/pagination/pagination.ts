import { Component, computed, input, output } from '@angular/core';
import { ButtonComponent } from '../button/button.component';

@Component({
  selector: 'app-pagination',
  imports: [ButtonComponent],
  templateUrl: './pagination.html',
  styleUrl: './pagination.css',
})
export class Pagination {
  currentPage = input<number>(1);
  totalPages = input<number>(1);
  pageSize = input<number>(10);

  pageChanged = output<number>();

  pages = computed(() =>
    Array.from({ length: this.totalPages() }, (_, index) => index + 1)
  );

  changePage(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    if (page === this.currentPage()) return;

    this.pageChanged.emit(page);
  }

  previousPage(): void {
    this.changePage(this.currentPage() - 1);
  }

  nextPage(): void {
    this.changePage(this.currentPage() + 1);
  }

}
