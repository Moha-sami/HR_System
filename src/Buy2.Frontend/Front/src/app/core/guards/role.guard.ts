import { type CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { TokenService } from '../auth/token.service';

export const roleGuard = (requiredRole: string): CanActivateFn => {
  return () => {
    const tokenSvc = inject(TokenService);
    const router = inject(Router);


    if (tokenSvc.user()?.role !== requiredRole) {
      router.navigateByUrl('/');
      return false;
    }

    return true;
  };
};