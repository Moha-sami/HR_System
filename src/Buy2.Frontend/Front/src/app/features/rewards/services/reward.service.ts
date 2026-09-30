import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';
import {
  mapInventoryPage,
  mapRewardProfileToItem,
  type BatchDeleteVouchersResultDto,
  type PaginatedRewards,
  type PaginatedVouchersResponseDto,
  type RewardAnalyticsDto,
  type RewardAnalyticsFilter,
  type RewardInventoryPage,
  type RewardItem,
  type RewardListFilter,
  type RewardProfileDto,
  type RewardProfileResponseDto,
  type RewardWriteInput,
  type UploadVouchersResponseDto,
  type VoucherInventoryFilter,
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

export function buildInventoryParams(filter: VoucherInventoryFilter): HttpParams {
  let params = new HttpParams()
    .set('page', filter.page.toString())
    .set('pageSize', filter.pageSize.toString());

  if (filter.voucherCode?.trim()) {
    params = params.set('voucherCode', filter.voucherCode.trim());
  }
  if (filter.batchId != null) {
    params = params.set('batchId', String(filter.batchId));
  }
  if (filter.status && filter.status !== 'All') {
    params = params.set('status', filter.status);
  }
  if (filter.dateFrom) {
    params = params.set('dateFrom', filter.dateFrom);
  }
  if (filter.dateTo) {
    params = params.set('dateTo', filter.dateTo);
  }

  return params;
}

export function buildAnalyticsParams(filter: RewardAnalyticsFilter): HttpParams {
  let params = new HttpParams()
    .set('pageNumber', filter.pageNumber.toString())
    .set('pageSize', filter.pageSize.toString());

  if (filter.dateFrom) {
    params = params.set('dateFrom', filter.dateFrom);
  }
  if (filter.dateTo) {
    params = params.set('dateTo', filter.dateTo);
  }
  if (filter.timelinePeriod) {
    params = params.set('timelinePeriod', filter.timelinePeriod);
  }
  if (filter.searchTerm?.trim()) {
    params = params.set('searchTerm', filter.searchTerm.trim());
  }

  return params;
}

export function buildVoucherUploadFormData(
  file: File,
  confirm: boolean,
  batchId?: string | null,
): FormData {
  const formData = new FormData();
  formData.append('file', file, file.name);
  formData.append('confirm', String(confirm));
  if (batchId) {
    formData.append('batchId', batchId);
  }
  return formData;
}

@Injectable({
  providedIn: 'root',
})
export class RewardService {
  private readonly http = inject(HttpClient);

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

  getInventory(
    rewardId: number | string,
    filter: VoucherInventoryFilter,
  ): Observable<RewardInventoryPage> {
    return this.http
      .get<PaginatedVouchersResponseDto>(`${REWARDS_API}/${rewardId}/inventory`, {
        params: buildInventoryParams(filter),
      })
      .pipe(map(mapInventoryPage));
  }

  previewInventoryUpload(
    rewardId: number | string,
    file: File,
  ): Observable<UploadVouchersResponseDto> {
    return this.http.post<UploadVouchersResponseDto>(
      `${REWARDS_API}/${rewardId}/inventory/upload`,
      buildVoucherUploadFormData(file, false),
    );
  }

  confirmInventoryUpload(
    rewardId: number | string,
    file: File,
    batchId: string,
  ): Observable<UploadVouchersResponseDto> {
    return this.http.post<UploadVouchersResponseDto>(
      `${REWARDS_API}/${rewardId}/inventory/upload`,
      buildVoucherUploadFormData(file, true, batchId),
    );
  }

  deleteInventoryBatch(
    rewardId: number | string,
    voucherIds: readonly number[],
  ): Observable<BatchDeleteVouchersResultDto> {
    return this.http.post<BatchDeleteVouchersResultDto>(
      `${REWARDS_API}/${rewardId}/inventory/delete-batch`,
      voucherIds,
    );
  }

  getAnalytics(
    rewardId: number | string,
    filter: RewardAnalyticsFilter,
  ): Observable<RewardAnalyticsDto> {
    return this.http.get<RewardAnalyticsDto>(`${REWARDS_API}/${rewardId}/analytics`, {
      params: buildAnalyticsParams(filter),
    });
  }
}
