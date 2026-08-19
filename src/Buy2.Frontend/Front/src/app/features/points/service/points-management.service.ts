import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { forkJoin, map, type Observable } from 'rxjs';

interface PointsTransactionResponse {
  id: string | number;
  employeeId: string | number;
  pointsRuleId: string | number | null;
  amount: number;
  transactionType: string;
  createdAt: string;
}

interface EmployeeResponse {
  id: string | number;
  firstName: string;
  lastName: string;
}

interface PointsRuleResponse {
  id: string | number;
  ruleKey: string;
}

export interface PointTableRow {
  id: string;
  name: string;
  date: string;
  time: string;
  month: string;
  points: number;
  triggeredBy: string;
  transactionType: string;
  comments: string;
}

@Injectable({
  providedIn: 'root',
})
export class PointsManagementService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:3000';

  getTransactions(): Observable<PointTableRow[]> {
    return forkJoin({
      transactions: this.http.get<PointsTransactionResponse[]>(
        `${this.apiUrl}/pointsTransactions`
      ),
      employees: this.http.get<EmployeeResponse[]>(
        `${this.apiUrl}/employees`
      ),
      rules: this.http.get<PointsRuleResponse[]>(
        `${this.apiUrl}/pointsRules`
      ),
    }).pipe(
      map(({ transactions, employees, rules }) =>
        transactions.map((transaction) => {
          const employee = employees.find(
            (employee) =>
              String(employee.id) === String(transaction.employeeId)
          );

          const rule = rules.find(
            (rule) =>
              String(rule.id) === String(transaction.pointsRuleId)
          );

          const createdAt = new Date(transaction.createdAt);

          return {
            id: String(transaction.id),
            name: employee
              ? `${employee.firstName} ${employee.lastName}`
              : 'Unknown employee',
            date: createdAt.toLocaleDateString(),
            time: createdAt.toLocaleTimeString([], {
              hour: '2-digit',
              minute: '2-digit',
            }),
            month: transaction.createdAt.slice(0, 7),
            points: transaction.amount,
            triggeredBy: rule
              ? this.formatRuleName(rule.ruleKey)
              : 'Manual adjustment',
            transactionType: transaction.transactionType,
            comments: '—',
          };
        })
      )
    );
  }

  private formatRuleName(ruleKey: string): string {
    return ruleKey
      .toLowerCase()
      .split('_')
      .map(
        (word) =>
          word.charAt(0).toUpperCase() + word.slice(1)
      )
      .join(' ');
  }
}