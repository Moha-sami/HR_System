import {
  Component,
  computed,
  inject,
  OnInit,
  signal,
  type TemplateRef,
  viewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { ButtonComponent } from '../../../../shared/components/button/button.component';
import { ModalComponent } from '../../../../shared/components/modal/modal.component';
import { ModalBodyComponent } from '../../../../shared/components/modal/modal-body.component';
import {
  TableComponent,
  type CellContext,
  type ColumnDef,
} from '../../../../shared/components/table/table.component';
import type {
  AutomationPeriod,
  PerformanceRange,
  PerformanceRangeDraft,
  PerformanceRangeType,
  PointsAutomationConfig,
} from '../../models/points-automation';
import { PointsAutomationService } from '../../service/points-automation.service';

@Component({
  selector: 'app-points-automation',
  standalone: true,
  imports: [
    FormsModule,
    TranslatePipe,
    ButtonComponent,
    TableComponent,
    ModalComponent,
    ModalBodyComponent,
  ],
  templateUrl: './points-automation.component.html',
  styleUrl: './points-automation.component.css',
})
export class PointsAutomationComponent implements OnInit {
  private readonly automationService = inject(PointsAutomationService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);

  private readonly pointsTemplate =
    viewChild<TemplateRef<CellContext>>('pointsTemplate');

  readonly periods: AutomationPeriod[] = [
    'daily',
    'weekly',
    'biweekly',
    'monthly',
  ];

  readonly loading = signal(true);
  readonly loadFailed = signal(false);
  readonly config = signal<PointsAutomationConfig | null>(null);
  readonly mode = signal<'view' | 'setup'>('view');
  readonly submitting = signal(false);
  readonly showSuccessModal = signal(false);

  readonly draftEnabled = signal(false);
  readonly draftPeriod = signal<AutomationPeriod>('daily');
  readonly draftRanges = signal<PerformanceRangeDraft[]>([]);

  readonly sortColumn = signal('');
  readonly sortDirection = signal<'asc' | 'desc'>('asc');

  private readonly columnLabels = toSignal(
    this.translate.stream([
      'POINTS.AUTOMATION.COL_TYPE',
      'POINTS.AUTOMATION.COL_FROM',
      'POINTS.AUTOMATION.COL_TO',
      'POINTS.AUTOMATION.COL_POINTS',
    ]),
    {
      initialValue: {
        'POINTS.AUTOMATION.COL_TYPE': 'Type',
        'POINTS.AUTOMATION.COL_FROM': 'From',
        'POINTS.AUTOMATION.COL_TO': 'To',
        'POINTS.AUTOMATION.COL_POINTS': 'Points',
      },
    }
  );

  readonly columns = computed<ColumnDef[]>(() => {
    const labels = this.columnLabels();

    return [
      {
        key: 'type',
        label: labels['POINTS.AUTOMATION.COL_TYPE'],
        width: '1.2fr',
        sortable: true,
      },
      {
        key: 'from',
        label: labels['POINTS.AUTOMATION.COL_FROM'],
        width: '1fr',
        sortable: true,
      },
      {
        key: 'to',
        label: labels['POINTS.AUTOMATION.COL_TO'],
        width: '1fr',
        sortable: true,
      },
      {
        key: 'value',
        label: labels['POINTS.AUTOMATION.COL_POINTS'],
        width: '1fr',
        sortable: true,
        template: 'points',
      },
    ];
  });

  readonly cellTemplates = computed(() => {
    const pointsTemplate = this.pointsTemplate();
    const templates = new Map<string, TemplateRef<CellContext>>();

    if (pointsTemplate) {
      templates.set('points', pointsTemplate);
    }

    return templates;
  });

  readonly viewRows = computed(() => {
    const ranges = this.config()?.performance.ranges ?? [];
    const column = this.sortColumn();
    const direction = this.sortDirection();

    if (!column) {
      return ranges;
    }

    return [...ranges].sort((first, second) => {
      const firstValue = first[column as keyof PerformanceRange];
      const secondValue = second[column as keyof PerformanceRange];

      if (typeof firstValue === 'number' && typeof secondValue === 'number') {
        return direction === 'asc' ? firstValue - secondValue : secondValue - firstValue;
      }

      return direction === 'asc'
        ? String(firstValue).localeCompare(String(secondValue))
        : String(secondValue).localeCompare(String(firstValue));
    });
  });

  readonly overlapIds = computed(() => {
    const ranges = this.draftRanges().filter(
      (range) => !this.isBlankRange(range) && this.isValidRange(range)
    );
    const overlapping = new Set<number>();

    for (let index = 0; index < ranges.length; index += 1) {
      for (let next = index + 1; next < ranges.length; next += 1) {
        const first = ranges[index];
        const second = ranges[next];

        if (first.from <= second.to && second.from <= first.to) {
          overlapping.add(first.id);
          overlapping.add(second.id);
        }
      }
    }

    return overlapping;
  });

  readonly hasOverlap = computed(() => this.overlapIds().size > 0);

  readonly canSave = computed(() => {
    if (this.submitting() || !this.config()) {
      return false;
    }

    const committed = this.draftRanges().filter((range) => !this.isBlankRange(range));
    const allValid = committed.every((range) => this.isValidRange(range));

    return allValid && !this.hasOverlap();
  });

  readonly tasksEnabled = computed(() => this.readEnabled(this.config()?.tasks));
  readonly attendanceEnabled = computed(() =>
    this.readEnabled(this.config()?.attendance)
  );

  ngOnInit(): void {
    this.automationService.getConfig().subscribe({
      next: (config) => {
        this.config.set(config);
        this.applyDraft(config);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        console.error('Load points automation failed:', err);
        this.loadFailed.set(true);
        this.loading.set(false);
      },
    });
  }

  navigateBack(): void {
    void this.router.navigate(['/points']);
  }

  enterSetup(): void {
    const config = this.config();
    if (!config) {
      return;
    }

    this.applyDraft(config);
    this.mode.set('setup');
  }

  onDiscard(): void {
    const config = this.config();
    if (config) {
      this.applyDraft(config);
    }
    this.mode.set('view');
  }

  onSave(): void {
    const config = this.config();
    if (!this.canSave() || !config) {
      return;
    }

    const ranges: PerformanceRange[] = this.draftRanges()
      .filter((range) => !this.isBlankRange(range) && this.isValidRange(range))
      .map((range) => ({
        id: range.id,
        type: range.type as PerformanceRangeType,
        from: range.from,
        to: range.to,
        value: range.value,
      }));

    const enabled = this.draftEnabled();
    const payload: PointsAutomationConfig = {
      ...config,
      activeCategory: enabled ? 'performance' : config.activeCategory,
      performance: {
        enabled,
        period: this.draftPeriod(),
        ranges,
      },
    };

    this.submitting.set(true);

    this.automationService.saveConfig(payload).subscribe({
      next: (saved) => {
        this.config.set(saved);
        this.applyDraft(saved);
        this.submitting.set(false);
        this.mode.set('view');
        this.showSuccessModal.set(true);
      },
      error: (err: unknown) => {
        this.submitting.set(false);
        console.error('Save points automation failed:', err);
      },
    });
  }

  onSuccessClose(): void {
    this.showSuccessModal.set(false);
  }

  addRange(): void {
    const nextId =
      Math.max(0, ...this.draftRanges().map((range) => range.id), 0) + 1;

    this.draftRanges.update((ranges) => [
      ...ranges,
      { id: nextId, type: '', from: 0, to: 0, value: 0 },
    ]);
  }

  removeRange(id: number): void {
    this.draftRanges.update((ranges) => ranges.filter((range) => range.id !== id));
  }

  updateRangeType(id: number, type: string): void {
    this.patchRange(id, {
      type: type === 'Reward' || type === 'Deduction' ? type : '',
    });
  }

  updateRangeFrom(id: number, value: number | string): void {
    this.patchRange(id, { from: this.toNumber(value) });
  }

  updateRangeTo(id: number, value: number | string): void {
    this.patchRange(id, { to: this.toNumber(value) });
  }

  updateRangeValue(id: number, value: number | string): void {
    this.patchRange(id, { value: Math.trunc(this.toNumber(value)) });
  }

  onSort(event: { column: string; direction: 'asc' | 'desc' }): void {
    this.sortColumn.set(event.column);
    this.sortDirection.set(event.direction);
  }

  displayPoints(range: PerformanceRange): number {
    return range.type === 'Deduction' ? -Math.abs(range.value) : Math.abs(range.value);
  }

  formatPoints(points: number): string {
    if (points > 0) {
      return `+ ${points}`;
    }
    if (points < 0) {
      return `- ${Math.abs(points)}`;
    }
    return '0';
  }

  periodLabelKey(period: AutomationPeriod): string {
    switch (period) {
      case 'daily':
        return 'POINTS.AUTOMATION.PERIOD_DAILY';
      case 'weekly':
        return 'POINTS.AUTOMATION.PERIOD_WEEKLY';
      case 'biweekly':
        return 'POINTS.AUTOMATION.PERIOD_BIWEEKLY';
      case 'monthly':
        return 'POINTS.AUTOMATION.PERIOD_MONTHLY';
    }
  }

  isBlankRange(range: PerformanceRangeDraft): boolean {
    return range.type === '' && range.from === 0 && range.to === 0 && range.value === 0;
  }

  isValidRange(range: PerformanceRangeDraft): boolean {
    return (
      (range.type === 'Reward' || range.type === 'Deduction') &&
      Number.isFinite(range.from) &&
      Number.isFinite(range.to) &&
      Number.isFinite(range.value) &&
      range.from >= 0 &&
      range.to <= 100 &&
      range.from <= range.to &&
      range.value >= 0 &&
      Number.isInteger(range.value)
    );
  }

  private applyDraft(config: PointsAutomationConfig): void {
    this.draftEnabled.set(config.performance.enabled);
    this.draftPeriod.set(config.performance.period);
    this.draftRanges.set(
      config.performance.ranges.map((range) => ({ ...range }))
    );
  }

  private patchRange(id: number, patch: Partial<PerformanceRangeDraft>): void {
    this.draftRanges.update((ranges) =>
      ranges.map((range) => (range.id === id ? { ...range, ...patch } : range))
    );
  }

  private toNumber(value: number | string): number {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : 0;
  }

  private readEnabled(value: unknown): boolean {
    return (
      typeof value === 'object' &&
      value !== null &&
      'enabled' in value &&
      Boolean((value as { enabled: boolean }).enabled)
    );
  }
}
