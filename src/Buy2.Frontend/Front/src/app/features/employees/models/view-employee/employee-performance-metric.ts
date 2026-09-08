import type { PerformanceDateRangeResolved } from './employee-performance';

export interface PerformanceMonthlyTrendPoint {
  readonly year: number;
  readonly month: number;
  readonly yearMonthLabel: string;
  readonly averageScore: number;
  readonly submissionCount: number;
}

export interface PerformanceMetricSubmission {
  readonly id: number;
  readonly score: number;
  readonly submittedAt: string;
  readonly notes: string;
  readonly evaluatorName: string | null;
}

export interface EmployeePerformanceMetricDetail {
  readonly employeeId: number;
  readonly metricId: number;
  readonly metricName: string;
  readonly metricDescription: string;
  readonly weight: number;
  readonly targetScore: number;
  readonly unit: string;
  readonly allTimeAverageScore: number;
  readonly periodAverageScore: number;
  readonly periodRatingLabel: string;
  readonly dateRangeResolved: PerformanceDateRangeResolved;
  readonly monthlyTrends: readonly PerformanceMonthlyTrendPoint[];
  readonly submissions: readonly PerformanceMetricSubmission[];
}
