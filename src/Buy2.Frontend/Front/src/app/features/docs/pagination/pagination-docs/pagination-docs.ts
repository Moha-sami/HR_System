import { Pagination } from '../../../../shared/components/pagination/pagination';
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-pagination-docs',
  imports: [Pagination, RouterLink],
  templateUrl: './pagination-docs.html',
  styleUrl: './pagination-docs.css',
})
export class PaginationDocs {
  totalPages = 12;
}
