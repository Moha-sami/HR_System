export type AutomationCategory = 'performance' | 'tasks' | 'attendance';

export type AutomationPeriod = 'daily' | 'weekly' | 'biWeekly' | 'monthly';

export type RangeType = 'Reward' | 'Deduction';

export interface PercentRangeRule {
  id: number;
  type: RangeType;
  from: number;
  to: number;
  value: number;
}

export interface DeadlineRule {
  id: number;
  priority: string;
  pointsPerDayDelay: number;
}

export interface LatenessRangeRule {
  id: number;
  fromMinutes: number;
  toMinutes: number;
  pointsDeduction: number;
}

export interface PerformanceAutomation {
  enabled: boolean;
  period: AutomationPeriod;
  ranges: PercentRangeRule[];
}

export interface TasksAutomation {
  enabled: boolean;
  period: AutomationPeriod;
  completionRanges: PercentRangeRule[];
  deadlineRules: DeadlineRule[];
}

export interface AttendanceAutomation {
  enabled: boolean;
  period: AutomationPeriod;
  attendanceRanges: PercentRangeRule[];
  latenessRanges: LatenessRangeRule[];
  onTimeBonus: number;
}

export interface PointsAutomationConfig {
  id: number;
  activeCategory: AutomationCategory;
  performance: PerformanceAutomation;
  tasks: TasksAutomation;
  attendance: AttendanceAutomation;
}
