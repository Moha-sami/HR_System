import type { Routes } from '@angular/router';
import { RoleListComponent } from './role-list/role-list.component';
import { RoleCreateComponent } from './role-create/role-create.component';
import { RoleEditComponent } from './role-edit/role-edit.component';

export const ROLES_ROUTES: Routes = [
  { path: '', component: RoleListComponent },
  { path: 'create', component: RoleCreateComponent },
  { path: 'edit/:id', component: RoleEditComponent },
];
