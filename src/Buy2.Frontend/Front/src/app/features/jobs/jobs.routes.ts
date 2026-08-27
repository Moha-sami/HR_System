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
  {
    path: 'details/:id',
    loadComponent: () => import('./components/job-details/job-details').then(c => c.JobDetails),
    children: [
      { path: '', redirectTo: 'information', pathMatch: 'full' },
      { path: 'information', loadComponent: () => import('./components/job-details/job-information/job-information').then(c => c.JobInformation) },
      { path: 'employees', loadComponent: () => import('./components/job-details/job-employees/job-employees').then(c => c.JobEmployees) }
    ]
  },
  {
    path: 'edit-task/:jobId/:taskId',
    loadComponent: () => import('./components/edit-task/edit-task').then(c => c.EditTask),
  }
];
