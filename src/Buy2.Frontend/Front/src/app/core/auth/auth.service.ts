import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { type Observable, tap } from 'rxjs';
import { TokenService } from './token.service';
import type { LoginRequest, LoginResponse, ResetPasswordRequest } from '../models/auth.models';

const API_BASE = '/api/v1/auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenSvc = inject(TokenService);

  readonly isAuthenticated = this.tokenSvc.isAuthenticated;
  readonly currentUser = this.tokenSvc.user;

  /** Login — POST /api/v1/auth/login */
  login(req: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${API_BASE}/login`, req).pipe(
      tap((res) => {
        this.tokenSvc.setToken(res.token);
      }),
    );
  }

  /** Logout — clear token */
  logout(): void {
    this.tokenSvc.clearToken();
  }

  /** Reset password — POST /api/v1/auth/password/reset */
  resetPassword(req: ResetPasswordRequest): Observable<boolean> {
    return this.http.post<boolean>(`${API_BASE}/password/reset`, req);
  }
}
