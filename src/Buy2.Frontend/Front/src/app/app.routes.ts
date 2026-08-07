import type { Routes } from '@angular/router';
// import { LayoutComponent } from './layout/layout.component';
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

  // // ──────────────────────────────────────────────────────────────────────────
  // // PUBLIC ROUTES — No layout shell, no auth required
  // // ──────────────────────────────────────────────────────────────────────────
  // {
  //   path: 'auth',
  //   children: [
  //     // §1.1 — Standard login with multi-tenant gateway
  //     { path: 'login', loadChildren: () => import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES) },

  //     // §1.1 — OTP-based password recovery flow
  //     { path: 'forgot-password', loadChildren: () => import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES) },

  //     // §1.1 — First-time employee password provisioning
  //     { path: 'initial-setup', loadChildren: () => import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES) },
  //   ]
  // },

  // // ──────────────────────────────────────────────────────────────────────────
  // // PROTECTED ROUTES — Wrapped in layout shell (sidebar + header)
  // // ──────────────────────────────────────────────────────────────────────────
  // {
  //   path: '',
  //   component: LayoutComponent,
  //   canActivate: [authGuard],
  //   children: [

  //     // Default redirect → dashboard
  //     { path: '', redirectTo: 'dashboard', pathMatch: 'full' },

  //     // ──────────────────────────────────────────────────────────────────────
  //     // §8 — Analytics & Executive Insights
  //     // ──────────────────────────────────────────────────────────────────────
  //     {
  //       path: 'dashboard',
  //       loadChildren: () => import('./features/dashboard/dashboard.routes').then(m => m.DASHBOARD_ROUTES)
  //     },

  //     // ──────────────────────────────────────────────────────────────────────
  //     // §2.2 — Comprehensive Employee Profiles
  //     // ──────────────────────────────────────────────────────────────────────
  //     {
  //       path: 'employees',
  //       loadChildren: () => import('./features/employees/employees.routes').then(m => m.EMPLOYEES_ROUTES)
  //     },

  //     // ──────────────────────────────────────────────────────────────────────
  //     // §2.1 — Job Management & Role Engineering
  //     // ──────────────────────────────────────────────────────────────────────
  //     {
  //       path: 'jobs',
  //       loadChildren: () => import('./features/jobs/jobs.routes').then(m => m.JOBS_ROUTES)
  //     },

  //     // ──────────────────────────────────────────────────────────────────────
  //     // §3.1 — Site & Location Management
  //     // ──────────────────────────────────────────────────────────────────────
  //     {
  //       path: 'sites',
  //       loadChildren: () => import('./features/sites/sites.routes').then(m => m.SITES_ROUTES)
  //     },

  //     // ──────────────────────────────────────────────────────────────────────
  //     // §3.2 — Attendance Profiles & Tracking
  //     // ──────────────────────────────────────────────────────────────────────
  //     {
  //       path: 'attendance',
  //       loadChildren: () => import('./features/attendance/attendance.routes').then(m => m.ATTENDANCE_ROUTES)
  //     },

  //     // ──────────────────────────────────────────────────────────────────────
  //     // §4 — Advanced Scheduling Engine
  //     // ──────────────────────────────────────────────────────────────────────
  //     {
  //       path: 'scheduling',
  //       loadChildren: () => import('./features/scheduling/scheduling.routes').then(m => m.SCHEDULING_ROUTES)
  //     },

  //     // ──────────────────────────────────────────────────────────────────────
  //     // §5 — Internal Shift Market (Claims)
  //     // ──────────────────────────────────────────────────────────────────────
  //     {
  //       path: 'shift-market',
  //       loadChildren: () => import('./features/shift-market/shift-market.routes').then(m => m.SHIFT_MARKET_ROUTES)
  //     },

  //     // ──────────────────────────────────────────────────────────────────────
  //     // §6 — Performance & Points Automation
  //     // ──────────────────────────────────────────────────────────────────────
  //     {
  //       path: 'gamification',
  //       loadChildren: () => import('./features/gamification/gamification.routes').then(m => m.GAMIFICATION_ROUTES)
  //     },

  //     // ──────────────────────────────────────────────────────────────────────
  //     // §7 — Rewards & Inventory Management
  //     // ──────────────────────────────────────────────────────────────────────
  //     {
  //       path: 'rewards',
  //       loadChildren: () => import('./features/rewards/rewards.routes').then(m => m.REWARDS_ROUTES)
  //     },

  //     // ──────────────────────────────────────────────────────────────────────
  //     // Org Structure — Department Management
  //     // ──────────────────────────────────────────────────────────────────────
  //     {
  //       path: 'departments',
  //       loadChildren: () => import('./features/departments/departments.routes').then(m => m.DEPARTMENTS_ROUTES)
  //     },

  //     // ──────────────────────────────────────────────────────────────────────
  //     // User Profile & Company Config
  //     // ──────────────────────────────────────────────────────────────────────
  //     {
  //       path: 'settings',
  //       loadChildren: () => import('./features/settings/settings.routes').then(m => m.SETTINGS_ROUTES)
  //     },
  //   ]
  // },

  // // ──────────────────────────────────────────────────────────────────────────
  // // WILDCARD — Unknown paths redirect to dashboard
  // // ──────────────────────────────────────────────────────────────────────────
  // { path: '**', redirectTo: '' }
];
