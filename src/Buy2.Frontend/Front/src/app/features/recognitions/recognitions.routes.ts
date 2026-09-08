import type { Routes } from '@angular/router';
import { unsavedChangesGuard } from '../news/guards/unsaved-changes.guard';
import { RecognitionListComponent } from './components/recognition-list/recognition-list.component';
import { RecognitionFormComponent } from './components/recognition-form/recognition-form.component';
import { RecognitionViewComponent } from './components/recognition-view/recognition-view.component';

export const RECOGNITIONS_ROUTES: Routes = [
  { path: '', component: RecognitionListComponent },
  { path: 'create', component: RecognitionFormComponent, canDeactivate: [unsavedChangesGuard] },
  { path: 'edit/:id', component: RecognitionFormComponent, canDeactivate: [unsavedChangesGuard] },
  { path: ':id', component: RecognitionViewComponent },
];
