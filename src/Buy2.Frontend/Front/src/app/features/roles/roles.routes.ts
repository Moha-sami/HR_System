import type { Routes } from '@angular/router';
import { RoleListComponent } from './role-list/role-list.component';
import { RoleFormComponent } from './role-form/role-form.component';

export const ROLES_ROUTES: Routes = [
  { path: '', component: RoleListComponent },
  { path: 'create', component: RoleFormComponent },
  { path: 'edit/:id', component: RoleFormComponent },
];
