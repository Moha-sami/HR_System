import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA, Pipe, type PipeTransform } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { Router, ActivatedRoute } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { ShiftTemplateListComponent } from './shift-template-list.component';
import type {
  ShiftTemplateListItem,
  ShiftTemplateListResponse,
} from '../../data-access/models/shift-template.models';

@Pipe({ name: 'translate', standalone: true })
class MockTranslatePipe implements PipeTransform {
  transform(value: string): string {
    return value;
  }
}

const FIRST_PAGE: ShiftTemplateListResponse = {
  items: [
    {
      id: 5,
      name: 'Morning Shift',
      creationDate: '2020-03-09',
      lastUpdated: '2024-07-15',
      numberOfAssignedSites: 5,
    },
    {
      id: 7,
      name: 'Evening Shift',
      creationDate: '2020-06-18',
      lastUpdated: '2023-01-12',
      numberOfAssignedSites: 4,
    },
  ],
  totalCount: 12,
  pageNumber: 1,
  pageSize: 10,
  totalPages: 2,
};

/** Ticket #326: server-side list — search/sort/paginate, edit/duplicate/delete, visual-only export. */
describe('ShiftTemplateListComponent', () => {
  let fixture: ComponentFixture<ShiftTemplateListComponent>;
  let component: ShiftTemplateListComponent;
  let httpMock: HttpTestingController;
  let navigated: unknown[][];

  async function setup(): Promise<void> {
    navigated = [];
    await TestBed.configureTestingModule({
      imports: [ShiftTemplateListComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Router, useValue: { navigate: (c: unknown[]) => { navigated.push(c); return Promise.resolve(true); } } },
        { provide: ActivatedRoute, useValue: { snapshot: { params: {}, queryParams: {} } } },
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
      ],
    })
      .overrideComponent(ShiftTemplateListComponent, {
        remove: { imports: [TranslatePipe] },
        add: { imports: [MockTranslatePipe] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(ShiftTemplateListComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  }

  function flushList(page = 1): void {
    const req = httpMock.expectOne((r) => r.url.endsWith('/shift-templates') && r.params.get('pageNumber') === String(page));
    expect(req.request.method).toBe('GET');
    req.flush({ ...FIRST_PAGE, pageNumber: page });
    fixture.detectChanges();
  }

  afterEach(() => httpMock.verify());

  it('should load the first page from the server on init', async () => {
    await setup();
    const req = httpMock.expectOne((r) => r.url.endsWith('/shift-templates'));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('pageNumber')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('10');
    expect(req.request.params.has('searchTerm')).toBe(false);
    req.flush(FIRST_PAGE);
    fixture.detectChanges();

    expect(component.templates().length).toBe(2);
    expect(component.totalCount()).toBe(12);
    expect(component.totalPages()).toBe(2);
  });

  it('should send the search term server-side after debounce and reset to page one', async () => {
    await setup();
    flushList();

    component.onPageChanged(2);
    flushList(2);
    expect(component.currentPage()).toBe(2);

    component.onSearch({ target: { value: 'morn' } } as unknown as Event);
    await new Promise((resolve) => setTimeout(resolve, 350));

    const req = httpMock.expectOne((r) => r.url.endsWith('/shift-templates'));
    expect(req.request.params.get('searchTerm')).toBe('morn');
    expect(req.request.params.get('pageNumber')).toBe('1');
    req.flush({ ...FIRST_PAGE, items: [FIRST_PAGE.items[0]], totalCount: 1 });
    fixture.detectChanges();

    expect(component.currentPage()).toBe(1);
    expect(component.templates().length).toBe(1);
  });

  it('should sort server-side by creation date', async () => {
    await setup();
    flushList();

    component.onSort({ column: 'creationDate', direction: 'desc' });

    const req = httpMock.expectOne((r) => r.url.endsWith('/shift-templates'));
    expect(req.request.params.get('creationSort')).toBe('desc');
    expect(req.request.params.get('pageNumber')).toBe('1');
    expect(req.request.params.has('updatedSort')).toBe(false);
    req.flush(FIRST_PAGE);
    fixture.detectChanges();
  });

  it('should ignore sort events from unknown columns', async () => {
    await setup();
    flushList();

    component.onSort({ column: 'actions', direction: 'asc' });
    httpMock.expectNone((r) => r.url.endsWith('/shift-templates'));
  });

  it('should request the selected page on pagination', async () => {
    await setup();
    flushList();

    component.onPageChanged(2);
    flushList(2);
    expect(component.currentPage()).toBe(2);
  });

  it('should navigate to the editor route on edit', async () => {
    await setup();
    flushList();

    component.editTemplate(FIRST_PAGE.items[1]);
    expect(navigated).toEqual([['/scheduling/shift-templates', 7, 'edit']]);
  });

  it('should navigate to the new route on create', async () => {
    await setup();
    flushList();

    component.navigateToCreate();
    expect(navigated).toEqual([['/scheduling/shift-templates/new']]);
  });

  it('should duplicate then refresh with a success modal', async () => {
    await setup();
    flushList();

    const item: ShiftTemplateListItem = FIRST_PAGE.items[0];
    component.duplicateTemplate(item);

    const dup = httpMock.expectOne((r) => r.url.endsWith('/shift-templates/5/duplicate'));
    expect(dup.request.method).toBe('POST');
    dup.flush({ id: 9, name: 'Morning Shift (copy)' });
    fixture.detectChanges();

    expect(component.showSuccessModal()).toBe(true);

    component.confirmSuccess();
    expect(component.showSuccessModal()).toBe(false);
    flushList(1);
  });

  it('should show the action error banner when duplication fails', async () => {
    await setup();
    flushList();

    component.duplicateTemplate(FIRST_PAGE.items[0]);
    httpMock.expectOne((r) => r.url.endsWith('/shift-templates/5/duplicate')).error(new ProgressEvent('error'));
    fixture.detectChanges();

    expect(component.duplicatingId()).toBeNull();
    expect(component.actionError()).toBe('SHIFT_TEMPLATES.DUPLICATE_ERROR');
    expect(
      (fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent,
    ).toContain('SHIFT_TEMPLATES.DUPLICATE_ERROR');
  });

  it('should delete after confirm with a success modal and refresh', async () => {
    await setup();
    flushList();

    component.openDeleteModal(FIRST_PAGE.items[0]);
    expect(component.showDeleteModal()).toBe(true);

    component.confirmDelete();
    const del = httpMock.expectOne((r) => r.url.endsWith('/shift-templates/5'));
    expect(del.request.method).toBe('DELETE');
    del.flush(null);
    fixture.detectChanges();

    expect(component.showDeleteModal()).toBe(false);
    expect(component.showSuccessModal()).toBe(true);

    component.confirmSuccess();
    flushList(1);
    expect(component.showSuccessModal()).toBe(false);
  });

  it('should render the export button with no server action', async () => {
    await setup();
    flushList();

    const buttons = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('button'),
    );
    const exportBtn = buttons.find((b) => b.textContent?.includes('SHIFT_TEMPLATES.EXPORT'));
    expect(exportBtn).toBeTruthy();

    exportBtn!.click();
    fixture.detectChanges();
    httpMock.expectNone(() => true);
  });
});
