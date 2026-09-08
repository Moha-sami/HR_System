import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { AuthService } from '../../../core/auth/auth.service';
import { environment } from '../../../../environments/environment';
import { RecognitionService } from './recognition.service';
import type { RecognitionInput } from '../models/recognition.models';

describe('Recognition JSON Server contract', () => {
  let service: RecognitionService;
  let http: HttpTestingController;
  const url = `${environment.jsonServerUrl}/recognitions`;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(),
      { provide: AuthService, useValue: { currentUser: () => ({ email: 'hr@example.com', role: 'Admin' }) } }] });
    service = TestBed.inject(RecognitionService); http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('uses JSON Server for all CRUD operations', () => {
    service.list().subscribe(); http.expectOne(url).flush([]);
    service.get('abc').subscribe(); http.expectOne(`${url}/abc`).flush({ id: 'abc' });
    const body = { status: 'archived', title: 'Recognition' } as RecognitionInput;
    service.create(body).subscribe(); const post = http.expectOne(url); expect(post.request.method).toBe('POST'); expect(post.request.body).toEqual(body); post.flush({ id: 'abc', ...body });
    service.update('abc', body).subscribe(); const patch = http.expectOne(`${url}/abc`); expect(patch.request.method).toBe('PATCH'); patch.flush({ id: 'abc', ...body });
    service.delete('abc').subscribe(); const deletion = http.expectOne(`${url}/abc`); expect(deletion.request.method).toBe('DELETE'); deletion.flush({});
  });
  it('normalizes employee IDs and uses authenticated email', () => {
    service.employees().subscribe(items => expect(items[0].id).toBe('7'));
    http.expectOne(`${environment.jsonServerUrl}/employees`).flush([{ id: 7, firstName: 'A', lastName: 'B' }]);
    expect(service.identity()).toBe('hr@example.com');
  });
});
