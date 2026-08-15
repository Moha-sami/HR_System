/**
 * Declarative UI config for the 6 permission groups shown in the Create / Edit forms.
 * Source: Figma frames 6377:104558 + 6380:105823.
 *
 * Each toggle pill's wire-format key is `${group.id}.${toggle}` (e.g. `employee.add`).
 * Access sub-sections emit `${scope}.access.type.${type}` and `${scope}.group.${id}`.
 */
export interface PermissionGroup {
  readonly id: GroupId;
  readonly title: string;
  readonly toggles: readonly string[];
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
    toggles: ['add', 'edit', 'delete', 'suspend', 'admin_access'],
    access: {
      scope: 'employee',
      types: ['all', 'department', 'region', 'sites', 'teams'],
      groupsLabel: 'Choose Groups',
    },
  },
  {
    id: 'job',
    title: 'Job Management',
    toggles: ['add', 'edit', 'delete'],
  },
  {
    id: 'site',
    title: 'Site Management',
    toggles: ['add', 'edit', 'delete', 'shifts'],
    access: {
      scope: 'site',
      types: ['all', 'region', 'specific'],
      groupsLabel: 'Choose Regions / Sites',
    },
  },
  {
    id: 'points',
    title: 'Points Management',
    toggles: ['add_transaction', 'automation', 'view_transactions'],
  },
  {
    id: 'notifications',
    title: 'Notifications Management',
    toggles: ['send'],
  },
  {
    id: 'rewards',
    title: 'Reward Management',
    toggles: ['add', 'edit', 'delete', 'inventory'],
  },
] as const;
