/**
 * Create Employee Request
 * Used for employee onboarding/creation
 * Matches backend OnboardEmployeeCommand
 */
export interface InsertEmployee {
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly phoneNumber: string;
  readonly jobRoleId: number;
  readonly roleId: number;
  readonly siteId: number;
  readonly createdAt: string; // ISO date string
}
