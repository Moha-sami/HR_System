import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { EmployeeListComponent } from './employee-list.component';

describe('EmployeeListComponent', () => {
  let fixture: ComponentFixture<EmployeeListComponent>;
  let component: EmployeeListComponent;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [EmployeeListComponent],
      providers: [provideRouter([])],
    });

    fixture = TestBed.createComponent(EmployeeListComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render the first page of employee rows in the table', () => {
    const table = fixture.nativeElement.querySelector('app-table') as HTMLElement;

    expect(table.textContent).toContain('Employee ID');
    expect(table.textContent).toContain('0409');
    expect(table.textContent).toContain('Ahmed Mohamed');
    expect(table.textContent).toContain('UI Designer');
    expect(table.textContent).not.toContain('Lina Mahmoud');
  });

  it('should update the displayed employee rows when the page changes', () => {
    component.onPageChanged(2);
    fixture.detectChanges();

    const table = fixture.nativeElement.querySelector('app-table') as HTMLElement;
    expect(table.textContent).toContain('Lina Mahmoud');
    expect(table.textContent).toContain('Youssef Ahmed');
    expect(table.textContent).not.toContain('Ahmed Mohamed');
  });

  it('should navigate to the add employee route', () => {
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const addEmployeeButton = fixture.nativeElement.querySelector(
      'app-button button',
    ) as HTMLButtonElement;

    addEmployeeButton.click();

    expect(navigate).toHaveBeenCalledWith(['/employees/add']);
  });

  it('should render the employee name in one custom table cell', () => {
    const table = fixture.nativeElement.querySelector('app-table') as HTMLElement;

    expect(table.textContent).toContain('Ahmed Mohamed');
    expect(table.textContent).not.toContain('undefined');
  });

  it('should render a view action for each displayed employee', () => {
    const viewActions = fixture.nativeElement.querySelectorAll(
      'button[aria-label^="View "]',
    ) as NodeListOf<HTMLButtonElement>;

    expect(viewActions).toHaveLength(component.pageSize);
    expect(viewActions[0].getAttribute('aria-label')).toBe('View Ahmed Mohamed');
  });
});
