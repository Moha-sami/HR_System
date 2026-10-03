import {
  Component,
  computed,
  inject,
  signal,
  type OnDestroy,
  type OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Subject, takeUntil } from 'rxjs';
import { CdkDropListGroup } from '@angular/cdk/drag-drop';
import { ShiftManagementService } from '../../data-access/services/shift-management.service';
import { ShiftsLookupsService } from '../../data-access/services/shifts-lookups.service';
import type { ShiftsJobRoleLookup } from '../../data-access/models/shifts-lookups.models';
import type {
  BoardBlock,
  ConflictWarning,
  DailySchedule,
  PublishCommitResult,
  PublishPreflightResult,
  PublishResolution,
} from '../../data-access/models/shift-management.models';
import type { ShiftCandidateEmployee } from '../../data-access/models/shifts-lookups.models';
import {
  formatTimeOnly,
  parseTimeInput,
  type Meridiem,
} from '../../shift-templates/shift-time.utils';
import {
  ShiftTimelineComponent,
  type TimelineBlock,
} from '../../ui/shift-timeline/shift-timeline.component';
import { EmployeeStripComponent } from '../../shift-templates/employee-strip/employee-strip.component';
import { ButtonComponent } from '@app/shared/components/button/button.component';
import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { ModalBodyComponent } from '@app/shared/components/modal/modal-body.component';
import { CandidatePreviewComponent } from '../candidate-preview/candidate-preview.component';
import { TemplatePickerComponent } from '../template-picker/template-picker.component';

const WEEK_DOT: Record<string, string> = {
  CoveredAndPublished: 'bg-success-500',
  MissingResourcesOrUnpublished: 'bg-warning-500',
  OvertimeOrMisallocation: 'bg-purple-500',
  NoAllocations: 'bg-neutral-300',
  DimmedDayOff: 'bg-neutral-200',
};

function toISODate(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

function addDays(iso: string, delta: number): string {
  const [y, m, d] = iso.split('-').map(Number);
  const date = new Date(y, m - 1, d);
  date.setDate(date.getDate() + delta);
  return toISODate(date);
}

/**
 * Ticket B: per-site day board. Real week navigation backed by GET
 * /api/v1/shifts/daily, slim block builder, extended timeline kit
 * (status colors, locked published bars), drag-assign plus picker assign
 * with conflict confirm, unassign/delete against the block endpoints.
 */
@Component({
  selector: 'app-shift-board',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslatePipe,
    ButtonComponent,
    ModalComponent,
    ModalBodyComponent,
    EmployeeStripComponent,
    ShiftTimelineComponent,
    CandidatePreviewComponent,
    TemplatePickerComponent,
    CdkDropListGroup,
  ],
  templateUrl: './shift-board.component.html',
})
export class ShiftBoardComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);
  private readonly managementService = inject(ShiftManagementService);
  private readonly lookups = inject(ShiftsLookupsService);
  private readonly destroy$ = new Subject<void>();

  readonly siteId = signal(0);
  readonly schedule = signal<DailySchedule | null>(null);
  readonly selectedDate = signal(toISODate(new Date()));
  readonly loading = signal(false);
  readonly loadError = signal(false);
  readonly actionError = signal<string | null>(null);

  readonly roles = signal<ShiftsJobRoleLookup[]>([]);

  readonly rowStartHm = signal('09:00');
  readonly rowStartMer = signal<Meridiem>('AM');
  readonly rowEndHm = signal('05:00');
  readonly rowEndMer = signal<Meridiem>('PM');
  readonly rowRoleId = signal<number | null>(null);
  readonly rowError = signal<string | null>(null);
  readonly posting = signal(false);

  readonly pendingAssign = signal<number | null>(null);
  readonly previewEmployeeId = signal<number | null>(null);
  readonly showPicker = signal(false);

  readonly showSaveModal = signal(false);
  readonly saveName = signal('');
  readonly saveError = signal<string | null>(null);
  readonly saving = signal(false);
  readonly savedTemplateName = signal<string | null>(null);

  readonly showPublishModal = signal(false);
  readonly preflight = signal<PublishPreflightResult | null>(null);
  readonly justification = signal('');
  readonly publishError = signal<string | null>(null);
  readonly publishing = signal(false);
  readonly publishSuccess = signal<PublishCommitResult | null>(null);

  readonly conflict = signal<{
    shiftId: number;
    employee: ShiftCandidateEmployee;
    warnings: ConflictWarning[];
  } | null>(null);
  readonly overrideReason = signal('');
  readonly assigning = signal(false);

  readonly removeTarget = signal<BoardBlock | null>(null);
  readonly confirmPublished = signal(false);
  readonly removing = signal(false);

  readonly timelineBlocks = computed<TimelineBlock[]>(() =>
    (this.schedule()?.blocks ?? []).map((b) => ({
      id: b.shiftId,
      start: b.startMin,
      end: b.endMin,
      label: b.roleTitle,
      assigned: b.employeeId !== null,
      assigneeName: b.employeeName,
      statusColor: b.statusColor,
      locked: b.isPublished,
    })),
  );

  readonly rangeStart = computed<number | null>(() => {
    const blocks = this.schedule()?.blocks ?? [];
    return blocks.length > 0 ? Math.min(...blocks.map((b) => b.startMin)) : null;
  });

  readonly rangeEnd = computed<number | null>(() => {
    const blocks = this.schedule()?.blocks ?? [];
    return blocks.length > 0 ? Math.max(...blocks.map((b) => b.endMin)) : null;
  });

  readonly assignedIds = computed<number[]>(() =>
    (this.schedule()?.blocks ?? []).flatMap((b) => (b.employeeId !== null ? [b.employeeId] : [])),
  );

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('siteId'));
    this.siteId.set(Number.isFinite(id) ? id : 0);
    this.lookups
      .getJobRoles()
      .pipe(takeUntil(this.destroy$))
      .subscribe({ next: (roles) => this.roles.set(roles), error: () => {} });
    this.loadDay();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  weekDot(status: string): string {
    return WEEK_DOT[status] ?? 'bg-neutral-300';
  }

  dayName(dateIso: string): string {
    const [y, m, d] = dateIso.split('-').map(Number);
    return new Intl.DateTimeFormat(this.translate.currentLang() ?? 'en', { weekday: 'short' }).format(
      new Date(y, m - 1, d),
    );
  }

  dayNumber(dateIso: string): string {
    return String(Number(dateIso.split('-')[2]));
  }

  loadDay(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.pendingAssign.set(null);
    this.managementService
      .getDailySchedule(this.siteId(), this.selectedDate())
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (schedule) => {
          this.schedule.set(schedule);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.loadError.set(true);
        },
      });
  }

  shiftWeek(delta: number): void {
    this.selectedDate.set(addDays(this.selectedDate(), delta));
    this.loadDay();
  }

  goToday(): void {
    this.selectedDate.set(toISODate(new Date()));
    this.loadDay();
  }

  selectDay(dateIso: string): void {
    if (dateIso === this.selectedDate()) return;
    this.selectedDate.set(dateIso);
    this.loadDay();
  }

  backToOverview(): void {
    this.router.navigate(['/scheduling/shift-management']);
  }

  openPicker(): void {
    this.showPicker.set(true);
  }

  closePicker(): void {
    this.showPicker.set(false);
  }

  onPickerApplied(): void {
    this.showPicker.set(false);
    this.loadDay();
  }

  openSaveModal(): void {
    this.saveName.set('');
    this.saveError.set(null);
    this.savedTemplateName.set(null);
    this.showSaveModal.set(true);
  }

  closeSaveModal(): void {
    this.showSaveModal.set(false);
  }

  confirmSave(): void {
    const name = this.saveName().trim();
    this.saveError.set(null);
    this.saving.set(true);
    this.managementService
      .saveAsTemplate(this.siteId(), this.selectedDate(), name ? name : null)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.saving.set(false);
          this.savedTemplateName.set(res.name);
        },
        error: (err: { error?: { message?: string } }) => {
          this.saving.set(false);
          this.saveError.set(err?.error?.message ?? 'SHIFT_MANAGEMENT.SAVE.SAVE_ERROR');
        },
      });
  }

  openPublish(): void {
    this.publishError.set(null);
    this.publishSuccess.set(null);
    this.preflight.set(null);
    this.publishing.set(true);
    this.managementService
      .publishPreflight({ siteId: this.siteId(), targetDates: [this.selectedDate()] })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.publishing.set(false);
          if (!res.hasExceptions) {
            this.commitPublish(res, {});
            return;
          }
          this.preflight.set(res);
          this.justification.set('');
          this.showPublishModal.set(true);
        },
        error: (err: { error?: { message?: string } }) => {
          this.publishing.set(false);
          this.publishError.set(err?.error?.message ?? 'SHIFT_MANAGEMENT.PUBLISH.PUBLISH_ERROR');
        },
      });
  }

  closePublishModal(): void {
    this.showPublishModal.set(false);
  }

  confirmPublish(): void {
    const pre = this.preflight();
    if (pre === null) return;
    const resolutions: Record<number, PublishResolution> = {};
    for (const v of [...pre.unqualifiedAssignees, ...pre.overtimeViolations]) {
      resolutions[v.shiftId] = 1;
    }
    this.commitPublish(pre, resolutions);
  }

  private commitPublish(
    pre: PublishPreflightResult,
    resolutions: Record<number, PublishResolution>,
  ): void {
    const just = this.justification().trim();
    this.publishError.set(null);
    this.publishing.set(true);
    this.managementService
      .publishCommit({
        siteId: pre.siteId,
        targetDates: [...pre.scannedDates],
        ...(Object.keys(resolutions).length > 0 ? { exceptionResolutions: resolutions } : {}),
        ...(just ? { overtimeJustification: just } : {}),
      })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.publishing.set(false);
          this.publishSuccess.set(res);
          this.showPublishModal.set(true);
          this.loadDay();
        },
        error: (err: { error?: { message?: string } }) => {
          this.publishing.set(false);
          this.publishError.set(err?.error?.message ?? 'SHIFT_MANAGEMENT.PUBLISH.PUBLISH_ERROR');
        },
      });
  }

  postBlock(): void {
    this.actionError.set(null);
    const roleId = this.rowRoleId();
    if (roleId === null) {
      this.rowError.set('SHIFT_MANAGEMENT.BOARD.ROLE_REQUIRED');
      return;
    }
    const start = parseTimeInput(this.rowStartHm(), this.rowStartMer());
    const end = parseTimeInput(this.rowEndHm(), this.rowEndMer());
    if (start === null || end === null || start === end) {
      this.rowError.set('SHIFT_MANAGEMENT.BOARD.TIME_INVALID');
      return;
    }
    this.rowError.set(null);
    this.posting.set(true);
    this.managementService
      .createShiftBlock({
        siteId: this.siteId(),
        date: this.selectedDate(),
        startTime: formatTimeOnly(start),
        endTime: formatTimeOnly(end),
        jobRoleId: roleId,
        dispatchPolicy: 0,
      })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.posting.set(false);
          this.loadDay();
        },
        error: () => {
          this.posting.set(false);
          this.rowError.set('SHIFT_MANAGEMENT.BOARD.POST_ERROR');
        },
      });
  }

  onStripDrop(event: { id: string | number; data: unknown }): void {
    const employee = asEmployee(event.data);
    if (employee === null) return;
    this.assignTo(employee, event.id);
  }

  onStripClick(employee: ShiftCandidateEmployee): void {
    const pending = this.pendingAssign();
    if (pending === null) {
      this.previewEmployeeId.set(employee.id);
      return;
    }
    this.pendingAssign.set(null);
    this.assignTo(employee, pending);
  }

  onTimelineAssign(shiftId: string | number): void {
    this.actionError.set(null);
    this.pendingAssign.set(Number(shiftId));
  }

  cancelPendingAssign(): void {
    this.pendingAssign.set(null);
  }

  confirmConflict(): void {
    const pending = this.conflict();
    if (pending === null) return;
    this.conflict.set(null);
    this.assignTo(pending.employee, pending.shiftId, true);
  }

  closeConflict(): void {
    this.conflict.set(null);
  }

  askRemove(block: BoardBlock): void {
    this.confirmPublished.set(false);
    this.removeTarget.set(block);
  }

  onTimelineDelete(shiftId: string | number): void {
    const block = (this.schedule()?.blocks ?? []).find((b) => b.shiftId === Number(shiftId));
    if (block) this.askRemove(block);
  }

  closeRemove(): void {
    this.removeTarget.set(null);
  }

  toggleConfirmPublished(event: Event): void {
    this.confirmPublished.set((event.target as HTMLInputElement).checked);
  }

  unassignBlock(): void {
    const target = this.removeTarget();
    if (target === null) return;
    this.removeTarget.set(null);
    this.remove(target.shiftId, 'UnassignOnly', false);
  }

  deleteBlock(): void {
    const target = this.removeTarget();
    if (target === null) return;
    if (target.isPublished && !this.confirmPublished()) return;
    this.removeTarget.set(null);
    this.remove(target.shiftId, 'DeleteBlock', target.isPublished && this.confirmPublished());
  }

  private assignTo(
    employee: ShiftCandidateEmployee,
    shiftId: string | number,
    confirm = false,
  ): void {
    this.actionError.set(null);
    this.assigning.set(true);
    this.managementService
      .assignBlock(Number(shiftId), {
        employeeId: employee.id,
        confirmOverride: confirm ? true : undefined,
        overrideReason: confirm && this.overrideReason().trim() ? this.overrideReason().trim() : undefined,
      })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.assigning.set(false);
          if (res.hasConflicts && !confirm) {
            this.overrideReason.set('');
            this.conflict.set({ shiftId: Number(shiftId), employee, warnings: res.warnings });
            return;
          }
          this.loadDay();
        },
        error: () => {
          this.assigning.set(false);
          this.actionError.set('SHIFT_MANAGEMENT.BOARD.ASSIGN_ERROR');
        },
      });
  }

  private remove(id: number, action: 'UnassignOnly' | 'DeleteBlock', confirmPublished: boolean): void {
    this.actionError.set(null);
    this.removing.set(true);
    this.managementService
      .removeBlock(id, action, confirmPublished)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.removing.set(false);
          this.loadDay();
        },
        error: () => {
          this.removing.set(false);
          this.actionError.set('SHIFT_MANAGEMENT.BOARD.REMOVE_ERROR');
        },
      });
  }
}

function asEmployee(data: unknown): ShiftCandidateEmployee | null {
  if (typeof data !== 'object' || data === null) return null;
  const candidate = data as { id?: unknown; fullName?: unknown };
  if (typeof candidate.id !== 'number' || typeof candidate.fullName !== 'string') return null;
  return data as ShiftCandidateEmployee;
}
