import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { AuthService } from './auth.service';
import { TokenService } from './token.service';
import type { LoginRequest, LoginResponse } from '../models/auth.models';
import { environment } from '../../../environments/environment';

const AUTH_URL = `${environment.baseUrl}/auth`;

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let tokenSvc: TokenService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), AuthService, TokenService],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    tokenSvc = TestBed.inject(TokenService);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('login', () => {
    it('should POST to /api/v1/auth/login and store token', () => {
      const loginReq: LoginRequest = { email: 'test@test.com', password: 'pass123' };
      const loginRes: LoginResponse = {
        token: fakeJwt({ nameid: '42', email: 'test@test.com', role: 'Admin', exp: futureExp() }),
        userId: 42,
        email: 'test@test.com',
        role: 'Admin',
      };

      let result: LoginResponse | null = null;

      service.login(loginReq).subscribe((res) => {
        result = res;
      });

      const req = httpMock.expectOne(`${AUTH_URL}/login`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(loginReq);

      req.flush(loginRes);

      expect(result).toEqual(loginRes);
      expect(tokenSvc.token()).toBe(loginRes.token);
      expect(tokenSvc.isAuthenticated()).toBeTruthy();
      expect(tokenSvc.user()?.email).toBe(loginRes.email);
    });

    it('should propagate error on failed login', () => {
      const loginReq: LoginRequest = { email: 'bad@test.com', password: 'wrong' };

      let error: unknown = null;

      service.login(loginReq).subscribe({
        error: (err) => {
          error = err;
        },
      });

      const req = httpMock.expectOne(`${AUTH_URL}/login`);
      req.flush({ message: 'Invalid credentials' }, { status: 401, statusText: 'Unauthorized' });

      expect(error).toBeTruthy();
      expect(tokenSvc.token()).toBeNull();
      expect(tokenSvc.isAuthenticated()).toBeFalsy();
    });
  });

  describe('logout', () => {
    it('should clear token', () => {
      tokenSvc.setToken(fakeJwt({ nameid: '1', email: 'a@b.com', role: 'User', exp: futureExp() }));
      expect(tokenSvc.isAuthenticated()).toBeTruthy();

      service.logout();

      expect(tokenSvc.token()).toBeNull();
      expect(tokenSvc.isAuthenticated()).toBeFalsy();
    });
  });

  describe('resetPassword', () => {
    it('should POST to /api/v1/auth/password/reset', () => {
      const resetReq = { email: 'test@test.com', newPassword: 'newPass123' };

      let result: boolean | null = null;

      service.resetPassword(resetReq).subscribe((res) => {
        result = res;
      });

      const req = httpMock.expectOne(`${AUTH_URL}/password/reset`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(resetReq);

      req.flush(true);

      expect(result).toBeTruthy();
    });

    it('should return false on failure', () => {
      const resetReq = { email: 'test@test.com', newPassword: 'newPass123' };

      let result: boolean | null = null;

      service.resetPassword(resetReq).subscribe((res) => {
        result = res;
      });

      const req = httpMock.expectOne(`${AUTH_URL}/password/reset`);
      req.flush(false);

      expect(result).toBeFalsy();
    });

    it('should propagate error on network failure', () => {
      const resetReq = { email: 'test@test.com', newPassword: 'newPass123' };

      let error: unknown = null;

      service.resetPassword(resetReq).subscribe({
        error: (err) => {
          error = err;
        },
      });

      const req = httpMock.expectOne(`${AUTH_URL}/password/reset`);
      req.flush({ message: 'Server error' }, { status: 500, statusText: 'Internal Server Error' });

      expect(error).toBeTruthy();
    });
  });

  describe('reactive state exposure', () => {
    it('isAuthenticated should reflect TokenService', () => {
      expect(service.isAuthenticated()).toBeFalsy();

      tokenSvc.setToken(fakeJwt({ nameid: '1', email: 'a@b.com', role: 'User', exp: futureExp() }));
      expect(service.isAuthenticated()).toBeTruthy();
    });

    it('currentUser should reflect TokenService user', () => {
      expect(service.currentUser()).toBeNull();

      tokenSvc.setToken(fakeJwt({ nameid: '1', email: 'a@b.com', role: 'User', exp: futureExp() }));

      expect(service.currentUser()?.email).toBe('a@b.com');
      expect(service.currentUser()?.role).toBe('User');
    });
  });
});
