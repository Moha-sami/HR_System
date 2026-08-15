import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { RoleService } from './role.service';
import type { Role } from '../models/role';
import { environment } from '../../../../environments/environment';

const ROLES_URL = `${environment.baseUrl}/roles`;

describe('RoleService', () => {
  let service: RoleService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), RoleService],
    });
    service = TestBed.inject(RoleService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  function sampleRoles(): Role[] {
    return [
      { id: 10, roleName: 'Admin', permissions: ['employee.add'] },
      { id: 11, roleName: 'Viewer', permissions: [] },
    ];
  }

  function flushRoles(roles: Role[]): void {
    const req = httpMock.expectOne(ROLES_URL);
    expect(req.request.method).toBe('GET');
    req.flush(roles);
  }

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('loadAll', () => {
    it('should GET roles on success and update signal', () => {
      service.loadAll();

      expect(service.loading()).toBe(true);
      expect(service.error()).toBeNull();

      flushRoles(sampleRoles());

      expect(service.roles()).toEqual(sampleRoles());
      expect(service.loading()).toBe(false);
      expect(service.mockMode()).toBe(false);
      expect(service.error()).toBeNull();
    });

    it('should fall back to mock roles and flag mockMode on HTTP error', () => {
      service.loadAll();

      const req = httpMock.expectOne(ROLES_URL);
      req.flush({ message: 'Not Found' }, { status: 404, statusText: 'Not Found' });

      expect(service.roles().length).toBeGreaterThan(0);
      expect(service.mockMode()).toBe(true);
      expect(service.loading()).toBe(false);
      // catchError absorbs the 404 into the mock fallback, so the user-facing
      // error signal stays null — the mock itself is the recovery.
      expect(service.error()).toBeNull();
    });

    it('should flag mockMode when the network fails before subscribe', () => {
      service.loadAll();

      const req = httpMock.expectOne(ROLES_URL);
      req.error(new ProgressEvent('error'));

      expect(service.mockMode()).toBe(true);
      expect(service.loading()).toBe(false);
      expect(service.roles().length).toBeGreaterThan(0);
    });

    it('should not re-issue the request while one is in flight', () => {
      service.loadAll();
      service.loadAll();

      const req = httpMock.expectOne(ROLES_URL);
      req.flush([]);
    });
  });

  describe('create', () => {
    it('should POST {roleName, permissions} and append to signal', () => {
      const input = { roleName: 'New Role', permissions: ['employee.add', 'site.edit'] };
      const responseBody: Role = { id: 99, ...input };

      let created: Role | null = null;
      service.create(input).subscribe((role) => {
        created = role;
      });

      const req = httpMock.expectOne(ROLES_URL);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(input);
      req.flush(responseBody);

      expect(created).toEqual(responseBody);
      expect(service.roles().map((r) => r.id)).toContain(99);
    });

    it('should propagate server errors', () => {
      const input = { roleName: 'Bad', permissions: [] };
      let error: unknown = null;

      service.create(input).subscribe({ error: (err) => (error = err) });

      const req = httpMock.expectOne(ROLES_URL);
      req.flush({ message: 'Validation failed' }, { status: 400, statusText: 'Bad Request' });

      expect(error).toBeTruthy();
    });
  });

  describe('remove', () => {
    it('should DELETE :id and remove from signal', () => {
      service.loadAll();
      flushRoles([
        { id: 1, roleName: 'A', permissions: [] },
        { id: 2, roleName: 'B', permissions: [] },
      ]);
      expect(service.roles().length).toBe(2);

      let done = false;
      service.remove(1).subscribe(() => (done = true));

      const req = httpMock.expectOne(`${ROLES_URL}/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);

      expect(done).toBe(true);
      expect(service.roles().map((r) => r.id)).toEqual([2]);
    });
  });

  describe('update', () => {
    it('should throw because backend PUT is not implemented yet', () => {
      expect(() => service.update(1, { roleName: 'X', permissions: [] })).toThrowError(
        /UpdateRole endpoint is not implemented/,
      );
    });

    it('should include role id and name in the thrown error', () => {
      const call = () => service.update(42, { roleName: 'Facilitator', permissions: [] });
      expect(call).toThrowError(/id=42/);
      expect(call).toThrowError(/Facilitator/);
    });
  });
});
