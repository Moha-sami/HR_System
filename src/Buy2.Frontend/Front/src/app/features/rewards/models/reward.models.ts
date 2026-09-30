export type RewardStatus = 'Active' | 'Inactive';
export type RewardListStatusFilter = RewardStatus | 'All' | '';
export type RewardSortBy = 'name' | 'points' | 'cost' | 'price' | 'redemptioncount';

export interface RewardCategory {
  id: string;
  name: string;
}

export interface RewardCategoryOption {
  readonly id: number;
  readonly name: string;
}

export const SEEDED_REWARD_CATEGORIES: readonly RewardCategoryOption[] = [
  { id: 1, name: 'Gift Cards' },
  { id: 2, name: 'Electronics & Tech Gadgets' },
  { id: 3, name: 'Food & Dining' },
  { id: 4, name: 'Health & Wellness' },
  { id: 5, name: 'Travel & Experiences' },
  { id: 6, name: 'Company Swag' },
];

export interface RewardListFilter {
  readonly page: number;
  readonly pageSize: number;
  readonly search?: string | null;
  readonly status?: RewardListStatusFilter | null;
  readonly fromDate?: string | null;
  readonly toDate?: string | null;
  readonly sortBy?: RewardSortBy | null;
  readonly sortDescending?: boolean;
}

export interface RewardListDto {
  readonly id: number;
  readonly name: string;
  readonly category: string;
  readonly points: number;
  readonly monetaryValue: number;
  readonly stockRatio: string;
  readonly redemptionCount: number;
  readonly isActive: boolean;
}

export interface PaginatedRewards {
  readonly items: readonly RewardListDto[];
  readonly totalCount: number;
  readonly page: number;
  readonly pageSize: number;
}

export interface RewardProfileDto {
  readonly id: number;
  readonly name: string;
  readonly description: string | null;
  readonly imageUrl: string | null;
  readonly category: string;
  readonly points: number;
  readonly monetaryValue: number;
  readonly howToRedeem: string;
  readonly termsOfUse: string;
  readonly isActive: boolean;
}

export interface RewardKpiStatistics {
  readonly redemptionCount: number;
  readonly availableStock: string;
  readonly totalCost: number;
  readonly topRedeemed: number;
  readonly pointsValue: number;
}

export interface RewardProfileResponseDto {
  readonly profile: RewardProfileDto;
  readonly kpiStats: RewardKpiStatistics;
}

export interface RewardWriteInput {
  readonly name: string;
  readonly description: string;
  readonly categoryId: number;
  readonly points: number;
  readonly monetaryValue: number;
  readonly howToRedeem: string;
  readonly termsOfUse: string;
  readonly imageFile?: File | null;
}

export type CreateRewardApiInput = RewardWriteInput;

export interface RewardItem {
  id: string;
  name: string;
  description: string;
  category: string;
  imageUrl: string;
  cost: number;
  price: number;
  pointsValue: number;
  howToRedeem: string;
  termsOfUse: string;
  status: RewardStatus;
  availableStock: number;
  createdAt: string;
}

export function mapRewardProfileToItem(
  profile: RewardProfileDto,
  kpi?: RewardKpiStatistics | null,
): RewardItem {
  return {
    id: String(profile.id),
    name: profile.name,
    description: profile.description ?? '',
    category: profile.category,
    imageUrl: profile.imageUrl ?? '',
    cost: kpi?.totalCost ?? 0,
    price: profile.monetaryValue,
    pointsValue: profile.points,
    howToRedeem: profile.howToRedeem,
    termsOfUse: profile.termsOfUse,
    status: profile.isActive ? 'Active' : 'Inactive',
    availableStock: firstStockCount(kpi?.availableStock),
    createdAt: '',
  };
}

function firstStockCount(availableStock?: string): number {
  if (!availableStock) {
    return 0;
  }
  const value = Number(availableStock.split('/')[0]);
  return Number.isFinite(value) ? value : 0;
}

export type CreateRewardDto = Omit<RewardItem, 'id'>;

export type RewardListRow = RewardListDto;

export type InventoryStatus = 'Available' | 'Redeemed' | 'Expired';

export interface RewardInventoryItem {
  id: string;
  batchId: string;
  voucherCode: string;
  status: InventoryStatus;
  createdAt: string;
}

export interface RewardInventoryListDto {
  readonly id: number;
  readonly batchId: number;
  readonly date: string;
  readonly voucherCode: string;
  readonly status: InventoryStatus | number;
}

export interface PageResultDto<T> {
  readonly items: readonly T[];
  readonly totalCount: number;
  readonly page: number;
  readonly pageSize: number;
}

export interface PaginatedVouchersResponseDto {
  readonly vouchers: PageResultDto<RewardInventoryListDto>;
  readonly availableCount: number;
  readonly redeemedCount: number;
  readonly expiredCount: number;
}

export interface VoucherInventoryFilter {
  readonly page: number;
  readonly pageSize: number;
  readonly voucherCode?: string | null;
  readonly batchId?: number | null;
  readonly status?: InventoryStatus | 'All' | '' | null;
  readonly dateFrom?: string | null;
  readonly dateTo?: string | null;
}

export interface RewardInventoryPage {
  readonly items: RewardInventoryItem[];
  readonly totalCount: number;
  readonly page: number;
  readonly pageSize: number;
  readonly availableCount: number;
  readonly redeemedCount: number;
  readonly expiredCount: number;
}

export interface VoucherUploadPreviewItemDto {
  readonly voucherCode: string;
  readonly isValid: boolean;
  readonly error: string | null;
}

export interface VoucherUploadPreviewDto {
  readonly batchId: number;
  readonly totalFound: number;
  readonly validCount: number;
  readonly duplicateInFileCount: number;
  readonly duplicateInDbCount: number;
  readonly preview: readonly VoucherUploadPreviewItemDto[];
}

export interface VoucherUploadResultDto {
  readonly batchId: number;
  readonly totalUploaded: number;
  readonly availableStock: number;
  readonly expiryDate: string | null;
}

export interface UploadVouchersResponseDto {
  readonly preview: VoucherUploadPreviewDto | null;
  readonly result: VoucherUploadResultDto | null;
}

export interface BatchDeleteVouchersResultDto {
  readonly deletedCount: number;
  readonly skippedCount: number;
  readonly message: string;
}

export interface RewardAnalyticsFilter {
  readonly dateFrom?: string | null;
  readonly dateTo?: string | null;
  readonly timelinePeriod?: 'Weekly' | 'Monthly';
  readonly searchTerm?: string | null;
  readonly pageNumber: number;
  readonly pageSize: number;
}

export interface RedemptionTimelinePointDto {
  readonly periodLabel: string;
  readonly dateFrom: string;
  readonly dateTo: string;
  readonly redemptionCount: number;
  readonly totalPointsSpent: number;
}

export interface RewardTransactionItemDto {
  readonly id: number;
  readonly employeeId: number;
  readonly employeeName: string;
  readonly employeeCode: string;
  readonly departmentName: string;
  readonly voucherCode: string;
  readonly redeemedAt: string;
  readonly time: string;
  readonly pointsDeducted: number;
}

export interface RewardAnalyticsDto {
  readonly timeline: readonly RedemptionTimelinePointDto[];
  readonly transactions: readonly RewardTransactionItemDto[];
  readonly totalCount: number;
  readonly pageNumber: number;
  readonly pageSize: number;
  readonly totalPages: number;
}

export interface UploadBatchPreview {
  clientId: string;
  file: File;
  fileName: string;
  batchId: string;
  totalFound: number;
  validCount: number;
  duplicateCount: number;
  selected: boolean;
  error: string | null;
}

export function mapInventoryStatus(status: InventoryStatus | number | string): InventoryStatus {
  if (status === 1 || status === 'Available') {
    return 'Available';
  }
  if (status === 2 || status === 'Redeemed') {
    return 'Redeemed';
  }
  return 'Expired';
}

export function mapInventoryItem(dto: RewardInventoryListDto): RewardInventoryItem {
  return {
    id: String(dto.id),
    batchId: String(dto.batchId),
    voucherCode: dto.voucherCode,
    status: mapInventoryStatus(dto.status),
    createdAt: dto.date,
  };
}

export function mapInventoryPage(response: PaginatedVouchersResponseDto): RewardInventoryPage {
  const vouchers = response.vouchers;
  return {
    items: (vouchers?.items ?? []).map(mapInventoryItem),
    totalCount: vouchers?.totalCount ?? 0,
    page: vouchers?.page ?? 1,
    pageSize: vouchers?.pageSize ?? 10,
    availableCount: response.availableCount,
    redeemedCount: response.redeemedCount,
    expiredCount: response.expiredCount,
  };
}
