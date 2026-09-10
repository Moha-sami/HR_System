import type { Routes } from '@angular/router';
import { ShiftsPlaceholderComponent } from './shifts-placeholder/shifts-placeholder.component';

export const SHIFTS_ROUTES: Routes = [
  { path: '', redirectTo: 'shift-templates', pathMatch: 'full' },
  {
    path: 'shift-templates',
    component: ShiftsPlaceholderComponent,
    data: { titleKey: 'LAYOUT.NAV.SHIFT_TEMPLATES' },
  },
  {
    path: 'shift-management',
    component: ShiftsPlaceholderComponent,
    data: { titleKey: 'LAYOUT.NAV.SHIFT_MANAGEMENT' },
  },
  {
    path: 'employee-assignment',
    component: ShiftsPlaceholderComponent,
    data: { titleKey: 'LAYOUT.NAV.EMPLOYEE_ASSIGNMENT' },
  },
  {
    path: 'shift-market',
    component: ShiftsPlaceholderComponent,
    data: { titleKey: 'LAYOUT.NAV.SHIFT_MARKET' },
  },
];
