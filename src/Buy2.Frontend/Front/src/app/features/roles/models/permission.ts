/**
 * Declarative UI config for the 6 permission groups shown in the Create / Edit forms.
 * Source: Figma frames 6377:104558 + 6380:105823.
 * Aligned with backend API format: ModulePermissionDto[]
 */

export interface PermissionGroup {
  readonly id: GroupId;
  readonly title: string;
  readonly actions: readonly string[];
  /** Sub-section for groups that scope access hierarchically (employee, site). */
  readonly access?: AccessSubsection;
}

export interface AccessSubsection {
  readonly scope: 'employee' | 'site';
  readonly types: readonly string[];
  readonly groupsLabel: string;
}

export type GroupId = 'employee' | 'job' | 'site' | 'points' | 'notifications' | 'rewards';

export const PERMISSION_GROUPS: readonly PermissionGroup[] = [
  {
    id: 'employee',
    title: 'Employee Management',
    actions: ['add', 'edit', 'delete', 'suspend', 'admin_access'],
    access: {
      scope: 'employee',
      types: ['all', 'department', 'region', 'sites', 'teams'],
      groupsLabel: 'Choose Groups',
    },
  },
  {
    id: 'job',
    title: 'Job Management',
    actions: ['add', 'edit', 'delete'],
  },
  {
    id: 'site',
    title: 'Site Management',
    actions: ['add', 'edit', 'delete', 'shifts'],
    access: {
      scope: 'site',
      types: ['all', 'region', 'specific'],
      groupsLabel: 'Choose Regions / Sites',
    },
  },
  {
    id: 'points',
    title: 'Points Management',
    actions: ['add_transaction', 'automation', 'view_transactions'],
  },
  {
    id: 'notifications',
    title: 'Notifications Management',
    actions: ['send'],
  },
  {
    id: 'rewards',
    title: 'Reward Management',
    actions: ['add', 'edit', 'delete', 'inventory'],
  },
] as const;
