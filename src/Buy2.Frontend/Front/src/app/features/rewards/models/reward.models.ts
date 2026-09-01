export type RewardStatus = 'Active' | 'Inactive';

export interface RewardCategory {
  id: string;
  name: string;
}

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

export type CreateRewardDto = Omit<RewardItem, 'id'>;

export interface RewardRedemption {
  id: string;
  rewardItemId: string | number;
  employeeId: number;
  voucherCode: string;
  redeemedAt: string;
  createdAt: string;
}

export interface RewardListRow extends RewardItem {
  redemptionCount: number;
}

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
