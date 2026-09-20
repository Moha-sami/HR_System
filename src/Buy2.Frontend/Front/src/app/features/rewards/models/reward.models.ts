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

export interface RewardRedemption {
  id: string;
  rewardItemId: string | number;
  employeeId: number;
  voucherCode: string;
  redeemedAt: string;
  createdAt: string;
}

export type RewardListRow = RewardListDto;

export type InventoryStatus = 'Available' | 'Redeemed';

export interface RewardInventoryItem {
  id: string;
  rewardItemId: string | number;
  batchId: string;
  fileName: string;
  voucherCode: string;
  status: InventoryStatus;
  createdAt: string;
  redeemedAt: string | null;
  employeeId: string | number | null;
}

export type CreateInventoryDto = Omit<RewardInventoryItem, 'id'>;

export interface EmployeeName {
  id: string;
  firstName: string;
  lastName: string;
}

export interface UploadBatchPreview {
  clientId: string;
  batchId: string;
  fileName: string;
  codes: string[];
  selected: boolean;
}
