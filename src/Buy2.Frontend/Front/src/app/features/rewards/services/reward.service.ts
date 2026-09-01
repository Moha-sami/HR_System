import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';
import type {
  CreateInventoryDto,
  CreateRewardDto,
  EmployeeName,
  RewardCategory,
  RewardInventoryItem,
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
  private readonly inventoryUrl = `${environment.jsonServerUrl}/rewardInventory`;
  private readonly employeesUrl = `${environment.jsonServerUrl}/employees`;

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

  getInventory(rewardItemId: string): Observable<RewardInventoryItem[]> {
    return this.http.get<RewardInventoryItem[]>(this.inventoryUrl).pipe(
      map((items) => items.filter((item) => String(item.rewardItemId) === String(rewardItemId))),
    );
  }

  createInventory(dto: CreateInventoryDto): Observable<RewardInventoryItem> {
    return this.http.post<RewardInventoryItem>(this.inventoryUrl, dto);
  }

  deleteInventory(id: string): Observable<void> {
    return this.http.delete<void>(`${this.inventoryUrl}/${id}`);
  }

  getEmployees(): Observable<EmployeeName[]> {
    return this.http.get<EmployeeName[]>(this.employeesUrl);
  }
}
