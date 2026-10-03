import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA, Pipe, type PipeTransform } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ShiftOverviewComponent } from './shift-overview.component';

@Pipe({ name: 'translate', standalone: true })
class MockTranslatePipe implements PipeTransform {
  transform(value: string): string {
    return value;
  }
}

const SITES = [
  {
    siteId: 1,
    siteName: 'Cairo HQ',
    address: 'Cairo',
    regionId: 2,
    regionName: 'Cairo',
    totalShifts: 3,
    openShifts: 1,
    filledShifts: 2,
    status: 'Shortage',
    isSmartAssignmentEnabled: true,
    isSmartPostingEnabled: false,
  },
];

/** Ticket A: site-coverage overview. */
describe('ShiftOverviewComponent', () => {
  let fixture: ComponentFixture<ShiftOverviewComponent>;
  let component: ShiftOverviewComponent;
  let httpMock: HttpTestingController;
  let navigated: unknown[][];

  async function setup(): Promise<void> {
    navigated = [];
    await TestBed.configureTestingModule({
      imports: [ShiftOverviewComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Router, useValue: { navigate: (c: unknown[]) => { navigated.push(c); } } },
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
      ],
    })
      .overrideComponent(ShiftOverviewComponent, {
        remove: { imports: [TranslatePipe] },
        add: { imports: [MockTranslatePipe] },
      })
      .compileComponents();
    fixture = TestBed.createComponent(ShiftOverviewComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  }

  function flushInit(): void {
    httpMock.expectOne((r) => r.url.endsWith('/sites/regions')).flush([{ id: 2, name: 'Cairo' }]);
    httpMock
      .expectOne((r) => r.url.endsWith('/shifts/overview'))
      .flush({ items: SITES, totalCount: 1, page: 1, pageSize: 9 });
    fixture.detectChanges();
  }

  afterEach(() => httpMock.verify());

  it('should load regions and the first overview page on init', async () => {
    await setup();
    flushInit();

    expect(component.regions().length).toBe(1);
    expect(component.sites().length).toBe(1);
    expect(component.totalCount()).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('Cairo HQ');
  });

  it('should send search server-side after debounce and reset to page one', async () => {
    await setup();
    flushInit();

    component.onSearchInput('Cairo');
    await new Promise((r) => setTimeout(r, 400));
    fixture.detectChanges();

    const req = httpMock.expectOne((r) => r.url.endsWith('/shifts/overview'));
    expect(req.request.params.get('search')).toBe('Cairo');
    expect(req.request.params.get('page')).toBe('1');
    req.flush({ items: SITES, totalCount: 1, page: 1, pageSize: 9 });
  });

  it('should reload with the region filter on region change', async () => {
    await setup();
    flushInit();

    component.onRegionChange('2');
    fixture.detectChanges();

    const req = httpMock.expectOne((r) => r.url.endsWith('/shifts/overview'));
    expect(req.request.params.get('regionId')).toBe('2');
    req.flush({ items: SITES, totalCount: 1, page: 1, pageSize: 9 });
  });

  it('should PATCH smart settings on toggle and revert on error', async () => {
    await setup();
    flushInit();

    component.toggleAssignment(component.sites()[0]);
    expect(component.sites()[0].isSmartAssignmentEnabled).toBe(false);

    const req = httpMock.expectOne((r) => r.url.endsWith('/sites/1/smart-settings'));
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({
      isSmartAssignmentEnabled: false,
      isSmartPostingEnabled: false,
    });
    req.flush(null, { status: 500, statusText: 'Error' });
    fixture.detectChanges();

    expect(component.sites()[0].isSmartAssignmentEnabled).toBe(true);
    expect(component.actionError()).toBe('SHIFT_MANAGEMENT.OVERVIEW.SMART_ERROR');
  });

  it('should navigate to the board on card click', async () => {
    await setup();
    flushInit();

    component.openSite(component.sites()[0]);

    expect(navigated).toEqual([['/scheduling/shift-management', 1]]);
  });

  it('should send no request on Export click (visual-only)', async () => {
    await setup();
    flushInit();

    const exportBtn = Array.from(
      fixture.nativeElement.querySelectorAll('app-button'),
    ) as HTMLElement[];
    exportBtn[0]?.click();
    fixture.detectChanges();

    httpMock.expectNone((r) => !r.url.endsWith('/shifts/overview') && !r.url.endsWith('/sites/regions'));
  });
});
