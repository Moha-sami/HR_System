import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { DocumentsTabComponent } from './documents-tab.component';
import { EmployeeDetailService } from '../../../../services/employee-detail.service';
import { TranslatePipe } from '@ngx-translate/core';
import { Pipe, type PipeTransform, signal } from '@angular/core';
import type { EmployeeDocumentDto } from '../../../../models/view-employee/employee-documents';

@Pipe({ name: 'translate', standalone: true })
class MockTranslatePipe implements PipeTransform {
  transform(value: string): string {
    return value;
  }
}

describe('DocumentsTabComponent', () => {
  let component: DocumentsTabComponent;
  let fixture: ComponentFixture<DocumentsTabComponent>;
  let mockEmployeeDetailService: {
    detailEmployee: ReturnType<typeof signal<any>>;
    documents: ReturnType<typeof signal<readonly EmployeeDocumentDto[]>>;
    documentsLoading: ReturnType<typeof signal<boolean>>;
    documentsError: ReturnType<typeof signal<string | null>>;
    loadEmployeeDocuments: ReturnType<typeof vi.fn>;
    uploadEmployeeDocument: ReturnType<typeof vi.fn>;
    deleteEmployeeDocument: ReturnType<typeof vi.fn>;
  };

  const mockEmployee = {
    id: 1,
    employeeCode: 'EMP-0001',
    fullName: 'John Doe',
  };

  const mockDocuments: EmployeeDocumentDto[] = [
    {
      id: 101,
      employeeId: 1,
      category: 'Identification',
      storageUrl: 'https://storage.buy2hrms.com/docs/national_id.pdf',
      createdAt: '2026-01-15T10:00:00Z',
    },
    {
      id: 102,
      employeeId: 1,
      category: 'Contracts',
      storageUrl: 'https://storage.buy2hrms.com/docs/employment_contract.docx',
      createdAt: '2026-02-01T12:00:00Z',
    },
    {
      id: 103,
      employeeId: 1,
      category: 'Certificates',
      storageUrl: 'https://storage.buy2hrms.com/docs/csharp_cert.png',
      createdAt: '2026-03-01T15:00:00Z',
    },
  ];

  beforeEach(async () => {
    mockEmployeeDetailService = {
      detailEmployee: signal(mockEmployee),
      documents: signal(mockDocuments),
      documentsLoading: signal(false),
      documentsError: signal(null),
      loadEmployeeDocuments: vi.fn(),
      uploadEmployeeDocument: vi.fn(() => of(104)),
      deleteEmployeeDocument: vi.fn(() => of(undefined)),
    };

    await TestBed.configureTestingModule({
      imports: [DocumentsTabComponent],
      providers: [
        { provide: EmployeeDetailService, useValue: mockEmployeeDetailService },
      ],
    })
      .overrideComponent(DocumentsTabComponent, {
        remove: { imports: [TranslatePipe] },
        add: { imports: [MockTranslatePipe] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(DocumentsTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should list all documents by default', () => {
    expect(component.selectedCategory()).toBe('All');
    expect(component.filteredDocuments().length).toBe(3);
  });

  it('should filter documents by category', () => {
    component.setCategory('Contracts');
    expect(component.selectedCategory()).toBe('Contracts');
    expect(component.filteredDocuments().length).toBe(1);
    expect(component.filteredDocuments()[0].category).toBe('Contracts');
  });

  it('should filter documents by search query', () => {
    component.searchQuery.set('csharp');
    expect(component.filteredDocuments().length).toBe(1);
    expect(component.filteredDocuments()[0].id).toBe(103);
  });

  it('should open and close upload modal', () => {
    component.openUploadModal();
    expect(component.showUploadModal()).toBe(true);

    component.closeUploadModal();
    expect(component.showUploadModal()).toBe(false);
  });

  it('should submit upload when valid url is provided', () => {
    component.openUploadModal();
    component.uploadSourceType.set('url');
    component.uploadCategory.set('Medical');
    component.uploadUrl.set('https://storage.buy2hrms.com/docs/checkup.pdf');

    component.submitUpload();

    expect(mockEmployeeDetailService.uploadEmployeeDocument).toHaveBeenCalledWith(
      1,
      'Medical',
      'https://storage.buy2hrms.com/docs/checkup.pdf',
    );
    expect(component.showUploadModal()).toBe(false);
    expect(mockEmployeeDetailService.loadEmployeeDocuments).toHaveBeenCalledWith(1);
  });

  it('should validate empty category and url in upload modal', () => {
    component.openUploadModal();
    component.uploadCategory.set('');
    component.submitUpload();
    expect(component.uploadError()).toBeTruthy();

    component.uploadCategory.set('Other');
    component.uploadSourceType.set('url');
    component.uploadUrl.set('not-a-valid-url');
    component.submitUpload();
    expect(component.uploadError()).toBeTruthy();
  });

  it('should open and close delete modal', () => {
    const doc = mockDocuments[0];
    component.openDeleteModal(doc);
    expect(component.showDeleteModal()).toBe(true);
    expect(component.documentToDelete()).toEqual(doc);

    component.closeDeleteModal();
    expect(component.showDeleteModal()).toBe(false);
    expect(component.documentToDelete()).toBeNull();
  });

  it('should confirm delete and call service', () => {
    const doc = mockDocuments[0];
    component.openDeleteModal(doc);
    component.confirmDelete();

    expect(mockEmployeeDetailService.deleteEmployeeDocument).toHaveBeenCalledWith(1, doc.id);
    expect(component.showDeleteModal()).toBe(false);
    expect(mockEmployeeDetailService.loadEmployeeDocuments).toHaveBeenCalledWith(1);
  });

  it('should identify file types correctly', () => {
    expect(component.getFileType('test.pdf')).toBe('pdf');
    expect(component.getFileType('test.docx')).toBe('word');
    expect(component.getFileType('test.xlsx')).toBe('excel');
    expect(component.getFileType('test.png')).toBe('image');
    expect(component.getFileType('test.zip')).toBe('archive');
    expect(component.getFileType('test.xyz')).toBe('other');
  });
});
