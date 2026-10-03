import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import type { Observable } from 'rxjs';
import { map } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import type {
  ApplyTemplateRequest,
  ApplyTemplateResult,
  AssignBlockRequest,
  AssignBlockResult,
  BlockRemovalAction,
  BoardBlock,
  CalendarDay,
  CreateShiftBlockRequest,
  CopyCommitRequest,
  CopyCommitResult,
  CopyPreflightRequest,
  CopyPreflightResult,
  DailySchedule,
  DailyScheduleDto,
  DailyShiftBlockDto,
  DraftShift,
  DraftValidationResult,
  PublishCommitRequest,
  PublishCommitResult,
  PublishPreflightRequest,
  PublishPreflightResult,
  RemoveBlockResult,
  SaveAsTemplateResult,
  ShiftCandidatePreview,
  SiteShiftOverviewCard,
  SiteShiftsOverviewFilter,
  SiteShiftsOverviewPage,
  SiteSmartSettingsUpdate,
  WeekDayStatus,
} from '../models/shift-management.models';
import { parseIsoMinutes } from '../../shift-templates/shift-time.utils';

const API_BASE = environment.baseUrl;

const STATUS_BY_INDEX = ['Covered', 'Shortage', 'OvertimeRisk', 'UnqualifiedAssignment'] as const;

const WEEK_STATUS_BY_INDEX = [
  'CoveredAndPublished',
  'MissingResourcesOrUnpublished',
  'OvertimeOrMisallocation',
  'NoAllocations',
  'DimmedDayOff',
] as const;

/** Ticket A: site-coverage overview, smart settings, candidate preview. */
@Injectable({ providedIn: 'root' })
export class ShiftManagementService {
  private readonly http = inject(HttpClient);

  getOverview(filter: SiteShiftsOverviewFilter): Observable<SiteShiftsOverviewPage> {
    let params = new HttpParams()
      .set('page', String(filter.page))
      .set('pageSize', String(filter.pageSize));
    if (filter.search?.trim()) params = params.set('search', filter.search.trim());
    if (filter.regionId !== undefined) params = params.set('regionId', String(filter.regionId));
    return this.http
      .get<SiteShiftsOverviewPage>(`${API_BASE}/shifts/overview`, { params })
      .pipe(map((res) => ({ ...res, items: res.items.map(normalizeCard) })));
  }

  updateSmartSettings(siteId: number, dto: SiteSmartSettingsUpdate): Observable<void> {
    return this.http.patch<void>(`${API_BASE}/sites/${siteId}/smart-settings`, dto);
  }

  getCandidatePreview(id: number): Observable<ShiftCandidatePreview> {
    return this.http.get<ShiftCandidatePreview>(`${API_BASE}/shifts/candidates/${id}/preview`);
  }

  getEmployeePreview(employeeId: number): Observable<ShiftCandidatePreview> {
    return this.http.get<ShiftCandidatePreview>(
      `${API_BASE}/shifts/employees/${employeeId}/preview`,
    );
  }

  /** Ticket B: day board. GET /api/v1/shifts/daily, normalized to board shapes. */
  getDailySchedule(siteId: number, date: string): Observable<DailySchedule> {
    const params = new HttpParams().set('siteId', String(siteId)).set('date', date);
    return this.http
      .get<DailyScheduleDto>(`${API_BASE}/shifts/daily`, { params })
      .pipe(map(normalizeSchedule));
  }

  /** Ticket B: POST /api/v1/shifts/blocks. */
  createShiftBlock(req: CreateShiftBlockRequest): Observable<{ shiftId: number }> {
    return this.http.post<{ shiftId: number }>(`${API_BASE}/shifts/blocks`, req);
  }

  /** Ticket B: POST /api/v1/shifts/blocks/{id}/assign. */
  assignBlock(id: number, req: AssignBlockRequest): Observable<AssignBlockResult> {
    return this.http.post<AssignBlockResult>(`${API_BASE}/shifts/blocks/${id}/assign`, req);
  }

  /** Ticket B: DELETE /api/v1/shifts/blocks/{id}/assign?action=&confirmPublishedDeletion=. */
  removeBlock(
    id: number,
    action: BlockRemovalAction,
    confirmPublishedDeletion: boolean,
  ): Observable<RemoveBlockResult> {
    const params = new HttpParams()
      .set('action', action)
      .set('confirmPublishedDeletion', String(confirmPublishedDeletion));
    return this.http.delete<RemoveBlockResult>(`${API_BASE}/shifts/blocks/${id}/assign`, {
      params,
    });
  }

  /** Ticket C: POST /api/v1/sites/{siteId}/dates/{date}/apply-template. */
  applyTemplate(
    siteId: number,
    date: string,
    req: ApplyTemplateRequest,
  ): Observable<ApplyTemplateResult> {
    return this.http.post<ApplyTemplateResult>(
      `${API_BASE}/sites/${siteId}/dates/${date}/apply-template`,
      { templateId: req.templateId, keep: req.keep },
    );
  }

  /** Ticket #421: POST /api/v1/sites/{siteId}/dates/{date}/save-as-template. */
  saveAsTemplate(siteId: number, date: string, name: string | null): Observable<SaveAsTemplateResult> {
    return this.http.post<SaveAsTemplateResult>(
      `${API_BASE}/sites/${siteId}/dates/${date}/save-as-template`,
      { name },
    );
  }

  /** Ticket #420: POST /api/v1/shifts/publish/preflight. */
  publishPreflight(req: PublishPreflightRequest): Observable<PublishPreflightResult> {
    return this.http.post<PublishPreflightResult>(`${API_BASE}/shifts/publish/preflight`, req);
  }

  /** Ticket #420: POST /api/v1/shifts/publish/commit. */
  publishCommit(req: PublishCommitRequest): Observable<PublishCommitResult> {
    return this.http.post<PublishCommitResult>(`${API_BASE}/shifts/publish/commit`, req);
  }

  /** Ticket #422: POST /api/v1/shifts/copy/preflight. */
  copyPreflight(req: CopyPreflightRequest): Observable<CopyPreflightResult> {
    return this.http.post<CopyPreflightResult>(`${API_BASE}/shifts/copy/preflight`, req);
  }

  /** Ticket #422: POST /api/v1/shifts/copy/commit. */
  copyCommit(req: CopyCommitRequest): Observable<CopyCommitResult> {
    return this.http.post<CopyCommitResult>(`${API_BASE}/shifts/copy/commit`, req);
  }

  /** Ticket #423: POST /api/v1/schedules/validate-draft. */
  validateDraft(shifts: DraftShift[]): Observable<DraftValidationResult> {
    return this.http.post<DraftValidationResult>(`${API_BASE}/schedules/validate-draft`, shifts);
  }
}

function normalizeCard(card: SiteShiftOverviewCard): SiteShiftOverviewCard {
  if (typeof card.status === 'number') {
    return { ...card, status: STATUS_BY_INDEX[card.status] ?? 'Covered' };
  }
  return card;
}

function normalizeDayStatus(status: WeekDayStatus | number): WeekDayStatus {
  if (typeof status === 'number') {
    return WEEK_STATUS_BY_INDEX[status] ?? 'NoAllocations';
  }
  return status;
}

function normalizeBlock(block: DailyShiftBlockDto): BoardBlock | null {
  const startMin = parseIsoMinutes(block.startTime);
  const endMin = parseIsoMinutes(block.endTime);
  if (startMin === null || endMin === null) return null;
  return {
    shiftId: block.shiftId,
    jobRoleId: block.jobRoleId,
    roleTitle: block.roleTitle,
    startMin,
    endMin,
    isPublished: block.isPublished,
    employeeId: block.employeeId,
    employeeName: block.employeeName,
    statusColor: block.statusColorCode,
  };
}

function normalizeSchedule(dto: DailyScheduleDto): DailySchedule {
  const week: CalendarDay[] = dto.weekCalendarStrip.map((d) => ({
    ...d,
    status: normalizeDayStatus(d.status),
  }));
  const blocks: BoardBlock[] = dto.hourlyTimeline
    .flatMap((interval) => interval.blocks)
    .map(normalizeBlock)
    .filter((b): b is BoardBlock => b !== null)
    .sort((a, b) => a.startMin - b.startMin);
  return {
    siteId: dto.siteId,
    siteName: dto.siteName,
    date: dto.date,
    isDayOff: dto.isDayOff,
    totalCost: dto.totalEstimatedLaborCost,
    week,
    blocks,
  };
}
