import { Injectable, computed, inject, signal } from '@angular/core';
import type { RewardItem, RewardKpiStatistics } from '../models/reward.models';
import { mapRewardProfileToItem } from '../models/reward.models';
import { RewardService } from './reward.service';

@Injectable({ providedIn: 'root' })
export class RewardDetailsContext {
  private readonly rewardService = inject(RewardService);

  readonly rewardId = signal('');
  readonly reward = signal<RewardItem | null>(null);
  readonly kpiStats = signal<RewardKpiStatistics | null>(null);
  readonly loading = signal(false);
  readonly loadError = signal(false);
  readonly toggling = signal(false);

  readonly isEnabled = computed(() => this.reward()?.status === 'Active');

  load(rewardId: string): void {
    this.rewardId.set(rewardId);
    this.loading.set(true);
    this.loadError.set(false);

    this.rewardService.getRewardProfile(rewardId).subscribe({
      next: (details) => {
        this.reward.set(mapRewardProfileToItem(details.profile, details.kpiStats));
        this.kpiStats.set(details.kpiStats);
        this.loading.set(false);
      },
      error: () => {
        this.loadError.set(true);
        this.loading.set(false);
      },
    });
  }

  reloadProfile(): void {
    const rewardId = this.rewardId();
    if (!rewardId) {
      return;
    }
    this.rewardService.getRewardProfile(rewardId).subscribe({
      next: (details) => {
        this.reward.set(mapRewardProfileToItem(details.profile, details.kpiStats));
        this.kpiStats.set(details.kpiStats);
      },
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
}
