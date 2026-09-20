import { Routes } from '@angular/router';

export const REQUESTS_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'types',
    pathMatch: 'full'
  },
  {
    path: 'types',
    loadComponent: () => import('./components/request-types-list/request-types-list.component').then(m => m.RequestTypesListComponent)
  },
  {
    path: 'types/add',
    loadComponent: () => import('./components/request-type-form/request-type-form.component').then(m => m.RequestTypeFormComponent)
  },
  {
    path: 'types/edit/:id',
    loadComponent: () => import('./components/request-type-form/request-type-form.component').then(m => m.RequestTypeFormComponent)
  }
];
