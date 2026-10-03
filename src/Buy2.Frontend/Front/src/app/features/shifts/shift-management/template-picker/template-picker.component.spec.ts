import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA, Pipe, type PipeTransform } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { TemplatePickerComponent } from './template-picker.component';

@Pipe({ name: 'translate', standalone: true })
class MockTranslatePipe implements PipeTransform {
  transform(value: string): string {
    return value;
  }
}

const TEMPLATES = [
  { id: 1, name: 'Morning', creationDate: '2026-01-01', lastUpdated: '2026-01-02', numberOfAssignedSites: 2 },
  { id: 2, name: 'Evening', creationDate: '2026-01-01', lastUpdated: '2026-01-02', numberOfAssignedSites: 0 },
];

/** Ticket C: picker lists templates and applies with a Keep/Replace choice. */
describe('TemplatePickerComponent', () => {
  let fixture: ComponentFixture<TemplatePickerComponent>;
  let component: TemplatePickerComponent;
  let httpMock: HttpTestingController;

  async function setup(): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [TemplatePickerComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
      ],
    })
      .overrideComponent(TemplatePickerComponent, {
        remove: { imports: [TranslatePipe] },
        add: { imports: [MockTranslatePipe] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(TemplatePickerComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.componentRef.setInput('siteId', 3);
    fixture.componentRef.setInput('date', '2026-10-02');
    fixture.detectChanges();
  }

  function flushTemplates() {
    const req = httpMock.expectOne((r) => r.url.endsWith('/shift-templates'));
    expect(req.request.params.get('pageNumber')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('100');
    req.flush({ items: TEMPLATES, totalCount: 2, pageNumber: 1, pageSize: 100, totalPages: 1 });
    fixture.detectChanges();
  }

  afterEach(() => httpMock.verify());

  it('should load templates on init and default to keep existing', async () => {
    await setup();
    flushTemplates();

    expect(component.templates().length).toBe(2);
    expect(component.keepMode()).toBe('existing');
    expect(fixture.nativeElement.textContent).toContain('Morning');
  });

  it('should reload with the search term after debounce', async () => {
    await setup();
    flushTemplates();

    component.onSearchInput('Even');
    await new Promise((r) => setTimeout(r, 400));
    fixture.detectChanges();

    const req = httpMock.expectOne((r) => r.url.endsWith('/shift-templates'));
    expect(req.request.params.get('searchTerm')).toBe('Even');
    req.flush({ items: [TEMPLATES[1]], totalCount: 1, pageNumber: 1, pageSize: 100, totalPages: 1 });
  });

  it('should require a selection before applying', async () => {
    await setup();
    flushTemplates();

    component.apply();

    httpMock.expectNone((r) => r.url.includes('/apply-template'));
    expect(component.applyError()).toBe('SHIFT_MANAGEMENT.PICKER.TEMPLATE_REQUIRED');
  });

  it('should apply with template id and keep mode then emit applied', async () => {
    await setup();
    flushTemplates();
    let applied = 0;
    component.applied.subscribe(() => applied++);

    component.selectTemplate(2);
    component.setKeepMode('new');
    component.apply();

    const req = httpMock.expectOne((r) => r.url.endsWith('/sites/3/dates/2026-10-02/apply-template'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ templateId: 2, keep: 'new' });
    req.flush({
      siteId: 3,
      date: '2026-10-02',
      templateId: 2,
      warnings: [],
      totalLaborCost: 400,
      regularCost: 400,
      overtimeCost: 0,
      prunedCount: 0,
    });

    expect(applied).toBe(1);
    expect(component.applyError()).toBeNull();
  });

  it('should show backend conflicts inline without emitting applied', async () => {
    await setup();
    flushTemplates();
    let applied = 0;
    component.applied.subscribe(() => applied++);

    component.selectTemplate(1);
    component.apply();

    const req = httpMock.expectOne((r) => r.url.includes('/apply-template'));
    req.flush({ message: 'Template has unpublished blocks.' }, { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();

    expect(component.applyError()).toBe('Template has unpublished blocks.');
    expect(applied).toBe(0);
    expect(fixture.nativeElement.textContent).toContain('Template has unpublished blocks.');
  });

  it('should show success warnings with a Done action that emits applied', async () => {
    await setup();
    flushTemplates();
    let applied = 0;
    component.applied.subscribe(() => applied++);

    component.selectTemplate(1);
    component.apply();

    httpMock.expectOne((r) => r.url.includes('/apply-template')).flush({
      siteId: 3,
      date: '2026-10-02',
      templateId: 1,
      warnings: [
        { employeeId: 100, employeeName: 'Sara', role: 'Cashier', reason: 'Stripped: overtime', code: 'OVERTIME' },
      ],
      totalLaborCost: 400,
      regularCost: 350,
      overtimeCost: 50,
      prunedCount: 1,
    });
    fixture.detectChanges();

    expect(component.resultWarnings()?.length).toBe(1);
    expect(applied).toBe(0);

    component.doneAfterWarnings();
    expect(applied).toBe(1);
  });
});
