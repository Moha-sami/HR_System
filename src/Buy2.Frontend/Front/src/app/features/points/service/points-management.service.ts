import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { EmployeeService } from '../../employees/services/employee.service';
import type {
  CreateManualPointsTransactionResult,
  CreatePointsTransactionInput,
  PaginatedPointsTransactions,
  PointTableRow,
  PointsEmployee,
  PointsTransactionFilter,
  PointsTransactionListItem,
} from '../models/points-transaction';

const TRANSACTIONS_API = `${environment.baseUrl}/points/transactions`;

export function buildPointsTransactionParams(filter: PointsTransactionFilter): HttpParams {
  let params = new HttpParams()
    .set('pageNumber', filter.pageNumber.toString())
    .set('pageSize', filter.pageSize.toString());

  if (filter.searchTerm?.trim()) {
    params = params.set('searchTerm', filter.searchTerm.trim());
  }
  if (filter.triggeredBy?.trim()) {
    params = params.set('triggeredBy', filter.triggeredBy.trim());
  }
  if (filter.transactionType) {
    params = params.set('transactionType', filter.transactionType);
  }
  if (filter.sortBy) {
    params = params.set('sortBy', filter.sortBy);
  }
  if (filter.sortDir) {
    params = params.set('sortDir', filter.sortDir);
  }
  if (filter.month != null) {
    params = params.set('month', filter.month.toString());
  }
  if (filter.year != null) {
    params = params.set('year', filter.year.toString());
  }

  return params;
}

@Injectable({
  providedIn: 'root',
})
export class PointsManagementService {
  private readonly http = inject(HttpClient);
  private readonly employeeService = inject(EmployeeService);

  getTransactions(
    filter: PointsTransactionFilter,
  ): Observable<{ items: PointTableRow[]; totalCount: number; pageNumber: number; pageSize: number; totalPages: number }> {
    return this.http
      .get<PaginatedPointsTransactions>(TRANSACTIONS_API, {
        params: buildPointsTransactionParams(filter),
      })
      .pipe(
        map((response) => ({
          items: response.items.map((item) => this.toTableRow(item)),
          totalCount: response.totalCount,
          pageNumber: response.pageNumber,
          pageSize: response.pageSize,
          totalPages: response.totalPages,
        })),
      );
  }

  getEmployees(search?: string): Observable<PointsEmployee[]> {
    return this.employeeService
      .getEmployeesPaginated({
        page: 1,
        pageSize: 20,
        search: search?.trim() || null,
      })
      .pipe(
        map((response) =>
          response.items.map((employee) => ({
            id: employee.id,
            employeeName: employee.employeeName,
          })),
        ),
      );
  }

  createTransaction(
    input: CreatePointsTransactionInput,
  ): Observable<CreateManualPointsTransactionResult> {
    return this.http.post<CreateManualPointsTransactionResult>(TRANSACTIONS_API, {
      employeeId: input.employeeId,
      transactionType: input.transactionType,
      pointsValue: input.pointsValue,
      comments: input.comments.trim(),
    });
  }

  private toTableRow(item: PointsTransactionListItem): PointTableRow {
    const createdAt = new Date(item.createdAt);

    return {
      id: item.id,
      employeeName: item.employeeName,
      date: this.formatDate(createdAt),
      time: this.formatTime(createdAt),
      transactionType: item.transactionType,
      points: item.points,
      triggeredBy: item.triggeredBy,
      comments: item.comments?.trim() || '—',
    };
  }

  private formatDate(date: Date): string {
    return `${date.getDate()}-${date.getMonth() + 1}-${date.getFullYear()}`;
  }

  private formatTime(date: Date): string {
    return date
      .toLocaleTimeString('en-US', {
        hour: '2-digit',
        minute: '2-digit',
        hour12: true,
      })
      .toLowerCase();
  }
}
