import {
  Component,
  computed,
  HostListener,
  inject,
  signal,
  type OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { forkJoin, Subject, takeUntil } from 'rxjs';
import { ShiftsLookupsService } from '../../data-access/services/shifts-lookups.service';
import { ShiftTemplateService } from '../../data-access/services/shift-template.service';
import type {
  ShiftsJobRoleLookup,
  ShiftsSiteEmployee,
  ShiftsSiteLookup,
} from '../../data-access/models/shifts-lookups.models';
import type {
  CreateShiftTemplateRequest,
  UpdateShiftTemplateRequest,
} from '../../data-access/models/shift-template.models';
import { ButtonComponent } from '@app/shared/components/button/button.component';
import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { ModalBodyComponent } from '@app/shared/components/modal/modal-body.component';
import {
  formatBackendTime,
  parseBackendTime,
  parseTimeInput,
  rangesOverlap,
  splitTime,
  type Meridiem,
} from '../shift-time.utils';

export interface WorkingBlock {
  clientId: number;
  /** 0 = new block, >0 = existing block (PUT full-replace by id). */
  id: number;
  start: number;
  end: number;
  jobRoleId: number;
  jobRoleTitle: string;
  assignedUserId: number | null;
  assignedUserName: string | null;
}

/**
 * Ticket #327: shared create/edit form with a block builder row.
 * Inline validation mirrors every server rule; timeline (#328) and the
 * employee strip with drag-and-drop (#329) land in later tickets.
 */
@Component({
  selector: 'app-shift-template-editor',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslatePipe,
    ButtonComponent,
    ModalComponent,
    ModalBodyComponent,
  ],
  templateUrl: './shift-template-editor.component.html',
})
export class ShiftTemplateEditorComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);
  private readonly templates = inject(ShiftTemplateService);
  private readonly lookups = inject(ShiftsLookupsService);
  private readonly destroy$ = new Subject<void>();

  private nextClientId = 1;

  // ── Mode ────────────────────────────────────────────────────────────────
  readonly templateId = signal<number | null>(null);
  readonly isEdit = computed(() => this.templateId() !== null);
  readonly loading = signal(false);
  readonly loadError = signal(false);

  // ── Form state ──────────────────────────────────────────────────────────
  readonly name = signal('');
  readonly selectedSiteIds = signal<number[]>([]);
  readonly startHm = signal('09:00');
  readonly startMer = signal<Meridiem>('AM');
  readonly endHm = signal('05:00');
  readonly endMer = signal<Meridiem>('PM');

  // ── Lookups ─────────────────────────────────────────────────────────────
  readonly sites = signal<ShiftsSiteLookup[]>([]);
  readonly roles = signal<ShiftsJobRoleLookup[]>([]);
  readonly employees = signal<ShiftsSiteEmployee[]>([]);

  // ── Working blocks ──────────────────────────────────────────────────────
  readonly blocks = signal<WorkingBlock[]>([]);

  // ── Builder row ─────────────────────────────────────────────────────────
  readonly rowStartHm = signal('09:00');
  readonly rowStartMer = signal<Meridiem>('AM');
  readonly rowEndHm = signal('10:00');
  readonly rowEndMer = signal<Meridiem>('AM');
  readonly rowRoleId = signal<number | null>(null);
  readonly rowEmployeeId = signal<number | null>(null);
  readonly editingClientId = signal<number | null>(null);
  readonly rowError = signal<string | null>(null);

  // ── Sites dropdown ──────────────────────────────────────────────────────
  readonly sitesOpen = signal(false);

  // ── Delete-block modal ──────────────────────────────────────────────────
  readonly showDeleteBlockModal = signal(false);
  readonly deletingBlock = signal<WorkingBlock | null>(null);

  // ── Save ────────────────────────────────────────────────────────────────
  readonly formErrors = signal<string[]>([]);
  readonly saveError = signal<string | null>(null);
  readonly saving = signal(false);

  readonly sitesLabel = computed(() => {
    const ids = this.selectedSiteIds();
    if (ids.length === 0) return this.translate.instant('SHIFT_TEMPLATES.EDITOR.SITES_PLACEHOLDER');
    const names = this.sites()
      .filter((s) => ids.includes(s.id))
      .map((s) => s.siteName);
    return names.length > 0 ? names.join(', ') : this.translate.instant('SHIFT_TEMPLATES.EDITOR.SITES_PLACEHOLDER');
  });

  ngOnInit(): void {
    this.loadLookups();
    const idParam = this.route.snapshot.paramMap.get('id');
    const id = idParam !== null ? Number(idParam) : NaN;
    if (idParam !== null && Number.isInteger(id) && id > 0) {
      this.templateId.set(id);
      this.loadTemplate(id);
    }
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    this.sitesOpen.set(false);
  }

  // ── Lookups ─────────────────────────────────────────────────────────────
  private loadLookups(): void {
    this.lookups
      .getSites()
      .pipe(takeUntil(this.destroy$))
      .subscribe({ next: (sites) => this.sites.set(sites), error: () => {} });
    this.lookups
      .getJobRoles()
      .pipe(takeUntil(this.destroy$))
      .subscribe({ next: (roles) => this.roles.set(roles), error: () => {} });
  }

  reloadEmployees(): void {
    const ids = this.selectedSiteIds();
    if (ids.length === 0) {
      this.employees.set([]);
      return;
    }
    forkJoin(ids.map((id) => this.lookups.getSiteEmployees(id)))
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (pages) => {
          const seen = new Map<number, ShiftsSiteEmployee>();
          for (const page of pages) for (const emp of page) seen.set(emp.employeeId, emp);
          this.employees.set([...seen.values()]);
        },
        error: () => {},
      });
  }

  // ── Edit prefill ────────────────────────────────────────────────────────
  private loadTemplate(id: number): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.templates
      .getTemplateById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (details) => {
          this.name.set(details.name);
          this.selectedSiteIds.set(details.sites.map((s) => s.siteId));
          const start = parseBackendTime(details.startTime);
          const end = parseBackendTime(details.endTime);
          if (start !== null) {
            const parts = splitTime(start);
            this.startHm.set(`${parts.hour}:${parts.minute}`);
            this.startMer.set(parts.meridiem);
          }
          if (end !== null) {
            const parts = splitTime(end);
            this.endHm.set(`${parts.hour}:${parts.minute}`);
            this.endMer.set(parts.meridiem);
          }
          this.blocks.set(
            details.shiftBlocks.map((b) => {
              const bStart = parseBackendTime(b.startTime) ?? 0;
              const bEnd = parseBackendTime(b.endTime) ?? 0;
              return {
                clientId: this.nextClientId++,
                id: b.id,
                start: bStart,
                end: bEnd,
                jobRoleId: b.jobRoleId,
                jobRoleTitle: b.jobRoleTitle,
                assignedUserId: b.assignedUserId ?? null,
                assignedUserName: b.assignedUserName ?? null,
              };
            }),
          );
          this.reloadEmployees();
          this.loading.set(false);
        },
        error: () => {
          this.loadError.set(true);
          this.loading.set(false);
        },
      });
  }

  // ── Sites multi-select ──────────────────────────────────────────────────
  toggleSitesDropdown(event: Event): void {
    event.stopPropagation();
    this.sitesOpen.update((open) => !open);
  }

  toggleSite(event: Event, siteId: number): void {
    event.stopPropagation();
    this.selectedSiteIds.update((ids) =>
      ids.includes(siteId) ? ids.filter((id) => id !== siteId) : [...ids, siteId],
    );
    this.reloadEmployees();
  }

  // ── Shift range ─────────────────────────────────────────────────────────
  shiftRange(): { start: number; end: number } | null {
    const start = parseTimeInput(this.startHm(), this.startMer());
    const end = parseTimeInput(this.endHm(), this.endMer());
    if (start === null || end === null || end <= start) return null;
    return { start, end };
  }

  // ── Block builder ───────────────────────────────────────────────────────
  postBlock(): void {
    const error = this.validateRow();
    if (error) {
      this.rowError.set(error);
      return;
    }
    this.rowError.set(null);
    const start = parseTimeInput(this.rowStartHm(), this.rowStartMer()) as number;
    const end = parseTimeInput(this.rowEndHm(), this.rowEndMer()) as number;
    const roleId = this.rowRoleId() as number;
    const role = this.roles().find((r) => r.id === roleId);
    const employeeId = this.rowEmployeeId();
    const employee = employeeId !== null ? this.employees().find((e) => e.employeeId === employeeId) : undefined;

    const editing = this.editingClientId();
    if (editing !== null) {
      this.blocks.update((blocks) =>
        blocks.map((b) =>
          b.clientId === editing
            ? {
                ...b,
                start,
                end,
                jobRoleId: roleId,
                jobRoleTitle: role?.title ?? b.jobRoleTitle,
                assignedUserId: employeeId,
                assignedUserName: employee?.fullName ?? null,
              }
            : b,
        ),
      );
    } else {
      this.blocks.update((blocks) => [
        ...blocks,
        {
          clientId: this.nextClientId++,
          id: 0,
          start,
          end,
          jobRoleId: roleId,
          jobRoleTitle: role?.title ?? '',
          assignedUserId: employeeId,
          assignedUserName: employee?.fullName ?? null,
        },
      ]);
    }
    this.resetRow();
  }

  private validateRow(): string | null {
    const t = (key: string): string => this.translate.instant(key);
    const start = parseTimeInput(this.rowStartHm(), this.rowStartMer());
    const end = parseTimeInput(this.rowEndHm(), this.rowEndMer());
    if (start === null || end === null) return t('SHIFT_TEMPLATES.EDITOR.BLOCK_TIME_INVALID');
    if (end <= start) return t('SHIFT_TEMPLATES.EDITOR.BLOCK_START_BEFORE_END');
    const range = this.shiftRange();
    if (!range) return t('SHIFT_TEMPLATES.EDITOR.TIME_INVALID');
    if (start < range.start || end > range.end)
      return t('SHIFT_TEMPLATES.EDITOR.BLOCK_WITHIN_RANGE');
    const editing = this.editingClientId();
    const overlap = this.blocks().some(
      (b) => b.clientId !== editing && rangesOverlap(start, end, b.start, b.end),
    );
    if (overlap) return t('SHIFT_TEMPLATES.EDITOR.BLOCK_OVERLAP');
    if (this.rowRoleId() === null) return t('SHIFT_TEMPLATES.EDITOR.ROLE_REQUIRED');
    const employeeId = this.rowEmployeeId();
    if (
      employeeId !== null &&
      this.blocks().some((b) => b.clientId !== editing && b.assignedUserId === employeeId)
    )
      return t('SHIFT_TEMPLATES.EDITOR.EMPLOYEE_DOUBLE');
    return null;
  }

  editBlock(block: WorkingBlock): void {
    const start = splitTime(block.start);
    const end = splitTime(block.end);
    this.rowStartHm.set(`${start.hour}:${start.minute}`);
    this.rowStartMer.set(start.meridiem);
    this.rowEndHm.set(`${end.hour}:${end.minute}`);
    this.rowEndMer.set(end.meridiem);
    this.rowRoleId.set(block.jobRoleId);
    this.rowEmployeeId.set(block.assignedUserId);
    this.editingClientId.set(block.clientId);
    this.rowError.set(null);
  }

  cancelRowEdit(): void {
    this.resetRow();
  }

  private resetRow(): void {
    this.rowStartHm.set('09:00');
    this.rowStartMer.set('AM');
    this.rowEndHm.set('10:00');
    this.rowEndMer.set('AM');
    this.rowRoleId.set(null);
    this.rowEmployeeId.set(null);
    this.editingClientId.set(null);
    this.rowError.set(null);
  }

  askDeleteBlock(block: WorkingBlock): void {
    this.deletingBlock.set(block);
    this.showDeleteBlockModal.set(true);
  }

  closeDeleteBlockModal(): void {
    this.showDeleteBlockModal.set(false);
    this.deletingBlock.set(null);
  }

  confirmDeleteBlock(): void {
    const block = this.deletingBlock();
    if (!block) return;
    if (this.editingClientId() === block.clientId) this.resetRow();
    this.blocks.update((blocks) => blocks.filter((b) => b.clientId !== block.clientId));
    this.closeDeleteBlockModal();
  }

  blockLabel(block: WorkingBlock): string {
    return `${formatBackendTime(block.start)} - ${formatBackendTime(block.end)}`;
  }

  // ── Save ────────────────────────────────────────────────────────────────
  save(): void {
    const errors = this.validateForm();
    this.formErrors.set(errors);
    if (errors.length > 0) return;
    const range = this.shiftRange() as { start: number; end: number };
    const payload = {
      name: this.name().trim(),
      siteIds: this.selectedSiteIds(),
      startTime: formatBackendTime(range.start),
      endTime: formatBackendTime(range.end),
      shiftBlocks: this.blocks().map((b) => ({
        id: b.id,
        startTime: formatBackendTime(b.start),
        endTime: formatBackendTime(b.end),
        jobRoleId: b.jobRoleId,
        assignedUserId: b.assignedUserId,
      })),
    };
    this.saving.set(true);
    this.saveError.set(null);
    const id = this.templateId();
    const request =
      id === null
        ? this.templates.createTemplate(payload as CreateShiftTemplateRequest)
        : this.templates.updateTemplate(id, payload as UpdateShiftTemplateRequest);
    request.pipe(takeUntil(this.destroy$)).subscribe({
      next: () => this.router.navigate(['/scheduling/shift-templates']),
      error: () => {
        this.saving.set(false);
        this.saveError.set(this.translate.instant('SHIFT_TEMPLATES.EDITOR.SAVE_ERROR'));
      },
    });
  }

  private validateForm(): string[] {
    const t = (key: string): string => this.translate.instant(key);
    const errors: string[] = [];
    if (this.name().trim().length === 0) errors.push(t('SHIFT_TEMPLATES.EDITOR.NAME_REQUIRED'));
    if (this.selectedSiteIds().length === 0) errors.push(t('SHIFT_TEMPLATES.EDITOR.SITES_REQUIRED'));
    const start = parseTimeInput(this.startHm(), this.startMer());
    const end = parseTimeInput(this.endHm(), this.endMer());
    if (start === null || end === null) {
      errors.push(t('SHIFT_TEMPLATES.EDITOR.TIME_INVALID'));
    } else if (end <= start) {
      errors.push(t('SHIFT_TEMPLATES.EDITOR.END_BEFORE_START'));
    }
    if (this.blocks().length === 0) errors.push(t('SHIFT_TEMPLATES.EDITOR.BLOCKS_REQUIRED'));
    return errors;
  }

  discard(): void {
    this.router.navigate(['/scheduling/shift-templates']);
  }
}
