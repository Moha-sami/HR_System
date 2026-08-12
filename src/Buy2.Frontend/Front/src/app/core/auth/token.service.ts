import { Injectable, signal, computed } from '@angular/core';
import type { User } from '../models/auth.models';

const TOKEN_KEY = 'hrms_token';
// Refresh tokens slightly before expiry to avoid in-flight requests racing the clock.
const EXPIRY_BUFFER_MS = 30_000;

@Injectable({ providedIn: 'root' })
export class TokenService {
  private readonly tokenState = signal<string | null>(this.loadToken());

  readonly token = this.tokenState.asReadonly();

  readonly isAuthenticated = computed(() => {
    const current = this.tokenState();
    return current !== null && !this.isExpired(current);
  });

  readonly user = computed<User | null>(() => {
    const current = this.tokenState();
    if (!current) return null;
    return this.decodeUser(current);
  });

  setToken(jwt: string): void {
    localStorage.setItem(TOKEN_KEY, jwt);
    this.tokenState.set(jwt);
  }

  clearToken(): void {
    localStorage.removeItem(TOKEN_KEY);
    this.tokenState.set(null);
  }

  getBearerToken(): string | null {
    return this.tokenState();
  }

  // ── Helpers ──────────────────────────────────────────

  private loadToken(): string | null {
    try {
      return localStorage.getItem(TOKEN_KEY);
    } catch {
      return null;
    }
  }

  private decodePayload<T = Record<string, unknown>>(jwt: string): T {
    const base64 = jwt.split('.')[1];
    const json = atob(base64.replace(/-/g, '+').replace(/_/g, '/'));
    return JSON.parse(json) as T;
  }

  private decodeUser(jwt: string): User | null {
    try {
      const payload = this.decodePayload<{
        nameid?: string;
        email?: string;
        role?: string;
      }>(jwt);

      return {
        userId: Number(payload.nameid ?? 0),
        email: payload.email ?? '',
        role: payload.role ?? '',
      };
    } catch {
      return null;
    }
  }

  private isExpired(jwt: string): boolean {
    try {
      const payload = this.decodePayload<{ exp?: number }>(jwt);
      if (!payload.exp) return false;
      return Date.now() >= payload.exp * 1000 - EXPIRY_BUFFER_MS;
    } catch {
      return true;
    }
  }
}
