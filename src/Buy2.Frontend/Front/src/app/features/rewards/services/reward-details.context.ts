import { Injectable, computed, inject, signal } from '@angular/core';
import { catchError, forkJoin, of } from 'rxjs';
import type {
  EmployeeName,
  RewardInventoryItem,
  RewardItem,
  RewardKpiStatistics,
  RewardRedemption,
} from '../models/reward.models';
import { mapRewardProfileToItem } from '../models/reward.models';
import { RewardService } from './reward.service';

@Injectable({ providedIn: 'root' })
export class RewardDetailsContext {
  private readonly rewardService = inject(RewardService);

  readonly rewardId = signal('');
  readonly reward = signal<RewardItem | null>(null);
  readonly kpiStats = signal<RewardKpiStatistics | null>(null);
  readonly inventory = signal<RewardInventoryItem[]>([]);
  readonly redemptions = signal<RewardRedemption[]>([]);
  readonly employees = signal<EmployeeName[]>([]);
  readonly loading = signal(false);
  readonly loadError = signal(false);
  readonly toggling = signal(false);

  readonly availableCount = computed(
    () => this.inventory().filter((item) => item.status === 'Available').length,
  );

  readonly totalCount = computed(() => this.inventory().length);

  readonly redemptionCount = computed(
    () => this.inventory().filter((item) => item.status === 'Redeemed').length,
  );

  readonly topRedeemed = computed(() => peakDayCount(this.redemptionDates()));

  readonly isEnabled = computed(() => this.reward()?.status === 'Active');

  employeeName(employeeId: string | number | null | undefined): string {
    if (employeeId == null) {
      return '—';
    }
    const employee = this.employees().find((item) => String(item.id) === String(employeeId));
    return employee ? `${employee.firstName} ${employee.lastName}` : '—';
  }

  load(rewardId: string): void {
    this.rewardId.set(rewardId);
    this.loading.set(true);
    this.loadError.set(false);

    forkJoin({
      details: this.rewardService.getRewardProfile(rewardId),
      inventory: this.rewardService.getInventory(rewardId).pipe(catchError(() => of([]))),
      redemptions: this.rewardService.getRedemptions().pipe(catchError(() => of([]))),
      employees: this.rewardService.getEmployees().pipe(catchError(() => of([]))),
    }).subscribe({
      next: ({ details, inventory, redemptions, employees }) => {
        this.reward.set(mapRewardProfileToItem(details.profile, details.kpiStats));
        this.kpiStats.set(details.kpiStats);
        this.inventory.set(inventory);
        this.redemptions.set(
          redemptions.filter((item) => String(item.rewardItemId) === String(rewardId)),
        );
        this.employees.set(employees);
        this.loading.set(false);
      },
      error: () => {
        this.loadError.set(true);
        this.loading.set(false);
      },
    });
  }

  reloadInventory(): void {
    const rewardId = this.rewardId();
    if (!rewardId) {
      return;
    }
    this.rewardService.getInventory(rewardId).subscribe({
      next: (inventory) => this.inventory.set(inventory),
    });
  }

  toggleEnabled(): void {
    const reward = this.reward();
    if (!reward || this.toggling()) {
      return;
    }
    this.toggling.set(true);
    const status = reward.status === 'Active' ? 'Inactive' : 'Active';
    this.reward.set({ ...reward, status });
    this.toggling.set(false);
  }

  private redemptionDates(): string[] {
    const fromInventory = this.inventory()
      .filter((item) => item.status === 'Redeemed')
      .map((item) => item.redeemedAt || item.createdAt);
    const fromRedemptions = this.redemptions().map((item) => item.redeemedAt);
    return [...fromInventory, ...fromRedemptions];
  }
}

export function peakDayCount(isoDates: string[]): number {
  const counts = new Map<string, number>();
  for (const iso of isoDates) {
    if (!iso) {
      continue;
    }
    const day = iso.slice(0, 10);
    counts.set(day, (counts.get(day) ?? 0) + 1);
  }
  let peak = 0;
  for (const count of counts.values()) {
    peak = Math.max(peak, count);
  }
  return peak;
}
