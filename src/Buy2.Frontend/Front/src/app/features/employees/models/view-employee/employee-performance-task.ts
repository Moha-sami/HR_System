export type EmployeePerformanceTaskStatus = 'Todo' | 'InProgress' | 'InReview' | 'Completed' | 'Overdue';

export interface EmployeePerformanceTask {
  readonly id: number;
  readonly employeeId: number;
  readonly title: string;
  readonly description: string | null;
  readonly status: EmployeePerformanceTaskStatus;
  readonly priority: string | null;
  readonly dueDate: string | null;
  readonly completedAt: string | null;
  readonly createdAt: string;
}
