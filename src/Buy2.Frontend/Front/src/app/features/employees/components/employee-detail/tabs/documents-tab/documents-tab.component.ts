import {
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonComponent } from '@app/shared/components/button/button.component';
import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { EmployeeDetailService } from '../../../../services/employee-detail.service';
import {
  DOCUMENT_CATEGORIES,
  type DocumentCategory,
  type EmployeeDocumentDto,
} from '../../../../models/view-employee/employee-documents';

@Component({
  selector: 'app-documents-tab',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe, ButtonComponent, ModalComponent],
  templateUrl: './documents-tab.component.html',
  styleUrl: './documents-tab.component.css',
})
export class DocumentsTabComponent {
  private readonly employeeDetailService = inject(EmployeeDetailService);

  readonly employee = this.employeeDetailService.detailEmployee;
  readonly documents = this.employeeDetailService.documents;
  readonly documentsLoading = this.employeeDetailService.documentsLoading;
  readonly documentsError = this.employeeDetailService.documentsError;

  // Filter & Search state
  readonly categories = DOCUMENT_CATEGORIES;
  readonly selectedCategory = signal<DocumentCategory>('All');
  readonly searchQuery = signal<string>('');
  readonly lastLoadedEmployeeId = signal<number | null>(null);

  // Upload modal state
  readonly showUploadModal = signal(false);
  readonly uploadCategory = signal<string>('Identification');
  readonly uploadSourceType = signal<'file' | 'url'>('file');
  readonly uploadUrl = signal<string>('');
  readonly selectedFile = signal<File | null>(null);
  readonly uploadError = signal<string | null>(null);
  readonly uploading = signal(false);

  // Delete modal state
  readonly showDeleteModal = signal(false);
  readonly documentToDelete = signal<EmployeeDocumentDto | null>(null);
  readonly deleteError = signal<string | null>(null);
  readonly deleting = signal(false);

  // Toast / feedback message
  readonly feedbackMessage = signal<{ type: 'success' | 'error'; key: string } | null>(null);

  // Computed filtered documents
  readonly filteredDocuments = computed(() => {
    const docs = this.documents();
    const cat = this.selectedCategory();
    const query = this.searchQuery().trim().toLowerCase();

    return docs.filter((doc) => {
      const matchesCat =
        cat === 'All' || doc.category.toLowerCase() === cat.toLowerCase();
      if (!matchesCat) return false;

      if (!query) return true;

      const fileName = this.getFileName(doc.storageUrl, doc.id, doc.category).toLowerCase();
      const storageUrl = doc.storageUrl.toLowerCase();
      const category = doc.category.toLowerCase();

      return (
        fileName.includes(query) ||
        storageUrl.includes(query) ||
        category.includes(query)
      );
    });
  });

  // Computed category counts
  readonly categoryCounts = computed(() => {
    const docs = this.documents();
    const counts: Record<string, number> = { All: docs.length };

    for (const cat of this.categories) {
      if (cat !== 'All') {
        counts[cat] = docs.filter(
          (d) => d.category.toLowerCase() === cat.toLowerCase(),
        ).length;
      }
    }

    return counts;
  });

  constructor() {
    // Reactive load on employee change
    effect(() => {
      const emp = this.employee();
      if (emp && emp.id !== this.lastLoadedEmployeeId()) {
        this.lastLoadedEmployeeId.set(emp.id);
        this.loadDocuments();
      }
    });
  }

  loadDocuments(): void {
    const emp = this.employee();
    if (emp) {
      this.employeeDetailService.loadEmployeeDocuments(emp.id);
    }
  }

  setCategory(cat: DocumentCategory): void {
    this.selectedCategory.set(cat);
  }

  // Upload modal handlers
  openUploadModal(): void {
    this.uploadCategory.set('Identification');
    this.uploadSourceType.set('file');
    this.uploadUrl.set('');
    this.selectedFile.set(null);
    this.uploadError.set(null);
    this.uploading.set(false);
    this.showUploadModal.set(true);
  }

  closeUploadModal(): void {
    if (this.uploading()) return;
    this.showUploadModal.set(false);
    this.uploadError.set(null);
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      const maxBytes = 10 * 1024 * 1024; // 10MB
      if (file.size > maxBytes) {
        this.uploadError.set('EMPLOYEE_DETAIL.DOCUMENTS.UPLOAD_MODAL.FILE_HINT');
        input.value = '';
        return;
      }
      this.selectedFile.set(file);
      this.uploadError.set(null);
    }
  }

  removeSelectedFile(): void {
    this.selectedFile.set(null);
  }

  submitUpload(): void {
    const emp = this.employee();
    if (!emp || this.uploading()) return;

    const category = this.uploadCategory().trim();
    if (!category) {
      this.uploadError.set('EMPLOYEE_DETAIL.DOCUMENTS.UPLOAD_MODAL.ERRORS.CATEGORY_REQUIRED');
      return;
    }

    let fileOrUrl = '';

    if (this.uploadSourceType() === 'url') {
      const url = this.uploadUrl().trim();
      if (!url) {
        this.uploadError.set('EMPLOYEE_DETAIL.DOCUMENTS.UPLOAD_MODAL.ERRORS.URL_REQUIRED');
        return;
      }
      if (!/^https?:\/\//i.test(url)) {
        this.uploadError.set('EMPLOYEE_DETAIL.DOCUMENTS.UPLOAD_MODAL.ERRORS.URL_INVALID');
        return;
      }
      fileOrUrl = url;
    } else {
      const file = this.selectedFile();
      if (!file) {
        this.uploadError.set('EMPLOYEE_DETAIL.DOCUMENTS.UPLOAD_MODAL.ERRORS.FILE_REQUIRED');
        return;
      }
      // For local uploaded files, build a clean persistent storage URL referencing the uploaded asset
      fileOrUrl = `https://storage.buy2hrms.com/documents/employees/${emp.id}/${encodeURIComponent(file.name)}`;
    }

    this.uploading.set(true);
    this.uploadError.set(null);

    this.employeeDetailService.uploadEmployeeDocument(emp.id, category, fileOrUrl).subscribe({
      next: () => {
        this.uploading.set(false);
        this.showUploadModal.set(false);
        this.loadDocuments();
        this.showFeedback('success', 'EMPLOYEE_DETAIL.DOCUMENTS.MESSAGES.UPLOAD_SUCCESS');
      },
      error: () => {
        this.uploading.set(false);
        this.uploadError.set('EMPLOYEE_DETAIL.DOCUMENTS.MESSAGES.UPLOAD_ERROR');
      },
    });
  }

  // Delete modal handlers
  openDeleteModal(doc: EmployeeDocumentDto): void {
    this.documentToDelete.set(doc);
    this.deleteError.set(null);
    this.deleting.set(false);
    this.showDeleteModal.set(true);
  }

  closeDeleteModal(): void {
    if (this.deleting()) return;
    this.showDeleteModal.set(false);
    this.documentToDelete.set(null);
    this.deleteError.set(null);
  }

  confirmDelete(): void {
    const emp = this.employee();
    const doc = this.documentToDelete();
    if (!emp || !doc || this.deleting()) return;

    this.deleting.set(true);
    this.deleteError.set(null);

    this.employeeDetailService.deleteEmployeeDocument(emp.id, doc.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.showDeleteModal.set(false);
        this.documentToDelete.set(null);
        this.loadDocuments();
        this.showFeedback('success', 'EMPLOYEE_DETAIL.DOCUMENTS.MESSAGES.DELETE_SUCCESS');
      },
      error: () => {
        this.deleting.set(false);
        this.deleteError.set('EMPLOYEE_DETAIL.DOCUMENTS.MESSAGES.DELETE_ERROR');
      },
    });
  }

  private showFeedback(type: 'success' | 'error', key: string): void {
    this.feedbackMessage.set({ type, key });
    setTimeout(() => {
      this.feedbackMessage.set(null);
    }, 4000);
  }

  // Helper formatting methods
  getFileName(url: string, id: number, category: string): string {
    if (!url) return `${category}_#${id}`;
    try {
      const cleanUrl = url.split('?')[0].split('#')[0];
      const parts = cleanUrl.split('/');
      const lastPart = parts[parts.length - 1];
      if (lastPart && lastPart.trim().length > 0) {
        return decodeURIComponent(lastPart);
      }
    } catch {
      // fallback
    }
    return `${category}_#${id}`;
  }

  getFileExtension(url: string): string {
    if (!url) return '';
    try {
      const cleanUrl = url.split('?')[0].split('#')[0];
      const match = cleanUrl.match(/\.([0-9a-z]+)$/i);
      return match ? match[1].toLowerCase() : '';
    } catch {
      return '';
    }
  }

  getFileType(url: string): 'pdf' | 'word' | 'excel' | 'image' | 'archive' | 'other' {
    const ext = this.getFileExtension(url);
    if (ext === 'pdf') return 'pdf';
    if (['doc', 'docx'].includes(ext)) return 'word';
    if (['xls', 'xlsx', 'csv'].includes(ext)) return 'excel';
    if (['png', 'jpg', 'jpeg', 'webp', 'svg', 'gif'].includes(ext)) return 'image';
    if (['zip', 'rar', 'tar', 'gz', '7z'].includes(ext)) return 'archive';
    return 'other';
  }

  getCategoryBadgeClass(category: string): string {
    switch (category.toLowerCase()) {
      case 'contracts':
      case 'contract':
        return 'bg-blue-50 text-blue-700 border-blue-200';
      case 'identification':
        return 'bg-purple-50 text-purple-700 border-purple-200';
      case 'certificates':
      case 'certificate':
        return 'bg-emerald-50 text-emerald-700 border-emerald-200';
      case 'medical':
        return 'bg-rose-50 text-rose-700 border-rose-200';
      default:
        return 'bg-amber-50 text-amber-700 border-amber-200';
    }
  }
}
