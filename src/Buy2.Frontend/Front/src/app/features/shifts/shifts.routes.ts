import type { Routes } from '@angular/router';
import { ShiftsPlaceholderComponent } from './shifts-placeholder/shifts-placeholder.component';
import { ShiftTemplateListComponent } from './shift-templates/shift-template-list/shift-template-list.component';
import { ShiftTemplateEditorComponent } from './shift-templates/shift-template-editor/shift-template-editor.component';

export const SHIFTS_ROUTES: Routes = [
  { path: '', redirectTo: 'shift-templates', pathMatch: 'full' },
  {
    path: 'shift-templates',
    component: ShiftTemplateListComponent,
    data: { titleKey: 'LAYOUT.NAV.SHIFT_TEMPLATES' },
  },
  {
    path: 'shift-templates/new',
    component: ShiftTemplateEditorComponent,
    data: { titleKey: 'COMMON.CREATE' },
  },
  {
    path: 'shift-templates/:id/edit',
    component: ShiftTemplateEditorComponent,
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
