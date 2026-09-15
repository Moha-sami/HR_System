import { Pipe, type PipeTransform } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';

import { RewardService } from '../../services/reward.service';
import { RewardListComponent } from './reward-list.component';

@Pipe({ name: 'translate', standalone: true })
class MockTranslatePipe implements PipeTransform {
  transform(value: string): string {
    return value;
  }
}

describe('RewardListComponent', () => {
  let component: RewardListComponent;
  let fixture: ComponentFixture<RewardListComponent>;
  let getRewards: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    getRewards = vi.fn().mockReturnValue(
      of({
        items: [
          {
            id: 1,
            name: 'Amazon Card',
            category: 'Gift Cards',
            points: 200,
            monetaryValue: 50,
            stockRatio: '2/3',
            redemptionCount: 4,
            isActive: true,
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 10,
      }),
    );

    await TestBed.configureTestingModule({
      imports: [RewardListComponent],
      providers: [
        {
          provide: RewardService,
          useValue: {
            getRewards,
            deleteReward: vi.fn(),
          },
        },
        {
          provide: TranslateService,
          useValue: {
            instant: (key: string) => key,
            stream: () => of({}),
            onLangChange: of({}),
          },
        },
        { provide: Router, useValue: { navigate: () => Promise.resolve(true) } },
      ],
    })
      .overrideComponent(RewardListComponent, {
        remove: { imports: [TranslatePipe] },
        add: { imports: [MockTranslatePipe] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(RewardListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should load rewards from the real API on init', () => {
    expect(component).toBeTruthy();
    expect(getRewards).toHaveBeenCalled();
    const filter = getRewards.mock.calls.at(-1)?.[0];
    expect(filter.page).toBe(1);
    expect(filter.pageSize).toBe(10);
    expect(filter.sortBy).toBe('name');
    expect(filter.sortDescending).toBe(false);
    expect(component.rewards().length).toBe(1);
  });

  it('should reload with status filter', () => {
    getRewards.mockClear();
    component.onStatusChange('Active');
    const filter = getRewards.mock.calls.at(-1)?.[0];
    expect(filter.status).toBe('Active');
    expect(filter.page).toBe(1);
  });

  it('should map table sort to API sort fields', () => {
    getRewards.mockClear();
    component.onSort({ column: 'monetaryValue', direction: 'desc' });
    const filter = getRewards.mock.calls.at(-1)?.[0];
    expect(filter.sortBy).toBe('price');
    expect(filter.sortDescending).toBe(true);
  });

  it('should set loadError when the list request fails', () => {
    getRewards.mockReturnValue(throwError(() => new Error('fail')));
    component.loadRewards();
    expect(component.loadError()).toBe(true);
    expect(component.loading()).toBe(false);
    expect(component.rewards()).toEqual([]);
  });
});
