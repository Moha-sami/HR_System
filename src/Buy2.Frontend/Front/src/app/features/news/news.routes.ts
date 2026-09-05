import type { Routes } from '@angular/router';
import { NewsListComponent } from './components/news-list/news-list.component';
import { NewsFormComponent } from './components/news-form/news-form.component';
import { NewsViewComponent } from './components/news-view/news-view.component';
import { unsavedChangesGuard } from './guards/unsaved-changes.guard';

export const NEWS_ROUTES: Routes = [
  { path: '', component: NewsListComponent },
  { path: 'create', component: NewsFormComponent, canDeactivate: [unsavedChangesGuard] },
  { path: 'edit/:id', component: NewsFormComponent, canDeactivate: [unsavedChangesGuard] },
  { path: ':id', component: NewsViewComponent },
];
