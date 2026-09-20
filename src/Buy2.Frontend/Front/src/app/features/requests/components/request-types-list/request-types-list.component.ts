import { Component, inject, type OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FormsModule } from '@angular/forms';
import { RequestTypeService } from '../../services/request-type.service';
import type { RequestType } from '../../models/request-type.model';

@Component({
  selector: 'app-request-types-list',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslatePipe, FormsModule],
  templateUrl: './request-types-list.component.html',
  styleUrls: ['./request-types-list.component.css']
})
export class RequestTypesListComponent implements OnInit {
  private requestTypeService = inject(RequestTypeService);
  private router = inject(Router);

  requestTypes: RequestType[] = [];
  filteredTypes: RequestType[] = [];
  searchTerm = '';

  // Delete Modals State
  showDeleteConfirmModal = false;
  showDeleteSuccessModal = false;
  requestToDeleteId: string | null = null;

  // Pagination
  currentPage = 1;
  pageSize = 10;
  Math = Math;

  ngOnInit(): void {
    this.loadRequestTypes();
  }

  loadRequestTypes(): void {
    this.requestTypeService.getRequestTypes().subscribe({
      next: (data) => {
        this.requestTypes = data;
        this.applyFilter();
      },
      error: (err) => console.error('Error loading request types', err)
    });
  }

  applyFilter(): void {
    if (this.searchTerm) {
      const term = this.searchTerm.toLowerCase();
      this.filteredTypes = this.requestTypes.filter(rt => 
        rt.name.toLowerCase().includes(term) || rt.category.toLowerCase().includes(term)
      );
    } else {
      this.filteredTypes = [...this.requestTypes];
    }
    this.currentPage = 1;
  }

  deleteRequestType(id: string): void {
    this.requestToDeleteId = id;
    this.showDeleteConfirmModal = true;
  }

  confirmDelete(): void {
    if (this.requestToDeleteId) {
      this.requestTypeService.deleteRequestType(this.requestToDeleteId).subscribe({
        next: () => {
          this.showDeleteConfirmModal = false;
          this.showDeleteSuccessModal = true;
          this.loadRequestTypes();
        },
        error: (err) => console.error('Error deleting request type', err)
      });
    }
  }

  cancelDelete(): void {
    this.showDeleteConfirmModal = false;
    this.requestToDeleteId = null;
  }

  closeDeleteSuccess(): void {
    this.showDeleteSuccessModal = false;
    this.requestToDeleteId = null;
  }

  get paginatedTypes(): RequestType[] {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    return this.filteredTypes.slice(startIndex, startIndex + this.pageSize);
  }

  get totalPages(): number {
    return Math.ceil(this.filteredTypes.length / this.pageSize) || 1;
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
    }
  }

  editRequestType(id: string): void {
    this.router.navigate(['/requests/types/edit', id]);
  }
}
