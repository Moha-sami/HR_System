import type { Routes } from '@angular/router';
import { EmployeeListComponent } from './employee-list/employee-list.component';

export const EMPLOYEES_ROUTES: Routes = [
  { path: '', component: EmployeeListComponent },
];
