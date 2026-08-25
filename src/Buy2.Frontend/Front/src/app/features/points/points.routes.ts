import type { Routes } from '@angular/router';
import { PointsManagement } from './components/points-management';
import { AddTransactionComponent } from './components/add-transaction/add-transaction.component';
import { PointsAutomationComponent } from './components/points-automation/points-automation.component';

export const POINTS_ROUTES: Routes = [
  { path: '', component: PointsManagement },
  { path: 'add', component: AddTransactionComponent },
  { path: 'automation', component: PointsAutomationComponent },
];