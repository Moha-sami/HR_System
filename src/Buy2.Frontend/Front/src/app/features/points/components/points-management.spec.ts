import { signal, Pipe, type PipeTransform } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';

import { LanguageService } from '../../../core/services/language.service';
import { PointsManagementService } from '../service/points-management.service';
import { PointsManagement } from './points-management';

@Pipe({ name: 'translate', standalone: true })
class MockTranslatePipe implements PipeTransform {
  transform(value: string): string {
    return value;
  }
}

describe('PointsManagement', () => {
  let component: PointsManagement;
  let fixture: ComponentFixture<PointsManagement>;
  let getTransactions: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    getTransactions = vi.fn().mockReturnValue(
      of({
        items: [],
        totalCount: 0,
        pageNumber: 1,
        pageSize: 10,
        totalPages: 0,
      }),
    );

    await TestBed.configureTestingModule({
      imports: [PointsManagement],
      providers: [
        { provide: PointsManagementService, useValue: { getTransactions } },
        {
          provide: TranslateService,
          useValue: {
            instant: (key: string) => key,
            stream: () => of({}),
            onLangChange: of({}),
          },
        },
        { provide: LanguageService, useValue: { currentLanguage: signal('en') } },
        { provide: Router, useValue: { navigate: () => Promise.resolve(true) } },
      ],
    })
      .overrideComponent(PointsManagement, {
        remove: { imports: [TranslatePipe] },
        add: { imports: [MockTranslatePipe] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(PointsManagement);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load transactions from the real API service', () => {
    expect(component).toBeTruthy();
    expect(getTransactions).toHaveBeenCalled();

    const filter = getTransactions.mock.calls.at(-1)?.[0];
    expect(filter.pageNumber).toBe(1);
    expect(filter.pageSize).toBe(10);
    expect(filter.sortBy).toBe('CreatedAt');
    expect(filter.sortDir).toBe('Desc');
    expect(filter.month).toBeGreaterThanOrEqual(1);
    expect(filter.year).toBeGreaterThanOrEqual(2000);
  });

  it('should reload with transaction type and triggeredBy filters', () => {
    getTransactions.mockClear();

    const typeEvent = { target: { value: 'Add' } } as unknown as Event;
    component.updateType(typeEvent);

    const triggerEvent = { target: { value: 'ManualAdjustment' } } as unknown as Event;
    component.updateTrigger(triggerEvent);

    const latest = getTransactions.mock.calls.at(-1)?.[0];
    expect(latest.transactionType).toBe('Add');
    expect(latest.triggeredBy).toBe('ManualAdjustment');
    expect(latest.pageNumber).toBe(1);
  });

  it('should map table sort to API sort fields', () => {
    getTransactions.mockClear();

    component.onSortChange({ column: 'employeeName', direction: 'asc' });

    const filter = getTransactions.mock.calls.at(-1)?.[0];
    expect(filter.sortBy).toBe('EmployeeName');
    expect(filter.sortDir).toBe('Asc');
  });

  it('should set loadError when the list request fails', () => {
    getTransactions.mockReturnValue(throwError(() => new Error('fail')));

    component.loadTransactions();

    expect(component.loadError()).toBe(true);
    expect(component.loading()).toBe(false);
    expect(component.transactions()).toEqual([]);
  });
});
