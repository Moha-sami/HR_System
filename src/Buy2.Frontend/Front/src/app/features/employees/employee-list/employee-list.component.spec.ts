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

    expect(table.textContent).toContain('Ahmed');
    expect(table.textContent).toContain('Fatima');
    expect(table.textContent).not.toContain('Khalid');
  });

  it('should update the displayed employee rows when the page changes', () => {
    component.onPageChanged(2);
    fixture.detectChanges();

    const table = fixture.nativeElement.querySelector('app-table') as HTMLElement;
    expect(table.textContent).toContain('Khalid');
    expect(table.textContent).toContain('Youssef');
    expect(table.textContent).not.toContain('Ahmed');
  });

  it('should navigate to the add employee route', () => {
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const addEmployeeButton = fixture.nativeElement.querySelector(
      'app-button button',
    ) as HTMLButtonElement;

    addEmployeeButton.click();

    expect(navigate).toHaveBeenCalledWith(['/employees/add']);
  });
});
