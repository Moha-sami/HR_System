import { Routes } from '@angular/router';
import { JobManagementComponent } from './components/job-management/job-management.component';
import { JobCreateComponent } from './components/job-create/job-create.component';

export const JOBS_ROUTES: Routes = [
  {
    path: '',
    component: JobManagementComponent,
  },
  {
    path: 'create',
    component: JobCreateComponent,
  },
  {
    path: 'edit/:id',
    component: JobCreateComponent,
  },
];
