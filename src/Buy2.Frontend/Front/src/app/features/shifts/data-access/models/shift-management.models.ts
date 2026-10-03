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

/** Shift Management board (ticket B). Backend: GET /api/v1/shifts/daily and block endpoints. */

export type WeekDayStatus =
  | 'CoveredAndPublished'
  | 'MissingResourcesOrUnpublished'
  | 'OvertimeOrMisallocation'
  | 'NoAllocations'
  | 'DimmedDayOff';

/** Raw calendar day; Status may arrive numeric (no string-enum converter). */
export interface CalendarDayDto {
  date: string;
  dayOfWeek: number;
  status: WeekDayStatus | number;
  isSelected: boolean;
  isDayOff: boolean;
}

export interface CalendarDay extends Omit<CalendarDayDto, 'status'> {
  status: WeekDayStatus;
}

export interface DailyShiftBlockDto {
  shiftId: number;
  siteId: number;
  jobRoleId: number;
  roleTitle: string;
  startTime: string;
  endTime: string;
  isPublished: boolean;
  employeeId: number | null;
  employeeName: string | null;
  employeeAvatarUrl: string | null;
  statusColorCode: string | null;
}

export interface TimelineIntervalDto {
  startHour: string;
  endHour: string;
  blocks: DailyShiftBlockDto[];
}

export interface DailyScheduleDto {
  siteId: number;
  siteName: string;
  date: string;
  isDayOff: boolean;
  totalEstimatedLaborCost: number;
  weekCalendarStrip: CalendarDayDto[];
  hourlyTimeline: TimelineIntervalDto[];
}

/** Board-ready block: times resolved to minutes since midnight. */
export interface BoardBlock {
  shiftId: number;
  jobRoleId: number;
  roleTitle: string;
  startMin: number;
  endMin: number;
  isPublished: boolean;
  employeeId: number | null;
  employeeName: string | null;
  statusColor: string | null;
}

export interface DailySchedule {
  siteId: number;
  siteName: string;
  date: string;
  isDayOff: boolean;
  totalCost: number;
  week: CalendarDay[];
  blocks: BoardBlock[];
}

/** POST /api/v1/shifts/blocks body (DateOnly/TimeOnly as strings). */
export interface CreateShiftBlockRequest {
  siteId: number;
  date: string;
  startTime: string;
  endTime: string;
  jobRoleId: number;
  dispatchPolicy: number;
}

/** POST /api/v1/shifts/blocks/{id}/assign body. */
export interface AssignBlockRequest {
  employeeId: number;
  confirmOverride?: boolean;
  overrideReason?: string | null;
}

export interface ConflictWarning {
  conflictType: string | number;
  message: string;
  details?: string | null;
}

export interface AssignBlockResult {
  success: boolean;
  hasConflicts: boolean;
  warnings: ConflictWarning[];
  wasOverridden: boolean;
  updatedCost: number;
}

export type BlockRemovalAction = 'UnassignOnly' | 'DeleteBlock';

export interface RemoveBlockResult {
  shiftBlockId: number;
  actionTaken: string | number;
  isDeleted: boolean;
  updatedCost: number;
  siteCoverageStatus: string | number;
}

/** Ticket C: POST /api/v1/sites/{siteId}/dates/{date}/apply-template body. */
export type TemplateKeepMode = 'existing' | 'new';

export interface ApplyTemplateRequest {
  templateId: number;
  keep: TemplateKeepMode;
}

export interface ApplyTemplateWarning {
  employeeId: number | null;
  employeeName: string;
  role: string;
  reason: string;
  code: string;
}

export interface ApplyTemplateResult {
  siteId: number;
  date: string;
  templateId: number;
  warnings: ApplyTemplateWarning[];
  totalLaborCost: number;
  regularCost: number;
  overtimeCost: number;
  prunedCount: number;
}

/** Ticket #421: POST /api/v1/sites/{siteId}/dates/{date}/save-as-template. */
export interface SaveAsTemplateResult {
  id: number;
  name: string;
  totalBlockCount: number;
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

/** Ticket #420: POST /api/v1/shifts/publish/preflight + /commit. */
export interface PublishPreflightRequest {
  siteId: number;
  targetDates?: string[];
  allUnpublishedDays?: boolean;
  targetRoleIds?: number[];
}

export interface UnqualifiedAssignee {
  shiftId: number;
  employeeId: number;
  employeeName: string;
  requiredRoleId: number;
  requiredRoleTitle: string;
  employeeRoleId: number;
  employeeRoleTitle: string;
  date: string;
  start: string;
  end: string;
  formattedTime: string;
}

export interface OvertimeViolation {
  shiftId: number;
  employeeId: number;
  employeeName: string;
  requiredRoleId: number;
  requiredRoleTitle: string;
  employeeRoleId: number;
  employeeRoleTitle: string;
  date: string;
  start: string;
  end: string;
  formattedTime: string;
  shiftHours: number;
  totalWeeklyHours: number;
  projectedOvertimeHours: number;
  violationReason: string;
}

export interface PublishPreflightResult {
  siteId: number;
  totalUnpublishedShiftsScanned: number;
  totalDatesScanned: number;
  scannedDates: string[];
  unqualifiedAssignees: UnqualifiedAssignee[];
  overtimeViolations: OvertimeViolation[];
  unqualifiedCount: number;
  overtimeCount: number;
  hasExceptions: boolean;
  canPublishImmediately: boolean;
}

/** 1 = Publish, 2 = Skip (backend PublishConflictResolution enum). */
export type PublishResolution = 1 | 2;

export interface PublishCommitRequest extends PublishPreflightRequest {
  exceptionResolutions?: Record<number, PublishResolution>;
  overtimeJustification?: string;
}

export interface PublishCommitResult {
  success: boolean;
  publishedImmediatelyCount: number;
  pendingHrApprovalCount: number;
  skippedCount: number;
  ids: number[];
  message: string;
}
