import {
  Component,
  computed,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import { ButtonComponent } from '../../../../shared/components/button/button.component';
import type {
  AutomationCategory,
  PointsAutomationConfig,
} from '../../models/points-automation';
import { PointsAutomationService } from '../../service/points-automation.service';

@Component({
  selector: 'app-points-automation',
  standalone: true,
  imports: [FormsModule, TranslatePipe, ButtonComponent],
  templateUrl: './points-automation.component.html',
  styleUrl: './points-automation.component.css',
})
export class PointsAutomationComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly automationService = inject(PointsAutomationService);

  readonly config = signal<PointsAutomationConfig | null>(null);
  readonly activeCategory = signal<AutomationCategory>('performance');
  readonly loading = signal(true);
  readonly savingToggle = signal(false);

  readonly performanceRanges = computed(
    () => this.config()?.performance.ranges ?? []
  );
  readonly taskCompletionRanges = computed(
    () => this.config()?.tasks.completionRanges ?? []
  );
  readonly taskDeadlineRules = computed(
    () => this.config()?.tasks.deadlineRules ?? []
  );
  readonly attendanceRanges = computed(
    () => this.config()?.attendance.attendanceRanges ?? []
  );
  readonly latenessRanges = computed(
    () => this.config()?.attendance.latenessRanges ?? []
  );
  readonly onTimeBonus = computed(
    () => this.config()?.attendance.onTimeBonus ?? 0
  );

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
        this.config.set(config);
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

  selectCategory(category: AutomationCategory): void {
    this.activeCategory.set(category);
  }

  toggleCategory(category: AutomationCategory, enabled: boolean): void {
    const current = this.config();
    if (!current || this.savingToggle()) {
      return;
    }

    // Only one category can be enabled at a time.
    const next: PointsAutomationConfig = {
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
    };

    this.activeCategory.set(category);
    this.savingToggle.set(true);
    this.automationService.saveConfig(next).subscribe({
      next: (saved) => {
        this.config.set(saved);
        this.savingToggle.set(false);
      },
      error: (err: unknown) => {
        console.error('Failed to save toggle:', err);
        this.savingToggle.set(false);
      },
    });
  }

  formatSignedPoints(type: 'Reward' | 'Deduction', value: number): string {
    return type === 'Reward' ? `+ ${value}` : `- ${value}`;
  }

  navigateToSetup(): void {
    void this.router.navigate(['/points/automation/setup'], {
      queryParams: { category: this.activeCategory() },
    });
  }
}
