import type { Routes } from '@angular/router';
import { devGuard } from './core/guards/dev.guard';
import { authGuard } from './core/guards/auth.guard';
import { Layout } from './layout/layout/layout';
// import { authGuard } from './core/guards/auth.guard';

/**
 * Application Routes — Buy2 HRMS
 *
 * Structure:
 *   /auth/*          → Public (no layout shell) — login, forgot-password, initial-setup
 *   /                → Protected (layout shell with sidebar + header)
 *     ├── /dashboard           §8  Analytics & Executive Insights
 *     ├── /employees/*         §2.2 Comprehensive Employee Profiles
 *     ├── /jobs/*              §2.1 Job Management & Role Engineering
 *     ├── /sites/*             §3.1 Site & Location Management
 *     ├── /attendance/*        §3.2 Attendance Profiles & Tracking
 *     ├── /scheduling/*        §4  Advanced Scheduling Engine
 *     ├── /shift-market/*      §5  Internal Shift Market (Claims)
 *     ├── /gamification/*      §6  Performance & Points Automation
 *     ├── /rewards/*           §7  Rewards & Inventory Management
 *     ├── /departments/*       Org Structure
 *     └── /settings/*          User Profile & Company Config
 */
export const routes: Routes = [
  // ──────────────────────────────────────────────────────────────────────────
  // DOCUMENTATION — Dev only (blocked in production)
  // ──────────────────────────────────────────────────────────────────────────
  {
    path: 'docs',
    canActivate: [devGuard],
    loadChildren: () => import('./features/docs/docs.routes').then((m) => m.DOCS_ROUTES),
  },

  // ──────────────────────────────────────────────────────────────────────────
  // PUBLIC ROUTES — No layout shell, no auth required
  // ──────────────────────────────────────────────────────────────────────────
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES),
  },

  // ──────────────────────────────────────────────────────────────────────────
  // PROTECTED ROUTES — Wrapped in Layout shell (sidebar + header)
  // ──────────────────────────────────────────────────────────────────────────
  {
    path: '',
    component: Layout,
    canActivate: [authGuard], // ← uncomment when auth is ready
    children: [
      // Default redirect → docs (temporary until dashboard is built)
      { path: '', redirectTo: 'docs', pathMatch: 'full' },

      // ──────────────────────────────────────────────────────────────────────
      // §8 — Analytics & Executive Insights
      // ──────────────────────────────────────────────────────────────────────
      // {
      //   path: 'dashboard',
      //   loadChildren: () => import('./features/dashboard/dashboard.routes').then(m => m.DASHBOARD_ROUTES)
      // },

      // ──────────────────────────────────────────────────────────────────────
      // §2.2 — Comprehensive Employee Profiles
      // ──────────────────────────────────────────────────────────────────────
      {
        path: 'employees',
        loadChildren: () =>
          import('./features/employees/employees.routes').then((m) => m.EMPLOYEES_ROUTES),
      },

      // ──────────────────────────────────────────────────────────────────────
      // §2.1 — Job Management & Role Engineering
      // ──────────────────────────────────────────────────────────────────────
      // {
      //   path: 'jobs',
      //   loadChildren: () => import('./features/jobs/jobs.routes').then(m => m.JOBS_ROUTES)
      // },

      // ──────────────────────────────────────────────────────────────────────
      // §3.1 — Site & Location Management
      // ──────────────────────────────────────────────────────────────────────
      // {
      //   path: 'sites',
      //   loadChildren: () => import('./features/sites/sites.routes').then(m => m.SITES_ROUTES)
      // },

      // ──────────────────────────────────────────────────────────────────────
      // §3.2 — Attendance Profiles & Tracking
      // ──────────────────────────────────────────────────────────────────────
      // {
      //   path: 'attendance',
      //   loadChildren: () => import('./features/attendance/attendance.routes').then(m => m.ATTENDANCE_ROUTES)
      // },

      // ──────────────────────────────────────────────────────────────────────
      // §4 — Advanced Scheduling Engine
      // ──────────────────────────────────────────────────────────────────────
      // {
      //   path: 'scheduling',
      //   loadChildren: () => import('./features/scheduling/scheduling.routes').then(m => m.SCHEDULING_ROUTES)
      // },

      // ──────────────────────────────────────────────────────────────────────
      // §5 — Internal Shift Market (Claims)
      // ──────────────────────────────────────────────────────────────────────
      // {
      //   path: 'shift-market',
      //   loadChildren: () => import('./features/shift-market/shift-market.routes').then(m => m.SHIFT_MARKET_ROUTES)
      // },

      // ──────────────────────────────────────────────────────────────────────
      // §6 — Performance & Points Automation
      // ──────────────────────────────────────────────────────────────────────
      // {
      //   path: 'gamification',
      //   loadChildren: () => import('./features/gamification/gamification.routes').then(m => m.GAMIFICATION_ROUTES)
      // },

      // ──────────────────────────────────────────────────────────────────────
      // §7 — Rewards & Inventory Management
      // ──────────────────────────────────────────────────────────────────────
      // {
      //   path: 'rewards',
      //   loadChildren: () => import('./features/rewards/rewards.routes').then(m => m.REWARDS_ROUTES)
      // },

      // ──────────────────────────────────────────────────────────────────────
      // RBAC — Role-Based Access Control
      // ──────────────────────────────────────────────────────────────────────
      {
        path: 'roles',
        loadChildren: () => import('./features/roles/roles.routes').then((m) => m.ROLES_ROUTES),
      },

      // ──────────────────────────────────────────────────────────────────────
      // User Profile & Company Config
      // ──────────────────────────────────────────────────────────────────────
      // {
      //   path: 'settings',
      //   loadChildren: () => import('./features/settings/settings.routes').then(m => m.SETTINGS_ROUTES)
      // },
    ],
  },

  // ──────────────────────────────────────────────────────────────────────────
  // WILDCARD — Unknown paths redirect to home
  // ──────────────────────────────────────────────────────────────────────────
  { path: '**', redirectTo: '' },
];
