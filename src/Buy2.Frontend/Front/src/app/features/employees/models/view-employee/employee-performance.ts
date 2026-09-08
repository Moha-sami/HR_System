export interface PerformanceFilters {
  readonly period?: string;
  readonly days?: number;
  readonly from?: string;
  readonly to?: string;
}

export interface PerformanceDateRangeResolved {
  readonly from: string;
  readonly to: string;
  readonly period: string | null;
}

export interface PerformanceTasksSummary {
  readonly totalTasks: number;
  readonly todoCount: number;
  readonly inProgressCount: number;
  readonly completedCount: number;
  readonly overdueCount: number;
  readonly deadlineCompliancePercentage: number;
}

export interface PerformanceAchievement {
  readonly id: number;
  readonly title: string;
  readonly description: string;
  readonly iconUrl: string | null;
  readonly pointsAwarded: number;
  readonly earnedAt: string;
}

export interface PerformanceChartPoint {
  readonly date: string;
  readonly score: number;
}

export interface PerformanceSubmissionDetail {
  readonly id: number;
  readonly metricName: string;
  readonly score: number;
  readonly weight: number;
  readonly submittedAt: string;
  readonly notes: string;
}

export interface EmployeePerformanceOverview {
  readonly employeeId: number;
  readonly dateRangeResolved: PerformanceDateRangeResolved;
  readonly overallWeightedScore: number;
  readonly ratingLabel: string;
  readonly tasksSummary: PerformanceTasksSummary;
  readonly achievements: readonly PerformanceAchievement[];
  readonly chartTrendPoints: readonly PerformanceChartPoint[];
  readonly submissionsDetail: readonly PerformanceSubmissionDetail[];
}
