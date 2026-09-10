import type { Routes } from '@angular/router';
import { ShiftsPlaceholderComponent } from './shifts-placeholder/shifts-placeholder.component';
import { ShiftTemplateListComponent } from './shift-templates/shift-template-list/shift-template-list.component';

export const SHIFTS_ROUTES: Routes = [
  { path: '', redirectTo: 'shift-templates', pathMatch: 'full' },
  {
    path: 'shift-templates',
    component: ShiftTemplateListComponent,
    data: { titleKey: 'LAYOUT.NAV.SHIFT_TEMPLATES' },
  },
  // Ticket #327 replaces this placeholder with the real template editor.
  {
    path: 'shift-templates/new',
    component: ShiftsPlaceholderComponent,
    data: { titleKey: 'COMMON.CREATE' },
  },
  // Ticket #327 replaces this placeholder with the real template editor.
  {
    path: 'shift-templates/:id/edit',
    component: ShiftsPlaceholderComponent,
    data: { titleKey: 'COMMON.EDIT' },
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
