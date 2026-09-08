import {
  Component,
  computed,
  effect,
  inject,
  signal,
  type TemplateRef,
  ViewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Router } from '@angular/router';
import { TableComponent, type ColumnDef } from '@app/shared/components/table/table.component';
import { ButtonComponent } from '@app/shared/components/button/button.component';
import { EmployeeDetailService } from '../../../../services/employee-detail.service';
import type {
  ViolationFilters,
  ViolationSortField,
  ViolationSortDirection,
} from '../../../../models/view-employee/employee-violations';

@Component({
  selector: 'app-violations-tab',
  standalone: true,
  imports: [CommonModule, FormsModule, TableComponent, ButtonComponent, TranslatePipe],
  templateUrl: './violations-tab.component.html',
})
export class ViolationsTabComponent {
  private readonly employeeDetailService = inject(EmployeeDetailService);
  private readonly translate = inject(TranslateService);
  readonly router = inject(Router);

  @ViewChild('dateTemplate') dateTemplate!: TemplateRef<any>;
  @ViewChild('descriptionTemplate') descriptionTemplate!: TemplateRef<any>;
  @ViewChild('severityTemplate') severityTemplate!: TemplateRef<any>;
  @ViewChild('statusTemplate') statusTemplate!: TemplateRef<any>;
  @ViewChild('actionsTemplate') actionsTemplate!: TemplateRef<any>;

  readonly employee = this.employeeDetailService.detailEmployee;
  readonly violations = this.employeeDetailService.violations;
  readonly violationsLoading = this.employeeDetailService.violationsLoading;
  readonly violationsError = this.employeeDetailService.violationsError;

  // Filter state
  readonly filters = signal<ViolationFilters>({});
  readonly availableTypes = signal<string[]>([]);
  readonly availableSeverities = signal<string[]>(['Low', 'Medium', 'High', 'Critical']);

  // Sort state
  readonly sortField = signal<ViolationSortField>('createdAt');
  readonly sortDirection = signal<ViolationSortDirection>('desc');

  // Track last loaded employee ID to prevent infinite loops
  readonly lastLoadedEmployeeId = signal<number | null>(null);

  // Cell templates map
  readonly cellTemplatesMap = signal<Map<string, TemplateRef<any>>>(new Map());

  // Computed sorted violations
  readonly sortedViolations = computed(() => {
    const violations = [...this.violations()];
    const field = this.sortField();
    const direction = this.sortDirection();

    return violations.sort((a, b) => {
      let aVal: string | number = '';
      let bVal: string | number = '';

      switch (field) {
        case 'createdAt':
          aVal = new Date(a.createdAt).getTime();
          bVal = new Date(b.createdAt).getTime();
          break;
        case 'type':
          aVal = a.type.toLowerCase();
          bVal = b.type.toLowerCase();
          break;
        case 'severity': {
          const severityOrder = { Low: 1, Medium: 2, High: 3, Critical: 4 };
          aVal = severityOrder[a.severity as keyof typeof severityOrder] || 0;
          bVal = severityOrder[b.severity as keyof typeof severityOrder] || 0;
          break;
        }
        case 'status':
          aVal = a.status.toLowerCase();
          bVal = b.status.toLowerCase();
          break;
      }

      if (aVal < bVal) return direction === 'asc' ? -1 : 1;
      if (aVal > bVal) return direction === 'asc' ? 1 : -1;
      return 0;
    });
  });

  // Table columns definition
  readonly columns = computed<ColumnDef[]>(() => {
    this.translate.onLangChange;

    return [
      {
        key: 'createdAt',
        label: this.translate.instant('EMPLOYEE_DETAIL.VIOLATIONS.TABLE.DATE'),
        sortable: true,
        width: '180px',
        template: 'dateTemplate',
      },
      {
        key: 'type',
        label: this.translate.instant('EMPLOYEE_DETAIL.VIOLATIONS.TABLE.TYPE'),
        sortable: true,
      },
      {
        key: 'description',
        label: this.translate.instant('EMPLOYEE_DETAIL.VIOLATIONS.TABLE.DESCRIPTION'),
        template: 'descriptionTemplate',
      },
      {
        key: 'reportedByName',
        label: this.translate.instant('EMPLOYEE_DETAIL.VIOLATIONS.TABLE.REPORTED_BY'),
      },
      {
        key: 'severity',
        label: this.translate.instant('EMPLOYEE_DETAIL.VIOLATIONS.TABLE.SEVERITY'),
        sortable: true,
        template: 'severityTemplate',
        align: 'center',
        width: '130px',
      },
      {
        key: 'status',
        label: this.translate.instant('EMPLOYEE_DETAIL.VIOLATIONS.TABLE.STATUS'),
        sortable: true,
        template: 'statusTemplate',
        align: 'center',
        width: '130px',
      },
      {
        key: 'actions',
        label: this.translate.instant('EMPLOYEE_DETAIL.VIOLATIONS.TABLE.ACTIONS'),
        template: 'actionsTemplate',
        align: 'center',
        width: '150px',
      },
    ];
  });

  constructor() {
    // Effect to load violations when employee changes (only once per employee)
    effect(() => {
      const emp = this.employee();
      if (emp && emp.id !== this.lastLoadedEmployeeId()) {
        this.lastLoadedEmployeeId.set(emp.id);
        this.loadViolations();
      }
    });

    // Effect to extract unique types from violations
    effect(() => {
      const violations = this.violations();
      if (violations.length > 0) {
        const types = [...new Set(violations.map((v) => v.type))].sort();
        this.availableTypes.set(types);
      }
    });
  }

  ngAfterViewInit(): void {
    this.cellTemplatesMap.set(
      new Map([
        ['dateTemplate', this.dateTemplate],
        ['descriptionTemplate', this.descriptionTemplate],
        ['severityTemplate', this.severityTemplate],
        ['statusTemplate', this.statusTemplate],
        ['actionsTemplate', this.actionsTemplate],
      ]),
    );
  }

  // Load violations with current filters
  loadViolations(): void {
    const emp = this.employee();
    if (emp) {
      this.employeeDetailService.loadViolations(emp.id, this.filters());
    }
  }

  // Handle filter change
  onFilterChange(): void {
    this.loadViolations();
  }

  // Handle sort change from table
  onSortChange(event: { column: string; direction: 'asc' | 'desc' }): void {
    const sortMap: Record<string, ViolationSortField> = {
      createdAt: 'createdAt',
      type: 'type',
      severity: 'severity',
      status: 'status',
    };
    const apiSort = sortMap[event.column] || 'createdAt';
    this.sortField.set(apiSort);
    this.sortDirection.set(event.direction);
  }

  // Handle sort header click (toggle direction on same field, reset to asc on new field)
  onSort(field: ViolationSortField): void {
    if (this.sortField() === field) {
      this.sortDirection.update((direction) => (direction === 'asc' ? 'desc' : 'asc'));
    } else {
      this.sortField.set(field);
      this.sortDirection.set('asc');
    }
  }

  // Export violations
  onExport(): void {
    const emp = this.employee();
    if (!emp) return;

    this.employeeDetailService.exportViolations(emp.id, this.filters()).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `employee_${emp.id}_violations.csv`;
        link.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => {
        console.error('Export failed');
      },
    });
  }

  // Clear all filters
  clearFilters(): void {
    this.filters.set({});
    this.loadViolations();
  }

  // Format date for display
  formatDate(isoString: string): string {
    const date = new Date(isoString);
    return date.toLocaleDateString('en-US', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
    });
  }

  // Get severity badge class
  getSeverityClass(severity: string): string {
    switch (severity.toLowerCase()) {
      case 'low':
        return 'px-2 py-1 text-xs font-medium rounded-full bg-green-100 text-green-700';
      case 'medium':
        return 'px-2 py-1 text-xs font-medium rounded-full bg-yellow-100 text-yellow-700';
      case 'high':
        return 'px-2 py-1 text-xs font-medium rounded-full bg-orange-100 text-orange-700';
      case 'critical':
        return 'px-2 py-1 text-xs font-medium rounded-full bg-red-100 text-red-700';
      default:
        return 'px-2 py-1 text-xs font-medium rounded-full bg-gray-100 text-gray-700';
    }
  }

  // Get status badge class
  getStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'pending':
        return 'px-2 py-1 text-xs font-medium rounded-full bg-gray-100 text-gray-700';
      case 'approved':
        return 'px-2 py-1 text-xs font-medium rounded-full bg-blue-100 text-blue-700';
      case 'rejected':
        return 'px-2 py-1 text-xs font-medium rounded-full bg-red-100 text-red-700';
      case 'resolved':
        return 'px-2 py-1 text-xs font-medium rounded-full bg-green-100 text-green-700';
      case 'underinvestigation':
        return 'px-2 py-1 text-xs font-medium rounded-full bg-purple-100 text-purple-700';
      default:
        return 'px-2 py-1 text-xs font-medium rounded-full bg-gray-100 text-gray-700';
    }
  }

  // Check if any filters are active
  hasActiveFilters(): boolean {
    const f = this.filters();
    return !!(f.type || f.severityLevel || f.dateFrom || f.dateTo);
  }

  readonly cellTemplates = this.cellTemplatesMap;
}
