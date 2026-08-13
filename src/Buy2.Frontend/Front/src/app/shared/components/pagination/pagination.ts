import { Component, computed, effect, input, output, signal, type OnChanges, type SimpleChanges } from '@angular/core';
import { ButtonComponent } from '../button/button.component';

type PageItem = number | 'ellipsis';

@Component({
  selector: 'app-pagination',
  imports: [ButtonComponent],
  templateUrl: './pagination.html',
  styleUrl: './pagination.css',
})
export class Pagination implements OnChanges {
  totalPages = input<number>(1);
  initialPage = input<number>(1);
  pageChanged = output<number>();

  currentPage = signal(1);

  readonly validTotalPages = computed(() => {
    const totalPages = Math.floor(this.totalPages());
    return Number.isFinite(totalPages) && totalPages > 0 ? totalPages : 1;
  });

  readonly pages = computed<PageItem[]>(() => {
    const totalPages = this.validTotalPages();
    const currentPage = this.currentPage();

    if (totalPages <= 7) {
      return Array.from({ length: totalPages }, (_, index) => index + 1);
    }

    const pages: PageItem[] = [1];
    const startPage = Math.max(2, currentPage - 1);
    const endPage = Math.min(totalPages - 1, currentPage + 1);

    if (startPage > 2) pages.push('ellipsis');
    for (let page = startPage; page <= endPage; page++) pages.push(page);
    if (endPage < totalPages - 1) pages.push('ellipsis');

    pages.push(totalPages);
    return pages;
  });

  constructor() {
    effect(() => {
      const maxPage = this.validTotalPages();
      if (this.currentPage() > maxPage) this.currentPage.set(maxPage);
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['initialPage']?.firstChange) {
      this.currentPage.set(this.clampPage(this.initialPage()));
    }
  }

  changePage(page: number): void {
    if (page < 1 || page > this.validTotalPages()) return;
    if (page === this.currentPage()) return;

    this.currentPage.set(page);
    this.pageChanged.emit(page);
  }

  previousPage(): void {
    this.changePage(this.currentPage() - 1);
  }

  nextPage(): void {
    this.changePage(this.currentPage() + 1);
  }

  private clampPage(page: number): number {
    const normalizedPage = Number.isFinite(page) ? Math.floor(page) : 1;
    return Math.min(Math.max(normalizedPage, 1), this.validTotalPages());
  }

}
