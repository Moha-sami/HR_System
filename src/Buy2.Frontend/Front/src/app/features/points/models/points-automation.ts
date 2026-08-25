export type AutomationPeriod = 'daily' | 'weekly' | 'biweekly' | 'monthly';

export type PerformanceRangeType = 'Reward' | 'Deduction';

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

export interface PointsAutomationConfig {
  id: string | number;
  activeCategory: string;
  performance: PerformanceAutomation;
  tasks: unknown;
  attendance: unknown;
}

export interface PerformanceRangeDraft {
  id: number;
  type: PerformanceRangeType | '';
  from: number;
  to: number;
  value: number;
}
