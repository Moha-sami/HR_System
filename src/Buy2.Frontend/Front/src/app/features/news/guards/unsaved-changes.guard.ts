import { type CanDeactivateFn } from '@angular/router';
import type { Observable } from 'rxjs';

export interface HasUnsavedChanges {
  canDeactivate(): boolean | Observable<boolean>;
}

export const unsavedChangesGuard: CanDeactivateFn<HasUnsavedChanges> = (component) => {
  return component.canDeactivate();
};
