import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA, Pipe, type PipeTransform } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { environment } from '../../../../../environments/environment';
import { EmployeeStripComponent } from './employee-strip.component';
import type { ShiftCandidateEmployee } from '../../data-access/models/shifts-lookups.models';

@Pipe({ name: 'translate', standalone: true })
class MockTranslatePipe implements PipeTransform {
  transform(value: string): string {
    return value;
  }
}

const API_BASE = environment.baseUrl;

const SARA: ShiftCandidateEmployee = {
  id: 100, employeeCode: 'E100', fullName: 'Sara', roleTitle: 'Cashier',
  jobRoleId: 10, weeklyCompletedHours: 20, ratingScore: 4.6,
  riskStatusToken: 'TopPerformer', isPreferredForSite: true,
};

const OMAR: ShiftCandidateEmployee = {
  id: 101, employeeCode: 'E101', fullName: 'Omar', roleTitle: 'Guard',
  jobRoleId: 11, weeklyCompletedHours: 40, ratingScore: 3.5,
  riskStatusToken: 'OvertimeRisk', isPreferredForSite: false,
};

function page(items: ShiftCandidateEmployee[], totalCount: number) {
  return { items, totalCount, page: 1, pageSize: 20 };
}

/** Ticket #329: strip renders cards/badges and pages server-side on scroll. */
describe('EmployeeStripComponent', () => {
  let fixture: ComponentFixture<EmployeeStripComponent>;
  let component: EmployeeStripComponent;
  let httpMock: HttpTestingController;

  async function setup(siteIds: number[], assignedIds: number[] = []): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [EmployeeStripComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
      ],
    })
      .overrideComponent(EmployeeStripComponent, {
        remove: { imports: [TranslatePipe] },
        add: { imports: [MockTranslatePipe] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(EmployeeStripComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.componentRef.setInput('siteIds', siteIds);
    fixture.componentRef.setInput('assignedIds', assignedIds);
    fixture.detectChanges();
  }

  function flushStrip(items: ShiftCandidateEmployee[], totalCount: number): void {
    const req = httpMock.expectOne((r) => r.url === `${API_BASE}/shifts/employees`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('PageSize')).toBe('20');
    req.flush(page(items, totalCount));
    fixture.detectChanges();
  }

  afterEach(() => httpMock.verify());

  it('should load page 1 for the selected sites and render cards with risk badges', async () => {
    await setup([1]);
    flushStrip([SARA, OMAR], 2);

    expect(component.items().length).toBe(2);
    expect(component.totalCount()).toBe(2);
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Sara');
    expect(text).toContain('SHIFT_TEMPLATES.STRIP.RISK_TOP_PERFORMER');
    expect(text).toContain('SHIFT_TEMPLATES.STRIP.RISK_OVERTIME');
  });

  it('should send the SiteId of the selected site', async () => {
    await setup([2]);
    const req = httpMock.expectOne((r) => r.url === `${API_BASE}/shifts/employees`);
    expect(req.request.params.get('SiteId')).toBe('2');
    req.flush(page([], 0));
  });

  it('should not request anything and hint at site selection when no site is chosen', async () => {
    await setup([]);
    httpMock.expectNone((r) => r.url === `${API_BASE}/shifts/employees`);

    expect(component.items()).toEqual([]);
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('SHIFT_TEMPLATES.STRIP.NO_SITES');
  });

  it('should load the next page when scrolled near the end', async () => {
    await setup([1]);
    flushStrip([SARA], 2);

    component.onScroll({
      target: { scrollLeft: 800, clientWidth: 400, scrollWidth: 1250 },
    } as unknown as Event);

    const req = httpMock.expectOne((r) => r.url === `${API_BASE}/shifts/employees`);
    expect(req.request.params.get('Page')).toBe('2');
    req.flush({ items: [OMAR], totalCount: 2, page: 2, pageSize: 20 });
    fixture.detectChanges();

    expect(component.items().length).toBe(2);
  });

  it('should reload page 1 with the search term after debounce', async () => {
    await setup([1]);
    flushStrip([SARA], 1);

    component.onSearchInput('sa');
    await new Promise((r) => setTimeout(r, 400));

    const req = httpMock.expectOne((r) => r.url === `${API_BASE}/shifts/employees`);
    expect(req.request.params.get('Search')).toBe('sa');
    expect(req.request.params.get('Page')).toBe('1');
    req.flush(page([SARA], 1));
    expect(component.items().length).toBe(1);
  });

  it('should mark assigned employees without removing them', async () => {
    await setup([1], [100]);
    flushStrip([SARA, OMAR], 2);

    expect(component.isAssigned(100)).toBe(true);
    expect(component.isAssigned(101)).toBe(false);
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('SHIFT_TEMPLATES.STRIP.ASSIGNED');
  });

  it('should emit every loaded page for the picker fallback', async () => {
    await setup([1]);
    const emitted: { page: number; items: ShiftCandidateEmployee[] }[] = [];
    component.pageLoaded.subscribe((e) => emitted.push(e));
    flushStrip([SARA], 1);

    expect(emitted.length).toBe(1);
    expect(emitted[0].page).toBe(1);
    expect(emitted[0].items).toEqual([SARA]);
  });
});
