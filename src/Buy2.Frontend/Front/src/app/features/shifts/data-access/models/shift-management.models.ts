/** Shift Management (ticket A) shapes. Backend: GET /api/v1/shifts/overview. */

export type ShiftCoverageStatus = 'Covered' | 'Shortage' | 'OvertimeRisk' | 'UnqualifiedAssignment';

/** Raw card as the backend sends it; Status may arrive numeric (no string-enum converter). */
export interface SiteShiftOverviewCardDto {
  siteId: number;
  siteName: string;
  address: string;
  regionId: number;
  regionName: string;
  totalShifts: number;
  openShifts: number;
  filledShifts: number;
  status: ShiftCoverageStatus | number;
  isSmartAssignmentEnabled: boolean;
  isSmartPostingEnabled: boolean;
}

export interface SiteShiftOverviewCard extends Omit<SiteShiftOverviewCardDto, 'status'> {
  status: ShiftCoverageStatus;
}

export interface SiteShiftsOverviewPage {
  items: SiteShiftOverviewCard[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface SiteShiftsOverviewFilter {
  search?: string;
  regionId?: number;
  page: number;
  pageSize: number;
}

/** PATCH /api/v1/sites/{id}/smart-settings body. */
export interface SiteSmartSettingsUpdate {
  isSmartAssignmentEnabled: boolean;
  isSmartPostingEnabled: boolean;
}

/** GET /api/v1/shifts/candidates/{id}/preview (ShiftCandidatePreviewDto, camelCase). */
export interface ShiftCandidatePreview {
  id: number;
  employeeCode: string;
  fullName: string;
  avatarUrl: string | null;
  jobTitle: string;
  joinDate: string;
  hourlyRate: number;
  rating: number;
  careerCompletedHours: number;
  qualifications: string[];
}
