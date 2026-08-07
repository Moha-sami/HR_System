import { inject, isDevMode } from '@angular/core';
import { type CanActivateFn, Router } from '@angular/router';

/**
 * Dev Guard — blocks route access in production builds.
 * Use for documentation, debug panels, and dev-only features.
 */
export const devGuard: CanActivateFn = () => {
  if (!isDevMode()) {
    return inject(Router).createUrlTree(['/']);
  }
  return true;
};
