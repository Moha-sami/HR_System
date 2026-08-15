import type { Routes } from '@angular/router';
import { AuthLayout } from './authLayout.component';
import { LoginComponent } from './login/login.component';
import { ForgotPassword } from './forgot-password/forgot-password.component';
import { authGuard } from '../../core/guards/auth.guard';

export const AUTH_ROUTES: Routes = [
  {
    path: '',
    component: AuthLayout,
    children: [
      { path: 'login', component: LoginComponent },
      { path: 'reset-password', component: ForgotPassword, canActivate: [authGuard] },
      { path: '', redirectTo: 'login', pathMatch: 'full' },
      { path: '**', redirectTo: 'login', pathMatch: 'full' },
    ],
  },
];
