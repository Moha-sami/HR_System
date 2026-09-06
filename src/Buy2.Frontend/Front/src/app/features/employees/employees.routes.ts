import type { Routes } from '@angular/router';
import { EmployeeListComponent } from './components/employee-list/employee-list.component';
import { EmployeeDetailComponent } from './components/employee-detail/employee-detail.component';
import { InformationTabComponent } from './components/employee-detail/tabs/information-tab/information-tab.component';
import { AttendanceTabComponent } from './components/employee-detail/tabs/attendance-tab/attendance-tab.component';
import { DocumentsTabComponent } from './components/employee-detail/tabs/documents-tab/documents-tab.component';
import { ViolationsTabComponent } from './components/employee-detail/tabs/violations-tab/violations-tab.component';
import { ViolationDetailPageComponent } from './components/employee-detail/tabs/violations-tab/violation-detail-page.component';
import { PointsRewardsTabComponent } from './components/employee-detail/tabs/points-rewards-tab/points-rewards-tab.component';

export const EMPLOYEES_ROUTES: Routes = [
  { path: '', component: EmployeeListComponent },
  {
    path: ':id',
    component: EmployeeDetailComponent,
    children: [
      { path: 'information', component: InformationTabComponent },
      { path: 'attendance', component: AttendanceTabComponent },
      { path: 'documents', component: DocumentsTabComponent },
      { path: 'violations', component: ViolationsTabComponent },
      { path: 'points-rewards', component: PointsRewardsTabComponent },
      { path: 'violations/:violationId', component: ViolationDetailPageComponent },
      { path: '', redirectTo: 'information', pathMatch: 'full' },
    ],
  },
];
