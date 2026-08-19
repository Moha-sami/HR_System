import type { Routes } from '@angular/router';
import { PointsAutomationComponent } from './components/points-automation/points-automation.component';
import { PointsAutomationSetupComponent } from './components/points-automation-setup/points-automation-setup.component';
import { PointsManagement } from './components/points-management';

export const POINTS_ROUTES: Routes = [
  { path: '', component: PointsManagement },
  { path: 'automation', component: PointsAutomationComponent },
  { path: 'automation/setup', component: PointsAutomationSetupComponent },
];
