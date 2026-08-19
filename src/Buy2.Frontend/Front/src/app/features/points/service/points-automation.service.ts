import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import type { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type { PointsAutomationConfig } from '../models/points-automation';

@Injectable({
  providedIn: 'root',
})
export class PointsAutomationService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.jsonServerUrl}/pointsAutomation`;
  private readonly configId = 1;

  getConfig(): Observable<PointsAutomationConfig> {
    return this.http.get<PointsAutomationConfig>(`${this.apiUrl}/${this.configId}`);
  }

  saveConfig(config: PointsAutomationConfig): Observable<PointsAutomationConfig> {
    return this.http.put<PointsAutomationConfig>(`${this.apiUrl}/${this.configId}`, {
      ...config,
      id: this.configId,
    });
  }
}
