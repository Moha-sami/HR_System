import { TestBed } from '@angular/core/testing';
import { TokenService } from './token.service';
import { fakeJwt, futureExp, pastExp } from './test-helpers';

const VALID_TOKEN = fakeJwt({
  nameid: '42',
  email: 'admin@buy2.com',
  role: 'Admin',
  exp: futureExp(),
});

const EXPIRED_TOKEN = fakeJwt({
  nameid: '42',
  email: 'admin@buy2.com',
  role: 'Admin',
  exp: pastExp(),
});

describe('TokenService', () => {
  let service: TokenService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(TokenService);
  });

  afterEach(() => {
    localStorage.clear();
  });

  describe('initial state (no token)', () => {
    it('should be created', () => {
      expect(service).toBeTruthy();
    });

    it('token should be null', () => {
      expect(service.token()).toBeNull();
    });

    it('isAuthenticated should be false', () => {
      expect(service.isAuthenticated()).toBeFalsy();
    });

    it('user should be null', () => {
      expect(service.user()).toBeNull();
    });
  });

  describe('setToken', () => {
    it('should store token in localStorage', () => {
      service.setToken(VALID_TOKEN);
      expect(localStorage.getItem('hrms_token')).toBe(VALID_TOKEN);
    });

    it('should update token signal', () => {
      service.setToken(VALID_TOKEN);
      expect(service.token()).toBe(VALID_TOKEN);
    });

    it('should set isAuthenticated to true for valid token', () => {
      service.setToken(VALID_TOKEN);
      expect(service.isAuthenticated()).toBeTruthy();
    });

    it('should set isAuthenticated to false for expired token', () => {
      service.setToken(EXPIRED_TOKEN);
      expect(service.isAuthenticated()).toBeFalsy();
    });
  });

  describe('user decoding', () => {
    it('should decode userId from nameid claim', () => {
      service.setToken(VALID_TOKEN);
      expect(service.user()?.userId).toBe(42);
    });

    it('should decode email from token', () => {
      service.setToken(VALID_TOKEN);
      expect(service.user()?.email).toBe('admin@buy2.com');
    });

    it('should decode role from token', () => {
      service.setToken(VALID_TOKEN);
      expect(service.user()?.role).toBe('Admin');
    });

    // user() decodes the payload regardless of expiry so consumers can read claims
    // even after the session expired — only isAuthenticated gates requests.
    it('should still decode user payload after token expires', () => {
      service.setToken(EXPIRED_TOKEN);
      expect(service.user()).toBeTruthy();
      expect(service.isAuthenticated()).toBeFalsy();
    });
  });

  describe('clearToken', () => {
    it('should remove token from localStorage', () => {
      service.setToken(VALID_TOKEN);
      service.clearToken();
      expect(localStorage.getItem('hrms_token')).toBeNull();
    });

    it('should reset token signal to null', () => {
      service.setToken(VALID_TOKEN);
      service.clearToken();
      expect(service.token()).toBeNull();
    });

    it('should set isAuthenticated to false', () => {
      service.setToken(VALID_TOKEN);
      service.clearToken();
      expect(service.isAuthenticated()).toBeFalsy();
    });

    it('should set user to null', () => {
      service.setToken(VALID_TOKEN);
      service.clearToken();
      expect(service.user()).toBeNull();
    });
  });

  describe('getBearerToken', () => {
    it('should return null when no token', () => {
      expect(service.getBearerToken()).toBeNull();
    });

    it('should return raw token string', () => {
      service.setToken(VALID_TOKEN);
      expect(service.getBearerToken()).toBe(VALID_TOKEN);
    });
  });

  describe('edge cases', () => {
    it('should handle malformed JWT gracefully', () => {
      service.setToken('not.a.valid.jwt');
      expect(service.isAuthenticated()).toBeFalsy();
      expect(service.user()).toBeNull();
    });

    it('should treat token without exp claim as non-expiring', () => {
      const tokenNoExp = fakeJwt({
        nameid: '1',
        email: 'test@test.com',
        role: 'User',
      });
      service.setToken(tokenNoExp);
      expect(service.isAuthenticated()).toBeTruthy();
    });
  });
});
