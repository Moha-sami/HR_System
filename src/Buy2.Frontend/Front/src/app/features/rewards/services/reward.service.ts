import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';
import {
  mapRewardProfileToItem,
  type CreateInventoryDto,
  type EmployeeName,
  type PaginatedRewards,
  type RewardCategory,
  type RewardInventoryItem,
  type RewardItem,
  type RewardListFilter,
  type RewardProfileDto,
  type RewardProfileResponseDto,
  type RewardRedemption,
  type RewardWriteInput,
} from '../models/reward.models';

const REWARDS_API = `${environment.baseUrl}/rewards`;

export function buildRewardListParams(filter: RewardListFilter): HttpParams {
  let params = new HttpParams()
    .set('page', filter.page.toString())
    .set('pageSize', filter.pageSize.toString());

  if (filter.search?.trim()) {
    params = params.set('search', filter.search.trim());
  }
  if (filter.status && filter.status !== 'All') {
    params = params.set('status', filter.status);
  }
  if (filter.fromDate) {
    params = params.set('fromDate', filter.fromDate);
  }
  if (filter.toDate) {
    params = params.set('toDate', filter.toDate);
  }
  if (filter.sortBy) {
    params = params.set('sortBy', filter.sortBy);
  }
  if (filter.sortDescending != null) {
    params = params.set('sortDescending', String(filter.sortDescending));
  }

  return params;
}

export function buildRewardFormData(input: RewardWriteInput): FormData {
  const formData = new FormData();
  formData.append('name', input.name);
  formData.append('description', input.description);
  formData.append('categoryId', String(input.categoryId));
  formData.append('points', String(input.points));
  formData.append('monetaryValue', String(input.monetaryValue));
  formData.append('howToRedeem', input.howToRedeem);
  formData.append('termsOfUse', input.termsOfUse);
  if (input.imageFile) {
    formData.append('imageFile', input.imageFile, input.imageFile.name);
  }
  return formData;
}

@Injectable({
  providedIn: 'root',
})
export class RewardService {
  private readonly http = inject(HttpClient);
  private readonly categoriesUrl = `${environment.jsonServerUrl}/rewardCategories`;
  private readonly redemptionsUrl = `${environment.jsonServerUrl}/rewardRedemptions`;
  private readonly inventoryUrl = `${environment.jsonServerUrl}/rewardInventory`;
  private readonly employeesUrl = `${environment.jsonServerUrl}/employees`;

  getRewards(filter: RewardListFilter): Observable<PaginatedRewards> {
    return this.http.get<PaginatedRewards>(REWARDS_API, {
      params: buildRewardListParams(filter),
    });
  }

  getRewardProfile(id: number | string): Observable<RewardProfileResponseDto> {
    return this.http.get<RewardProfileResponseDto>(`${REWARDS_API}/${id}`);
  }

  getReward(id: string): Observable<RewardItem> {
    return this.getRewardProfile(id).pipe(
      map((response) => mapRewardProfileToItem(response.profile, response.kpiStats)),
    );
  }

  createReward(input: RewardWriteInput): Observable<RewardProfileDto> {
    return this.http.post<RewardProfileDto>(REWARDS_API, buildRewardFormData(input));
  }

  updateReward(id: number | string, input: RewardWriteInput): Observable<RewardProfileDto> {
    return this.http.put<RewardProfileDto>(`${REWARDS_API}/${id}`, buildRewardFormData(input));
  }

  deleteReward(id: string | number): Observable<void> {
    return this.http.delete<void>(`${REWARDS_API}/${id}`);
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
