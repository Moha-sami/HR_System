import type { Routes } from '@angular/router';
import { SiteListComponent } from './components/site-list/site-list.component';
import { SiteCreateComponent } from './components/site-create/site-create.component';

export const SITES_ROUTES: Routes = [
  { path: '', component: SiteListComponent },
  { path: 'create', component: SiteCreateComponent },
];
