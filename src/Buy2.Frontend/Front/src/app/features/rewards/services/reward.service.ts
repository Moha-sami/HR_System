import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import type {
  CreateRewardDto,
  RewardCategory,
  RewardItem,
  RewardRedemption,
} from '../models/reward.models';

@Injectable({
  providedIn: 'root',
})
export class RewardService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.jsonServerUrl}/rewardItems`;
  private readonly categoriesUrl = `${environment.jsonServerUrl}/rewardCategories`;
  private readonly redemptionsUrl = `${environment.jsonServerUrl}/rewardRedemptions`;

  getRewards(): Observable<RewardItem[]> {
    return this.http.get<RewardItem[]>(this.apiUrl);
  }

  getReward(id: string): Observable<RewardItem> {
    return this.http.get<RewardItem>(`${this.apiUrl}/${id}`);
  }

  createReward(dto: CreateRewardDto): Observable<RewardItem> {
    return this.http.post<RewardItem>(this.apiUrl, dto);
  }

  updateReward(id: string, dto: Partial<CreateRewardDto>): Observable<RewardItem> {
    return this.http.patch<RewardItem>(`${this.apiUrl}/${id}`, dto);
  }

  deleteReward(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getCategories(): Observable<RewardCategory[]> {
    return this.http.get<RewardCategory[]>(this.categoriesUrl);
  }

  getRedemptions(): Observable<RewardRedemption[]> {
    return this.http.get<RewardRedemption[]>(this.redemptionsUrl);
  }
}
