import { Routes } from '@angular/router';
import { JobManagementComponent } from './components/job-management/job-management.component';

export const JOBS_ROUTES: Routes = [
  {
    path: '',
    component: JobManagementComponent,
  },
];
