import type { Routes } from '@angular/router';
import { Login } from './login/login.component';
import { ForgotPassword } from './forgot-password/forgot-password.component';
import { authGuard } from '../../core/guards/auth.guard';

export const AUTH_ROUTES: Routes = [
  {
    path: '',
    children: [
      { path: 'login', component: Login },
      { path: 'reset-password', component: ForgotPassword, canActivate: [authGuard]},
      { path: '', redirectTo: 'login', pathMatch: 'full' },
      { path: '**', redirectTo: 'login', pathMatch: 'full' },
    ],
  },
];
