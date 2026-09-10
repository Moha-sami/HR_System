/**
 * Shift Template contracts — mirrors api/v1/shift-templates DTOs.
 * Time values are 12-hour 'hh:mm tt' strings (backend contract).
 * PUT is full-replace: omitted sites/blocks are removed server-side.
 */

export interface ShiftTemplateFilter {
  searchTerm?: string;
  nameSort?: 'asc' | 'desc';
  creationSort?: 'asc' | 'desc';
  updatedSort?: 'asc' | 'desc';
  numberOfAssignedSort?: 'asc' | 'desc';
  pageNumber?: number;
  pageSize?: number;
}

export interface ShiftTemplateListItem {
  id: number;
  name: string;
  creationDate: string;
  lastUpdated: string;
  numberOfAssignedSites: number;
}

export interface ShiftTemplateListResponse {
  items: ShiftTemplateListItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface ShiftTemplateSiteItem {
  siteId: number;
  siteName: string;
}

export interface ShiftTemplateBlockDetails {
  id: number;
  startTime: string;
  endTime: string;
  jobRoleId: number;
  jobRoleTitle: string;
  assignedUserId?: number | null;
  assignedUserName?: string | null;
}

export interface ShiftTemplateDetails {
  id: number;
  name: string;
  startTime: string;
  endTime: string;
  creationDate: string;
  lastUpdated: string;
  numberOfAssignedSites: number;
  sites: ShiftTemplateSiteItem[];
  shiftBlocks: ShiftTemplateBlockDetails[];
  lastUpdatedByEmployeeId?: number | null;
}

export interface CreateShiftTemplateBlock {
  startTime: string;
  endTime: string;
  jobRoleId: number;
  assignedUserId?: number | null;
}

export interface CreateShiftTemplateRequest {
  name: string;
  siteIds: number[];
  startTime: string;
  endTime: string;
  shiftBlocks: CreateShiftTemplateBlock[];
}

export interface UpdateShiftTemplateBlock extends CreateShiftTemplateBlock {
  /** 0 = new block, >0 = existing block to overwrite. */
  id: number;
}

export interface UpdateShiftTemplateRequest {
  name: string;
  siteIds: number[];
  startTime: string;
  endTime: string;
  shiftBlocks: UpdateShiftTemplateBlock[];
}

export interface DuplicateShiftTemplateResponse {
  id: number;
  name: string;
}
