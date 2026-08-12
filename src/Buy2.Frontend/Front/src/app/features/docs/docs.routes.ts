import type { Routes } from '@angular/router';
import { DocsHomeComponent } from './docs-home/docs-home.component';

/**
 * Documentation Routes — Dev only
 * Access at: /docs (ng serve only, blocked in production)
 */
export const DOCS_ROUTES: Routes = [
  {
    path: '',
    component: DocsHomeComponent,
  },
  {
    path: 'button',
    loadComponent: () =>
      import('./button/button-docs.component').then((m) => m.ButtonDocsComponent),
  },
  {
    path: 'table',
    loadComponent: () => import('./table/table-docs.component').then((m) => m.TableDocsComponent),
  },
  {
    path: 'modal',
    loadComponent: () => import('./modal/modal-docs.component').then((m) => m.ModalDocsComponent),
  },
  {
    path: 'design-system',
    loadComponent: () =>
      import('./design-system/design-system-docs.component').then(
        (m) => m.DesignSystemDocsComponent,
      ),
  },
  {
    path: 'pagination',
    loadComponent: () =>
      import('./pagination/pagination-docs/pagination-docs').then(
        (m) => m.PaginationDocs
      ),
  },
];
