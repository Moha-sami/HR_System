export type AutomationPeriod = 'daily' | 'weekly' | 'biweekly' | 'monthly';

export type AutomationCategory = 'performance' | 'tasks' | 'attendance';

export type PerformanceRangeType = 'Reward' | 'Deduction';

export type TaskPriority = 'Urgent' | 'High' | 'Medium' | 'Low';

export interface PerformanceRange {
  id: number;
  type: PerformanceRangeType;
  from: number;
  to: number;
  value: number;
}

export interface PerformanceAutomation {
  enabled: boolean;
  period: AutomationPeriod;
  ranges: PerformanceRange[];
}

export interface TaskDeadlineRule {
  id: number;
  priority: string;
  pointsPerDayDelay: number;
}

export interface TasksAutomation {
  enabled: boolean;
  period: AutomationPeriod;
  completionRanges: PerformanceRange[];
  deadlineRules: TaskDeadlineRule[];
}

export interface LatenessRange {
  id: number;
  fromMinutes: number;
  toMinutes: number;
  pointsDeduction: number;
}

export interface AttendanceAutomation {
  enabled: boolean;
  period: AutomationPeriod;
  attendanceRanges: PerformanceRange[];
  latenessRanges: LatenessRange[];
  onTimeBonus: number;
}

export interface PointsAutomationConfig {
  id: string | number;
  activeCategory: string;
  performance: PerformanceAutomation;
  tasks: TasksAutomation;
  attendance: AttendanceAutomation;
}

export interface PerformanceRangeDraft {
  id: number;
  type: PerformanceRangeType | '';
  from: number;
  to: number;
  value: number;
}

export interface TaskDeadlineDraft {
  id: number;
  priority: string;
  pointsPerDayDelay: number;
}

export interface LatenessRangeDraft {
  id: number;
  fromMinutes: number;
  toMinutes: number;
  pointsDeduction: number;
}
