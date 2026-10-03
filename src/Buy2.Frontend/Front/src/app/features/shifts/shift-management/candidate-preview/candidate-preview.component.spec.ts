import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA, Pipe, type PipeTransform } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TranslatePipe } from '@ngx-translate/core';
import { CandidatePreviewComponent } from './candidate-preview.component';

@Pipe({ name: 'translate', standalone: true })
class MockTranslatePipe implements PipeTransform {
  transform(value: string): string {
    return value;
  }
}

const PREVIEW = {
  id: 7,
  employeeCode: 'EMP-1',
  fullName: 'Sara',
  avatarUrl: null,
  jobTitle: 'Cashier',
  joinDate: '2020-07-29',
  hourlyRate: 11,
  rating: 4.5,
  careerCompletedHours: 2956,
  qualifications: ['Cashier Training'],
};

/** Ticket A: shared candidate preview modal. */
describe('CandidatePreviewComponent', () => {
  let fixture: ComponentFixture<CandidatePreviewComponent>;
  let component: CandidatePreviewComponent;
  let httpMock: HttpTestingController;

  async function setup(employeeId: number | null): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [CandidatePreviewComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    })
      .overrideComponent(CandidatePreviewComponent, {
        remove: { imports: [TranslatePipe] },
        add: { imports: [MockTranslatePipe] },
      })
      .compileComponents();
    fixture = TestBed.createComponent(CandidatePreviewComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.componentRef.setInput('employeeId', employeeId);
    fixture.detectChanges();
  }

  afterEach(() => httpMock.verify());

  it('should create without fetching when closed', async () => {
    await setup(null);
    expect(component).toBeTruthy();
    expect(fixture.nativeElement.querySelector('app-modal')).toBeNull();
  });

  it('should load the preview when opened', async () => {
    await setup(7);

    httpMock.expectOne((r) => r.url.endsWith('/shifts/candidates/7/preview')).flush(PREVIEW);
    fixture.detectChanges();

    expect(component.preview()?.fullName).toBe('Sara');
    expect(fixture.nativeElement.textContent).toContain('Sara');
    expect(fixture.nativeElement.textContent).toContain('Cashier Training');
  });

  it('should show the error state on load failure', async () => {
    await setup(7);

    httpMock
      .expectOne((r) => r.url.endsWith('/shifts/candidates/7/preview'))
      .flush(null, { status: 500, statusText: 'Error' });
    fixture.detectChanges();

    expect(component.loadError()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('SHIFT_MANAGEMENT.PREVIEW.LOAD_ERROR');
  });

  it('should emit closed on close', async () => {
    await setup(7);
    httpMock.expectOne((r) => r.url.endsWith('/shifts/candidates/7/preview')).flush(PREVIEW);

    const closed: unknown[] = [];
    component.closed.subscribe(() => closed.push(true));
    component.close();

    expect(closed.length).toBe(1);
  });
});
