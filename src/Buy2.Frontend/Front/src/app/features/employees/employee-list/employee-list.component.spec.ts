import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import { LanguageService } from '@app/core/services/language.service';
import { EmployeeListComponent } from './employee-list.component';

describe('EmployeeListComponent', () => {
  let fixture: ComponentFixture<EmployeeListComponent>;
  let component: EmployeeListComponent;
  let router: Router;
  let translate: TranslateService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [EmployeeListComponent],
      providers: [provideRouter([]), provideTranslateService()],
    });

    translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', {
      EMPLOYEE_MANAGEMENT: {
        TITLE: 'Employee Management',
        ADD_EMPLOYEES: 'Add Employees',
        EMPLOYEE_ID: 'Employee ID',
        EMPLOYEE_NAME: 'Employee Name',
        JOIN_DATE: 'Join Date',
        JOB_TITLE: 'Job Title',
        EMAIL: 'Email',
        ADMIN_ACCESS: 'Admin Access',
        ACTIONS: 'Actions',
        NO_EMPLOYEES: 'No employees available',
      },
    });
    translate.setTranslation('ar', {
      EMPLOYEE_MANAGEMENT: {
        TITLE: 'إدارة الموظفين',
        ADD_EMPLOYEES: 'إضافة موظفين',
        EMPLOYEE_ID: 'معرف الموظف',
        EMPLOYEE_NAME: 'اسم الموظف',
        JOIN_DATE: 'تاريخ الانضمام',
        JOB_TITLE: 'المسمى الوظيفي',
        EMAIL: 'البريد الإلكتروني',
        ADMIN_ACCESS: 'صلاحية المسؤول',
        ACTIONS: 'الإجراءات',
        NO_EMPLOYEES: 'لا يوجد موظفون',
      },
    });
    translate.use('en').subscribe();

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

  it('should update column labels when the language changes', () => {
    TestBed.inject(LanguageService).changeLanguage('ar').subscribe();
    fixture.detectChanges();

    const table = fixture.nativeElement.querySelector('app-table') as HTMLElement;
    expect(table.textContent).toContain('معرف الموظف');
    expect(table.textContent).toContain('اسم الموظف');
  });
});
