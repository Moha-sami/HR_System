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
  AutomationCategory,
  AutomationPeriod,
  PerformanceRange,
  PerformanceRangeDraft,
  PerformanceRangeType,
  PointsAutomationConfig,
  TaskDeadlineDraft,
  TaskDeadlineRule,
  TaskPriority,
  TasksAutomation,
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
  private readonly deadlinePointsTemplate =
    viewChild<TemplateRef<CellContext>>('deadlinePointsTemplate');

  readonly periods: AutomationPeriod[] = [
    'daily',
    'weekly',
    'biweekly',
    'monthly',
  ];

  readonly priorities: TaskPriority[] = ['Urgent', 'High', 'Medium', 'Low'];

  readonly loading = signal(true);
  readonly loadFailed = signal(false);
  readonly config = signal<PointsAutomationConfig | null>(null);
  readonly mode = signal<'view' | 'setup'>('view');
  readonly activeTab = signal<AutomationCategory>('performance');
  readonly submitting = signal(false);
  readonly showSuccessModal = signal(false);
  readonly successMessageKey = signal('POINTS.AUTOMATION.SUCCESS_PERFORMANCE');

  readonly draftEnabledCategory = signal<AutomationCategory>('performance');
  readonly draftPeriod = signal<AutomationPeriod>('daily');
  readonly draftRanges = signal<PerformanceRangeDraft[]>([]);
  readonly draftTasksPeriod = signal<AutomationPeriod>('daily');
  readonly draftCompletionRanges = signal<PerformanceRangeDraft[]>([]);
  readonly draftDeadlineRules = signal<TaskDeadlineDraft[]>([]);

  readonly sortColumn = signal('');
  readonly sortDirection = signal<'asc' | 'desc'>('asc');
  readonly deadlineSortColumn = signal('');
  readonly deadlineSortDirection = signal<'asc' | 'desc'>('asc');

  private readonly columnLabels = toSignal(
    this.translate.stream([
      'POINTS.AUTOMATION.COL_TYPE',
      'POINTS.AUTOMATION.COL_FROM',
      'POINTS.AUTOMATION.COL_TO',
      'POINTS.AUTOMATION.COL_POINTS',
      'POINTS.AUTOMATION.COL_PRIORITY',
      'POINTS.AUTOMATION.COL_POINTS_PER_DAY',
    ]),
    {
      initialValue: {
        'POINTS.AUTOMATION.COL_TYPE': 'Type',
        'POINTS.AUTOMATION.COL_FROM': 'From',
        'POINTS.AUTOMATION.COL_TO': 'To',
        'POINTS.AUTOMATION.COL_POINTS': 'Points',
        'POINTS.AUTOMATION.COL_PRIORITY': 'Task Priority',
        'POINTS.AUTOMATION.COL_POINTS_PER_DAY': 'Points Deduction/Day delay',
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

  readonly deadlineColumns = computed<ColumnDef[]>(() => {
    const labels = this.columnLabels();

    return [
      {
        key: 'priority',
        label: labels['POINTS.AUTOMATION.COL_PRIORITY'],
        width: '1.2fr',
        sortable: true,
      },
      {
        key: 'pointsPerDayDelay',
        label: labels['POINTS.AUTOMATION.COL_POINTS_PER_DAY'],
        width: '1.4fr',
        sortable: true,
        template: 'deadlinePoints',
      },
    ];
  });

  readonly cellTemplates = computed(() => {
    const templates = new Map<string, TemplateRef<CellContext>>();
    const pointsTemplate = this.pointsTemplate();
    const deadlinePointsTemplate = this.deadlinePointsTemplate();

    if (pointsTemplate) {
      templates.set('points', pointsTemplate);
    }

    if (deadlinePointsTemplate) {
      templates.set('deadlinePoints', deadlinePointsTemplate);
    }

    return templates;
  });

  readonly viewRows = computed(() =>
    this.sortRanges(this.config()?.performance.ranges ?? [], this.sortColumn(), this.sortDirection())
  );

  readonly viewCompletionRows = computed(() =>
    this.sortRanges(
      this.config()?.tasks.completionRanges ?? [],
      this.sortColumn(),
      this.sortDirection()
    )
  );

  readonly viewDeadlineRows = computed(() => {
    const rules = this.config()?.tasks.deadlineRules ?? [];
    const column = this.deadlineSortColumn();
    const direction = this.deadlineSortDirection();

    if (!column) {
      return rules;
    }

    return [...rules].sort((first, second) => {
      const firstValue = first[column as keyof TaskDeadlineRule];
      const secondValue = second[column as keyof TaskDeadlineRule];

      if (typeof firstValue === 'number' && typeof secondValue === 'number') {
        return direction === 'asc' ? firstValue - secondValue : secondValue - firstValue;
      }

      return direction === 'asc'
        ? String(firstValue).localeCompare(String(secondValue))
        : String(secondValue).localeCompare(String(firstValue));
    });
  });

  readonly overlapIds = computed(() =>
    this.findOverlapIds(this.draftRanges())
  );

  readonly completionOverlapIds = computed(() =>
    this.findOverlapIds(this.draftCompletionRanges())
  );

  readonly duplicatePriorityIds = computed(() => {
    const rules = this.draftDeadlineRules().filter(
      (rule) => !this.isBlankDeadline(rule)
    );
    const overlapping = new Set<number>();

    for (let index = 0; index < rules.length; index += 1) {
      for (let next = index + 1; next < rules.length; next += 1) {
        if (
          rules[index].priority &&
          rules[index].priority === rules[next].priority
        ) {
          overlapping.add(rules[index].id);
          overlapping.add(rules[next].id);
        }
      }
    }

    return overlapping;
  });

  readonly hasOverlap = computed(() => this.overlapIds().size > 0);
  readonly hasCompletionOverlap = computed(
    () => this.completionOverlapIds().size > 0
  );
  readonly hasDuplicatePriority = computed(
    () => this.duplicatePriorityIds().size > 0
  );

  readonly enabledCategory = computed(() => {
    if (this.mode() === 'setup') {
      return this.draftEnabledCategory();
    }

    return this.enabledCategoryFrom(this.config());
  });

  readonly canSave = computed(() => {
    if (this.submitting() || !this.config() || this.activeTab() === 'attendance') {
      return false;
    }

    if (this.activeTab() === 'tasks') {
      const completion = this.draftCompletionRanges().filter(
        (range) => !this.isBlankRange(range)
      );
      const deadlines = this.draftDeadlineRules().filter(
        (rule) => !this.isBlankDeadline(rule)
      );

      return (
        completion.every((range) => this.isValidRange(range)) &&
        deadlines.every((rule) => this.isValidDeadline(rule)) &&
        !this.hasCompletionOverlap() &&
        !this.hasDuplicatePriority()
      );
    }

    const committed = this.draftRanges().filter((range) => !this.isBlankRange(range));
    return committed.every((range) => this.isValidRange(range)) && !this.hasOverlap();
  });

  ngOnInit(): void {
    this.automationService.getConfig().subscribe({
      next: (config) => {
        const normalized = this.normalizeConfig(config);
        this.config.set(normalized);
        this.activeTab.set(this.enabledCategoryFrom(normalized));
        this.applyDraft(normalized);
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

  selectTab(tab: AutomationCategory): void {
    this.activeTab.set(tab);
    this.sortColumn.set('');
    this.deadlineSortColumn.set('');

    if (tab === 'attendance' && this.mode() === 'setup') {
      this.onDiscard();
    }
  }

  onToggle(tab: AutomationCategory, enabled: boolean): void {
    if (!enabled) {
      return;
    }

    if (this.mode() === 'setup') {
      this.draftEnabledCategory.set(tab);
      this.activeTab.set(tab);
      return;
    }

    this.enableCategory(tab);
  }

  enterSetup(): void {
    const config = this.config();
    if (!config || this.activeTab() === 'attendance') {
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

    const enabled = this.draftEnabledCategory();
    let payload = this.withExclusiveEnabled(config, enabled);

    if (this.activeTab() === 'tasks') {
      payload = {
        ...payload,
        tasks: {
          ...payload.tasks,
          enabled: enabled === 'tasks',
          period: this.draftTasksPeriod(),
          completionRanges: this.commitRanges(this.draftCompletionRanges()),
          deadlineRules: this.commitDeadlines(this.draftDeadlineRules()),
        },
      };
    } else {
      payload = {
        ...payload,
        performance: {
          ...payload.performance,
          enabled: enabled === 'performance',
          period: this.draftPeriod(),
          ranges: this.commitRanges(this.draftRanges()),
        },
      };
    }

    this.submitting.set(true);

    this.automationService.saveConfig(payload).subscribe({
      next: (saved) => {
        const normalized = this.normalizeConfig(saved);
        this.config.set(normalized);
        this.applyDraft(normalized);
        this.submitting.set(false);
        this.mode.set('view');
        this.successMessageKey.set(
          this.activeTab() === 'tasks'
            ? 'POINTS.AUTOMATION.SUCCESS_TASKS'
            : 'POINTS.AUTOMATION.SUCCESS_PERFORMANCE'
        );
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
    this.draftRanges.update((ranges) => [
      ...ranges,
      { id: this.nextId(ranges), type: '', from: 0, to: 0, value: 0 },
    ]);
  }

  addCompletionRange(): void {
    this.draftCompletionRanges.update((ranges) => [
      ...ranges,
      { id: this.nextId(ranges), type: '', from: 0, to: 0, value: 0 },
    ]);
  }

  addDeadlineRule(): void {
    this.draftDeadlineRules.update((rules) => [
      ...rules,
      { id: this.nextId(rules), priority: '', pointsPerDayDelay: 0 },
    ]);
  }

  removeRange(id: number): void {
    this.draftRanges.update((ranges) => ranges.filter((range) => range.id !== id));
  }

  removeCompletionRange(id: number): void {
    this.draftCompletionRanges.update((ranges) =>
      ranges.filter((range) => range.id !== id)
    );
  }

  removeDeadlineRule(id: number): void {
    this.draftDeadlineRules.update((rules) => rules.filter((rule) => rule.id !== id));
  }

  updateRangeType(id: number, type: string): void {
    this.patchRange(this.draftRanges, id, {
      type: type === 'Reward' || type === 'Deduction' ? type : '',
    });
  }

  updateCompletionType(id: number, type: string): void {
    this.patchRange(this.draftCompletionRanges, id, {
      type: type === 'Reward' || type === 'Deduction' ? type : '',
    });
  }

  updateRangeFrom(id: number, value: number | string): void {
    this.patchRange(this.draftRanges, id, { from: this.toNumber(value) });
  }

  updateCompletionFrom(id: number, value: number | string): void {
    this.patchRange(this.draftCompletionRanges, id, { from: this.toNumber(value) });
  }

  updateRangeTo(id: number, value: number | string): void {
    this.patchRange(this.draftRanges, id, { to: this.toNumber(value) });
  }

  updateCompletionTo(id: number, value: number | string): void {
    this.patchRange(this.draftCompletionRanges, id, { to: this.toNumber(value) });
  }

  updateRangeValue(id: number, value: number | string): void {
    this.patchRange(this.draftRanges, id, { value: Math.trunc(this.toNumber(value)) });
  }

  updateCompletionValue(id: number, value: number | string): void {
    this.patchRange(this.draftCompletionRanges, id, {
      value: Math.trunc(this.toNumber(value)),
    });
  }

  updateDeadlinePriority(id: number, priority: string): void {
    this.draftDeadlineRules.update((rules) =>
      rules.map((rule) => (rule.id === id ? { ...rule, priority } : rule))
    );
  }

  updateDeadlinePoints(id: number, value: number | string): void {
    this.draftDeadlineRules.update((rules) =>
      rules.map((rule) =>
        rule.id === id
          ? { ...rule, pointsPerDayDelay: Math.trunc(this.toNumber(value)) }
          : rule
      )
    );
  }

  onSort(event: { column: string; direction: 'asc' | 'desc' }): void {
    this.sortColumn.set(event.column);
    this.sortDirection.set(event.direction);
  }

  onDeadlineSort(event: { column: string; direction: 'asc' | 'desc' }): void {
    this.deadlineSortColumn.set(event.column);
    this.deadlineSortDirection.set(event.direction);
  }

  displayPoints(range: PerformanceRange): number {
    return range.type === 'Deduction' ? -Math.abs(range.value) : Math.abs(range.value);
  }

  displayDeadlinePoints(points: number): number {
    return -Math.abs(points);
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

  priorityLabelKey(priority: TaskPriority): string {
    switch (priority) {
      case 'Urgent':
        return 'POINTS.AUTOMATION.PRIORITY_URGENT';
      case 'High':
        return 'POINTS.AUTOMATION.PRIORITY_HIGH';
      case 'Medium':
        return 'POINTS.AUTOMATION.PRIORITY_MEDIUM';
      case 'Low':
        return 'POINTS.AUTOMATION.PRIORITY_LOW';
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

  isBlankDeadline(rule: TaskDeadlineDraft): boolean {
    return rule.priority === '' && rule.pointsPerDayDelay === 0;
  }

  isValidDeadline(rule: TaskDeadlineDraft): boolean {
    return (
      this.priorities.includes(rule.priority as TaskPriority) &&
      Number.isFinite(rule.pointsPerDayDelay) &&
      rule.pointsPerDayDelay >= 0 &&
      Number.isInteger(rule.pointsPerDayDelay)
    );
  }

  private enableCategory(tab: AutomationCategory): void {
    const config = this.config();
    if (!config || this.enabledCategoryFrom(config) === tab) {
      this.activeTab.set(tab);
      return;
    }

    const payload = this.withExclusiveEnabled(config, tab);

    this.automationService.saveConfig(payload).subscribe({
      next: (saved) => {
        const normalized = this.normalizeConfig(saved);
        this.config.set(normalized);
        this.applyDraft(normalized);
        this.activeTab.set(tab);
      },
      error: (err: unknown) => {
        console.error('Update automation category failed:', err);
      },
    });
  }

  private applyDraft(config: PointsAutomationConfig): void {
    this.draftEnabledCategory.set(this.enabledCategoryFrom(config));
    this.draftPeriod.set(config.performance.period);
    this.draftRanges.set(config.performance.ranges.map((range) => ({ ...range })));
    this.draftTasksPeriod.set(config.tasks.period);
    this.draftCompletionRanges.set(
      config.tasks.completionRanges.map((range) => ({ ...range }))
    );
    this.draftDeadlineRules.set(
      config.tasks.deadlineRules.map((rule) => ({ ...rule }))
    );
  }

  private withExclusiveEnabled(
    config: PointsAutomationConfig,
    category: AutomationCategory
  ): PointsAutomationConfig {
    return {
      ...config,
      activeCategory: category,
      performance: {
        ...config.performance,
        enabled: category === 'performance',
      },
      tasks: {
        ...config.tasks,
        enabled: category === 'tasks',
      },
      attendance: {
        ...config.attendance,
        enabled: category === 'attendance',
      },
    };
  }

  private enabledCategoryFrom(config: PointsAutomationConfig | null): AutomationCategory {
    if (!config) {
      return 'performance';
    }

    if (config.performance.enabled) {
      return 'performance';
    }
    if (config.tasks.enabled) {
      return 'tasks';
    }
    if (config.attendance?.enabled) {
      return 'attendance';
    }

    if (config.activeCategory === 'tasks' || config.activeCategory === 'attendance') {
      return config.activeCategory;
    }

    return 'performance';
  }

  private normalizeConfig(config: PointsAutomationConfig): PointsAutomationConfig {
    const tasks = (config.tasks ?? {}) as Partial<TasksAutomation>;

    return {
      ...config,
      tasks: {
        enabled: Boolean(tasks.enabled),
        period: tasks.period ?? 'daily',
        completionRanges: [...(tasks.completionRanges ?? [])],
        deadlineRules: [...(tasks.deadlineRules ?? [])],
      },
      attendance:
        typeof config.attendance === 'object' && config.attendance
          ? { ...config.attendance }
          : { enabled: false },
    };
  }

  private findOverlapIds(ranges: PerformanceRangeDraft[]): Set<number> {
    const valid = ranges.filter(
      (range) => !this.isBlankRange(range) && this.isValidRange(range)
    );
    const overlapping = new Set<number>();

    for (let index = 0; index < valid.length; index += 1) {
      for (let next = index + 1; next < valid.length; next += 1) {
        const first = valid[index];
        const second = valid[next];

        if (first.from <= second.to && second.from <= first.to) {
          overlapping.add(first.id);
          overlapping.add(second.id);
        }
      }
    }

    return overlapping;
  }

  private sortRanges(
    ranges: PerformanceRange[],
    column: string,
    direction: 'asc' | 'desc'
  ): PerformanceRange[] {
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
  }

  private commitRanges(ranges: PerformanceRangeDraft[]): PerformanceRange[] {
    return ranges
      .filter((range) => !this.isBlankRange(range) && this.isValidRange(range))
      .map((range) => ({
        id: range.id,
        type: range.type as PerformanceRangeType,
        from: range.from,
        to: range.to,
        value: range.value,
      }));
  }

  private commitDeadlines(rules: TaskDeadlineDraft[]): TaskDeadlineRule[] {
    return rules
      .filter((rule) => !this.isBlankDeadline(rule) && this.isValidDeadline(rule))
      .map((rule) => ({
        id: rule.id,
        priority: rule.priority,
        pointsPerDayDelay: rule.pointsPerDayDelay,
      }));
  }

  private patchRange(
    target: typeof this.draftRanges,
    id: number,
    patch: Partial<PerformanceRangeDraft>
  ): void {
    target.update((ranges) =>
      ranges.map((range) => (range.id === id ? { ...range, ...patch } : range))
    );
  }

  private nextId(items: { id: number }[]): number {
    return Math.max(0, ...items.map((item) => item.id), 0) + 1;
  }

  private toNumber(value: number | string): number {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : 0;
  }
}
