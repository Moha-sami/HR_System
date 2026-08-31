// ─── Site List ────────────────────────────────────────────────────────────────
export interface SiteListItemDto {
  id: number;
  siteName: string;
  address?: string;
  regionName?: string;
  regionId?: number;
  latitude?: number;
  longitude?: number;
  phoneNumber?: string;
  maxCapacity?: number;
}

// ─── Operational Hours ─────────────────────────────────────────────────────────
export interface SiteOperationalHourDto {
  day: number; // 0=Sunday ... 6=Saturday
  isOpen: boolean;
  from: string; // "HH:mm"
  to: string;   // "HH:mm"
}

// ─── Create Site ───────────────────────────────────────────────────────────────
export interface CreateSiteDto {
  siteName: string;
  latitude: number;
  longitude: number;
  macWhitelist?: string[];
  macAddress?: string;
  address?: string;
  mapUrl?: string;
  phoneNumber?: string;
  instructions?: string;
  regionId: number;
  maxCapacity?: number;
  preferredEmployeeIds?: number[];
  operationalHours: SiteOperationalHourDto[];
}

// ─── Delete Site ───────────────────────────────────────────────────────────────
export interface EmployeeSiteReassignmentDto {
  employeeId: number;
  newSiteId: number;
}

export interface DeleteSiteDto {
  employeeSiteReassignments: EmployeeSiteReassignmentDto[];
}

// ─── Region ───────────────────────────────────────────────────────────────────
export interface RegionDto {
  id: number;
  name: string;
}
