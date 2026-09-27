import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { PointsRewardsTabComponent } from './points-rewards-tab.component';
import { EmployeeDetailService } from '../../../../services/employee-detail.service';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LanguageService } from '@app/core/services/language.service';
import { Pipe, type PipeTransform, signal } from '@angular/core';
import type {
  EmployeePointsSummary,
  PaginatedEmployeePointsTransactions,
} from '../../../../models/view-employee/employee-points';

@Pipe({ name: 'translate', standalone: true })
class MockTranslatePipe implements PipeTransform {
  transform(value: string): string {
    return value;
  }
}

describe('PointsRewardsTabComponent', () => {
  let component: PointsRewardsTabComponent;
  let fixture: ComponentFixture<PointsRewardsTabComponent>;

  let mockEmployeeDetailService: {
    detailEmployee: ReturnType<typeof signal<any>>;
    pointsSummary: ReturnType<typeof signal<EmployeePointsSummary | null>>;
    pointsSummaryLoading: ReturnType<typeof signal<boolean>>;
    pointsSummaryError: ReturnType<typeof signal<string | null>>;
    pointsTransactions: ReturnType<typeof signal<PaginatedEmployeePointsTransactions | null>>;
    pointsTransactionsLoading: ReturnType<typeof signal<boolean>>;
    pointsTransactionsError: ReturnType<typeof signal<string | null>>;
    loadEmployeePointsSummary: jasmine.Spy;
    loadEmployeePointsTransactions: jasmine.Spy;
    clearEmployeePoints: jasmine.Spy;
  };

  let mockTranslateService: {
    instant: jasmine.Spy;
  };

  let mockLanguageService: {
    currentLanguage: ReturnType<typeof signal<string>>;
  };

  const mockEmployee = {
    id: 42,
    employeeCode: 'EMP-0042',
    fullName: 'Ahmed Ali',
  };

  const mockSummary: EmployeePointsSummary = {
    currentBalance: 2500,
    totalPointsRedeemed: 800,
    totalRewardsRedeemed: 12,
    totalRewardsCostPoints: 1500,
  };

  const mockTransactionsResponse: PaginatedEmployeePointsTransactions = {
    items: [
      {
        id: 1,
        date: '2026-09-01T10:00:00Z',
        amount: 100,
        triggeredBy: 'Attendance System',
        comments: 'On-time arrival streak bonus',
        type: 'Earned',
      },
      {
        id: 2,
        date: '2026-09-02T14:30:00Z',
        amount: -250,
        triggeredBy: 'Self Service Portal',
        comments: 'Redeemed Gift Card',
        type: 'Redeemed',
      },
      {
        id: 3,
        date: '2026-09-03T09:15:00Z',
        amount: 50,
        triggeredBy: 'Manager Approval',
        comments: 'Shift coverage incentive',
        type: 'Add',
      },
    ],
    page: 1,
    pageSize: 10,
    totalCount: 3,
    totalPages: 1,
  };

  beforeEach(async () => {
    mockEmployeeDetailService = {
      detailEmployee: signal(mockEmployee),
      pointsSummary: signal(mockSummary),
      pointsSummaryLoading: signal(false),
      pointsSummaryError: signal(null),
      pointsTransactions: signal(mockTransactionsResponse),
      pointsTransactionsLoading: signal(false),
      pointsTransactionsError: signal(null),
      loadEmployeePointsSummary: jasmine.createSpy('loadEmployeePointsSummary'),
      loadEmployeePointsTransactions: jasmine.createSpy('loadEmployeePointsTransactions'),
      clearEmployeePoints: jasmine.createSpy('clearEmployeePoints'),
    };

    mockTranslateService = {
      instant: jasmine.createSpy('instant').and.callFake((key: string) => key),
    };

    mockLanguageService = {
      currentLanguage: signal('en'),
    };

    await TestBed.configureTestingModule({
      imports: [PointsRewardsTabComponent],
      providers: [
        { provide: EmployeeDetailService, useValue: mockEmployeeDetailService },
        { provide: TranslateService, useValue: mockTranslateService },
        { provide: LanguageService, useValue: mockLanguageService },
      ],
    })
      .overrideComponent(PointsRewardsTabComponent, {
        remove: { imports: [TranslatePipe] },
        add: { imports: [MockTranslatePipe] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(PointsRewardsTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should display summary metrics when loaded', () => {
    expect(component.summary()).toEqual(mockSummary);
    expect(component.summary()?.currentBalance).toBe(2500);
    expect(component.summary()?.totalPointsRedeemed).toBe(800);
    expect(component.summary()?.totalRewardsRedeemed).toBe(12);
    expect(component.summary()?.totalRewardsCostPoints).toBe(1500);
  });

  it('should format amounts correctly with prefix for positive numbers', () => {
    expect(component.formatAmount(100)).toBe('+100');
    expect(component.formatAmount(-250)).toBe('-250');
    expect(component.formatAmount(0)).toBe('0');
  });

  it('should return all visible transactions when search query is empty', () => {
    expect(component.visibleTransactions().length).toBe(3);
  });

  it('should filter transactions by search query', () => {
    component.onSearchChange('Gift Card');
    expect(component.visibleTransactions().length).toBe(1);
    expect(component.visibleTransactions()[0].comments).toBe('Redeemed Gift Card');

    component.onSearchChange('streak bonus');
    expect(component.visibleTransactions().length).toBe(1);
    expect(component.visibleTransactions()[0].triggeredBy).toBe('Attendance System');

    component.onSearchChange('NonExistentKeyword');
    expect(component.visibleTransactions().length).toBe(0);
  });

  it('should trigger server filter reload on type change', () => {
    component.selectedType.set('Earned');
    component.onServerFilterChange();

    expect(component.currentPage()).toBe(1);
    expect(mockEmployeeDetailService.loadEmployeePointsTransactions).toHaveBeenCalledWith(
      42,
      jasmine.objectContaining({
        page: 1,
        pageSize: 10,
        type: 'Earned',
      }),
    );
  });

  it('should handle pagination changes', () => {
    component.changePage(2);
    expect(component.currentPage()).toBe(2);
    expect(mockEmployeeDetailService.loadEmployeePointsTransactions).toHaveBeenCalledWith(
      42,
      jasmine.objectContaining({
        page: 2,
        pageSize: 10,
      }),
    );
  });

  it('should retry loading summary when retrySummary is called', () => {
    component.retrySummary();
    expect(mockEmployeeDetailService.loadEmployeePointsSummary).toHaveBeenCalledWith(42);
  });

  it('should format date properly according to active language', () => {
    const formattedDateEn = component.formatDate('2026-09-01T10:00:00Z');
    expect(formattedDateEn).toBeTruthy();
    expect(typeof formattedDateEn).toBe('string');

    mockLanguageService.currentLanguage.set('ar');
    const formattedDateAr = component.formatDate('2026-09-01T10:00:00Z');
    expect(formattedDateAr).toBeTruthy();
    expect(typeof formattedDateAr).toBe('string');
  });

  it('should apply triggeredBy filter on blur if changed', () => {
    component.triggeredBy.set('Supervisor Admin');
    component.applyTriggeredByFilter();

    expect(mockEmployeeDetailService.loadEmployeePointsTransactions).toHaveBeenCalledWith(
      42,
      jasmine.objectContaining({
        triggeredBy: 'Supervisor Admin',
      }),
    );
  });
});
