import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { forkJoin, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type {
  CreatePointsTransactionInput,
  PointTableRow,
  PointsEmployee,
  PointsTransactionResponse,
} from '../models/points-transaction';

interface EmployeeResponse {
  id: string | number;
  firstName: string;
  lastName: string;
}

interface PointsRuleResponse {
  id: string | number;
  ruleKey: string;
}

@Injectable({
  providedIn: 'root',
})
export class PointsManagementService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.jsonServerUrl;

  getTransactions(): Observable<PointTableRow[]> {
    return forkJoin({
      transactions: this.http.get<PointsTransactionResponse[]>(
        `${this.apiUrl}/pointsTransactions`
      ),
      employees: this.http.get<EmployeeResponse[]>(`${this.apiUrl}/employees`),
      rules: this.http.get<PointsRuleResponse[]>(`${this.apiUrl}/pointsRules`),
    }).pipe(
      map(({ transactions, employees, rules }) =>
        transactions.map((transaction) => {
          const employee = employees.find(
            (item) => String(item.id) === String(transaction.employeeId)
          );

          const rule = rules.find(
            (item) => String(item.id) === String(transaction.pointsRuleId)
          );

          const createdAt = new Date(transaction.createdAt);

          return {
            id: String(transaction.id),
            name: employee
              ? `${employee.firstName} ${employee.lastName}`
              : 'Unknown employee',
            date: this.formatDate(createdAt),
            time: this.formatTime(createdAt),
            month: transaction.createdAt.slice(0, 7),
            createdAt: transaction.createdAt,
            type: this.formatType(transaction.transactionType),
            points: transaction.amount,
            triggeredBy: rule
              ? this.formatRuleName(rule.ruleKey)
              : 'Manual adjustment',
            transactionType: transaction.transactionType,
            comments: transaction.comments?.trim() || '—',
          };
        })
      )
    );
  }

  getEmployees(): Observable<PointsEmployee[]> {
    return this.http.get<EmployeeResponse[]>(`${this.apiUrl}/employees`).pipe(
      map((employees) =>
        employees.map((employee) => ({
          id: String(employee.id),
          firstName: employee.firstName,
          lastName: employee.lastName,
        }))
      )
    );
  }

  createTransaction(
    input: CreatePointsTransactionInput
  ): Observable<PointsTransactionResponse> {
    const amount =
      input.type === 'Add' ? Math.abs(input.pointsValue) : -Math.abs(input.pointsValue);

    const payload: Omit<PointsTransactionResponse, 'id'> = {
      employeeId: Number(input.employeeId) || input.employeeId,
      pointsRuleId: null,
      amount,
      transactionType: 'ManualAdjustment',
      comments: input.comments.trim(),
      createdAt: new Date().toISOString(),
    };

    return this.http.post<PointsTransactionResponse>(
      `${this.apiUrl}/pointsTransactions`,
      payload
    );
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

  private formatType(transactionType: string): string {
    switch (transactionType) {
      case 'Credit':
      case 'Debit':
        return 'Event';
      case 'ManualAdjustment':
        return 'Manual';
      default:
        return transactionType;
    }
  }

  private formatRuleName(ruleKey: string): string {
    return ruleKey
      .toLowerCase()
      .split('_')
      .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ');
  }
}