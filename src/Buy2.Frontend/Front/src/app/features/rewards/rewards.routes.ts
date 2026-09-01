import { Routes } from '@angular/router';
import { RewardListComponent } from './components/reward-list/reward-list.component';
import { RewardFormComponent } from './components/reward-form/reward-form.component';
import { RewardDetailsComponent } from './components/reward-details/reward-details.component';

export const REWARDS_ROUTES: Routes = [
  { path: '', component: RewardListComponent },
  { path: 'create', component: RewardFormComponent },
  { path: 'edit/:id', component: RewardFormComponent },
  {
    path: 'details/:id',
    component: RewardDetailsComponent,
    children: [
      { path: '', redirectTo: 'information', pathMatch: 'full' },
      {
        path: 'information',
        loadComponent: () =>
          import('./components/reward-information/reward-information.component').then(
            (m) => m.RewardInformationComponent,
          ),
      },
      {
        path: 'inventory',
        loadComponent: () =>
          import('./components/reward-inventory/reward-inventory.component').then(
            (m) => m.RewardInventoryComponent,
          ),
      },
    ],
  },
];
