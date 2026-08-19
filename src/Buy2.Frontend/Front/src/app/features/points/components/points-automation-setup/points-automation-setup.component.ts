import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import { ButtonComponent } from '../../../../shared/components/button/button.component';
import { ModalComponent } from '../../../../shared/components/modal/modal.component';
import { ModalBodyComponent } from '../../../../shared/components/modal/modal-body.component';
import type {
  AttendanceAutomation,
  AutomationCategory,
  AutomationPeriod,
  DeadlineRule,
  LatenessRangeRule,
  PercentRangeRule,
  PerformanceAutomation,
  PointsAutomationConfig,
  RangeType,
  TasksAutomation,
} from '../../models/points-automation';
import { PointsAutomationService } from '../../service/points-automation.service';
import { findOverlappingRangeIndexes } from '../../utils/range-overlap';

@Component({
  selector: 'app-points-automation-setup',
  standalone: true,
  imports: [
    FormsModule,
    TranslatePipe,
    ButtonComponent,
    ModalComponent,
    ModalBodyComponent,
  ],
  templateUrl: './points-automation-setup.component.html',
  styleUrl: './points-automation-setup.component.css',
})
export class PointsAutomationSetupComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly automationService = inject(PointsAutomationService);

  readonly config = signal<PointsAutomationConfig | null>(null);
  readonly activeCategory = signal<AutomationCategory>('performance');
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly showSuccessModal = signal(false);

  readonly draftPercentRange = signal<PercentRangeRule>({
    id: 0,
    type: 'Reward',
    from: 0,
    to: 0,
    value: 0,
  });

  readonly draftDeadline = signal<DeadlineRule>({
    id: 0,
    priority: 'Urgent',
    pointsPerDayDelay: 0,
  });

  readonly draftLateness = signal<LatenessRangeRule>({
    id: 0,
    fromMinutes: 0,
    toMinutes: 0,
    pointsDeduction: 0,
  });

  readonly performanceOverlapIndexes = computed(() => {
    const ranges = this.config()?.performance.ranges ?? [];
    return new Set(findOverlappingRangeIndexes(ranges));
  });

  readonly tasksCompletionOverlapIndexes = computed(() => {
    const ranges = this.config()?.tasks.completionRanges ?? [];
    return new Set(findOverlappingRangeIndexes(ranges));
  });

  readonly attendanceOverlapIndexes = computed(() => {
    const ranges = this.config()?.attendance.attendanceRanges ?? [];
    return new Set(findOverlappingRangeIndexes(ranges));
  });

  readonly latenessOverlapIndexes = computed(() => {
    const ranges = (this.config()?.attendance.latenessRanges ?? []).map(
      (row) => ({ from: row.fromMinutes, to: row.toMinutes })
    );
    return new Set(findOverlappingRangeIndexes(ranges));
  });

  readonly hasOverlapError = computed(() => {
    const category = this.activeCategory();
    if (category === 'performance') {
      return this.performanceOverlapIndexes().size > 0;
    }
    if (category === 'tasks') {
      return this.tasksCompletionOverlapIndexes().size > 0;
    }
    return (
      this.attendanceOverlapIndexes().size > 0 ||
      this.latenessOverlapIndexes().size > 0
    );
  });

  readonly successMessageKey = computed(() => {
    switch (this.activeCategory()) {
      case 'tasks':
        return 'POINTS.AUTOMATION.SUCCESS_TASKS';
      case 'attendance':
        return 'POINTS.AUTOMATION.SUCCESS_ATTENDANCE';
      default:
        return 'POINTS.AUTOMATION.SUCCESS_PERFORMANCE';
    }
  });

  readonly periodOptions: { value: AutomationPeriod; labelKey: string }[] = [
    { value: 'daily', labelKey: 'POINTS.AUTOMATION.PERIOD_DAILY' },
    { value: 'weekly', labelKey: 'POINTS.AUTOMATION.PERIOD_WEEKLY' },
    { value: 'biWeekly', labelKey: 'POINTS.AUTOMATION.PERIOD_BIWEEKLY' },
    { value: 'monthly', labelKey: 'POINTS.AUTOMATION.PERIOD_MONTHLY' },
  ];

  readonly rangeTypes: RangeType[] = ['Reward', 'Deduction'];
  readonly priorities = ['Urgent', 'High', 'Medium', 'Low'];

  ngOnInit(): void {
    const categoryParam = this.route.snapshot.queryParamMap.get('category');
    if (
      categoryParam === 'performance' ||
      categoryParam === 'tasks' ||
      categoryParam === 'attendance'
    ) {
      this.activeCategory.set(categoryParam);
    }

    this.automationService.getConfig().subscribe({
      next: (config) => {
        this.config.set(structuredClone(config));
        if (!categoryParam) {
          this.activeCategory.set(config.activeCategory);
        }
        this.loading.set(false);
      },
      error: (err: unknown) => {
        console.error('Failed to load automation config:', err);
        this.loading.set(false);
      },
    });
  }

  selectCategory(_category: AutomationCategory): void {
    // Setup is locked to the category opened from Edit.
  }

  isEditingCategory(category: AutomationCategory): boolean {
    return this.activeCategory() === category;
  }

  toggleCategory(category: AutomationCategory, enabled: boolean): void {
    const current = this.config();
    if (!current || category !== this.activeCategory()) {
      return;
    }

    // Only one category can be enabled at a time.
    this.config.set({
      ...current,
      activeCategory: category,
      performance: {
        ...current.performance,
        enabled: category === 'performance' ? enabled : false,
      },
      tasks: {
        ...current.tasks,
        enabled: category === 'tasks' ? enabled : false,
      },
      attendance: {
        ...current.attendance,
        enabled: category === 'attendance' ? enabled : false,
      },
    });
  }

  updatePerformance(
    updater: (section: PerformanceAutomation) => PerformanceAutomation
  ): void {
    const current = this.config();
    if (!current) {
      return;
    }
    this.config.set({
      ...current,
      performance: updater(current.performance),
    });
  }

  updateTasks(updater: (section: TasksAutomation) => TasksAutomation): void {
    const current = this.config();
    if (!current) {
      return;
    }
    this.config.set({
      ...current,
      tasks: updater(current.tasks),
    });
  }

  updateAttendance(
    updater: (section: AttendanceAutomation) => AttendanceAutomation
  ): void {
    const current = this.config();
    if (!current) {
      return;
    }
    this.config.set({
      ...current,
      attendance: updater(current.attendance),
    });
  }

  setPeriod(period: AutomationPeriod): void {
    const category = this.activeCategory();
    if (category === 'performance') {
      this.updatePerformance((section) => ({ ...section, period }));
    } else if (category === 'tasks') {
      this.updateTasks((section) => ({ ...section, period }));
    } else {
      this.updateAttendance((section) => ({ ...section, period }));
    }
  }

  currentPeriod(): AutomationPeriod {
    const cfg = this.config();
    if (!cfg) {
      return 'daily';
    }
    if (this.activeCategory() === 'performance') {
      return cfg.performance.period;
    }
    if (this.activeCategory() === 'tasks') {
      return cfg.tasks.period;
    }
    return cfg.attendance.period;
  }

  updatePercentRange(
    list: 'performance' | 'tasksCompletion' | 'attendance',
    index: number,
    patch: Partial<PercentRangeRule>
  ): void {
    if (list === 'performance') {
      this.updatePerformance((section) => ({
        ...section,
        ranges: section.ranges.map((row, i) =>
          i === index ? { ...row, ...patch } : row
        ),
      }));
      return;
    }

    if (list === 'tasksCompletion') {
      this.updateTasks((section) => ({
        ...section,
        completionRanges: section.completionRanges.map((row, i) =>
          i === index ? { ...row, ...patch } : row
        ),
      }));
      return;
    }

    this.updateAttendance((section) => ({
      ...section,
      attendanceRanges: section.attendanceRanges.map((row, i) =>
        i === index ? { ...row, ...patch } : row
      ),
    }));
  }

  removePercentRange(
    list: 'performance' | 'tasksCompletion' | 'attendance',
    index: number
  ): void {
    if (list === 'performance') {
      this.updatePerformance((section) => ({
        ...section,
        ranges: section.ranges.filter((_, i) => i !== index),
      }));
      return;
    }

    if (list === 'tasksCompletion') {
      this.updateTasks((section) => ({
        ...section,
        completionRanges: section.completionRanges.filter((_, i) => i !== index),
      }));
      return;
    }

    this.updateAttendance((section) => ({
      ...section,
      attendanceRanges: section.attendanceRanges.filter((_, i) => i !== index),
    }));
  }

  addPercentRange(list: 'performance' | 'tasksCompletion' | 'attendance'): void {
    const draft = { ...this.draftPercentRange(), id: Date.now() };

    if (list === 'performance') {
      this.updatePerformance((section) => ({
        ...section,
        ranges: [...section.ranges, draft],
      }));
    } else if (list === 'tasksCompletion') {
      this.updateTasks((section) => ({
        ...section,
        completionRanges: [...section.completionRanges, draft],
      }));
    } else {
      this.updateAttendance((section) => ({
        ...section,
        attendanceRanges: [...section.attendanceRanges, draft],
      }));
    }

    this.draftPercentRange.set({
      id: 0,
      type: 'Reward',
      from: 0,
      to: 0,
      value: 0,
    });
  }

  updateDeadline(index: number, patch: Partial<DeadlineRule>): void {
    this.updateTasks((section) => ({
      ...section,
      deadlineRules: section.deadlineRules.map((row, i) =>
        i === index ? { ...row, ...patch } : row
      ),
    }));
  }

  removeDeadline(index: number): void {
    this.updateTasks((section) => ({
      ...section,
      deadlineRules: section.deadlineRules.filter((_, i) => i !== index),
    }));
  }

  addDeadline(): void {
    const draft = { ...this.draftDeadline(), id: Date.now() };
    this.updateTasks((section) => ({
      ...section,
      deadlineRules: [...section.deadlineRules, draft],
    }));
    this.draftDeadline.set({
      id: 0,
      priority: 'Urgent',
      pointsPerDayDelay: 0,
    });
  }

  updateLateness(index: number, patch: Partial<LatenessRangeRule>): void {
    this.updateAttendance((section) => ({
      ...section,
      latenessRanges: section.latenessRanges.map((row, i) =>
        i === index ? { ...row, ...patch } : row
      ),
    }));
  }

  removeLateness(index: number): void {
    this.updateAttendance((section) => ({
      ...section,
      latenessRanges: section.latenessRanges.filter((_, i) => i !== index),
    }));
  }

  addLateness(): void {
    const draft = { ...this.draftLateness(), id: Date.now() };
    this.updateAttendance((section) => ({
      ...section,
      latenessRanges: [...section.latenessRanges, draft],
    }));
    this.draftLateness.set({
      id: 0,
      fromMinutes: 0,
      toMinutes: 0,
      pointsDeduction: 0,
    });
  }

  setOnTimeBonus(value: number): void {
    this.updateAttendance((section) => ({
      ...section,
      onTimeBonus: value,
    }));
  }

  onDiscard(): void {
    void this.router.navigate(['/points/automation'], {
      queryParams: { category: this.activeCategory() },
    });
  }

  onSave(): void {
    const current = this.config();
    if (!current || this.hasOverlapError() || this.saving()) {
      return;
    }

    const payload: PointsAutomationConfig = {
      ...current,
      activeCategory: this.activeCategory(),
    };

    this.saving.set(true);
    this.automationService.saveConfig(payload).subscribe({
      next: (saved) => {
        this.config.set(saved);
        this.saving.set(false);
        this.showSuccessModal.set(true);
      },
      error: (err: unknown) => {
        console.error('Failed to save automation config:', err);
        this.saving.set(false);
      },
    });
  }

  onSuccessClose(): void {
    this.showSuccessModal.set(false);
    void this.router.navigate(['/points/automation'], {
      queryParams: { category: this.activeCategory() },
    });
  }
}
