import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, throwError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../core/auth/auth.service';
import type { Recognition, RecognitionEmployee, RecognitionInput } from '../models/recognition.models';

@Injectable({ providedIn: 'root' })
export class RecognitionService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly url = `${environment.jsonServerUrl}/recognitions`;

  identity(): string {
    const user = this.auth.currentUser();
    return user?.email || user?.role || String(user?.userId ?? '');
  }
  list() { return this.http.get<Recognition[]>(this.url); }
  get(id: string) { return this.http.get<Recognition>(`${this.url}/${encodeURIComponent(id)}`); }
  create(value: RecognitionInput) { return this.http.post<Recognition>(this.url, value); }
  update(id: string, value: Partial<RecognitionInput>) {
    return this.http.patch<Recognition>(`${this.url}/${encodeURIComponent(id)}`, value);
  }
  delete(id: string) { return this.http.delete<unknown>(`${this.url}/${encodeURIComponent(id)}`); }
  employees() {
    return this.http.get<RecognitionEmployee[]>(`${environment.jsonServerUrl}/employees`).pipe(
      map(items => items.map(item => ({ ...item, id: String(item.id) }))),
    );
  }

  /**
   * Recognition comments have no API contract in this application yet.  Keep the
   * boundary here so the view can handle the unavailable API consistently and a
   * documented endpoint can be wired in without putting HTTP logic in the UI.
   */
  getComments(_recognitionId: string) {
    return throwError(() => new Error('Recognition comments API is not available.'));
  }

  addComment(_recognitionId: string, _content: string) {
    return throwError(() => new Error('Recognition comments API is not available.'));
  }
}
