import { Routes } from '@angular/router';
import { RewardListComponent } from './components/reward-list/reward-list.component';
import { RewardFormComponent } from './components/reward-form/reward-form.component';

export const REWARDS_ROUTES: Routes = [
  { path: '', component: RewardListComponent },
  { path: 'create', component: RewardFormComponent },
  { path: 'edit/:id', component: RewardFormComponent },
  {
    path: 'details/:id',
    loadComponent: () =>
      import('./components/reward-details/reward-details.component').then(
        (m) => m.RewardDetailsComponent,
      ),
  },
];
