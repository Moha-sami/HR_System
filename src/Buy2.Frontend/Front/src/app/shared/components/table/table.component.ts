import { Component, input, output, signal, computed, type TemplateRef } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface ColumnDef {
  key: string;
  label: string;
  align?: 'left' | 'center' | 'right';
  width?: string;
  sortable?: boolean;
  template?: string;
}

export interface CellContext {
  $implicit: any;
  column: ColumnDef;
  rowIndex: number;
}

@Component({
  selector: 'app-table',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './table.component.html',
  styleUrl: './table.component.css',
})
export class TableComponent {
  columns = input<ColumnDef[]>([]);
  rows = input<any[]>([]);
  variant = input<'default' | 'striped' | 'bordered'>('default');
  size = input<'sm' | 'md' | 'lg'>('md');
  selectable = input<boolean>(false);
  emptyMessage = input<string>('No data available');
  cellTemplates = input<Map<string, TemplateRef<CellContext>>>(new Map());

  rowClick = output<any>();
  sortChange = output<{ column: string; direction: 'asc' | 'desc' }>();

  sortColumn = signal('');
  sortDirection = signal<'asc' | 'desc'>('asc');
  selectedRows = signal<Set<any>>(new Set());

  sortedRows = computed(() => {
    const rows = this.rows();
    const col = this.sortColumn();
    const dir = this.sortDirection();
    if (!col) return rows;
    return [...rows].sort((a, b) => {
      const aVal = a[col];
      const bVal = b[col];
      const comparison = String(aVal).localeCompare(String(bVal));
      return dir === 'asc' ? comparison : -comparison;
    });
  });

  getTemplate(column: ColumnDef): TemplateRef<CellContext> | null {
    if (!column.template) return null;
    return this.cellTemplates().get(column.template) || null;
  }

  getCellContext(row: any, column: ColumnDef, rowIndex: number): CellContext {
    return { $implicit: row, column, rowIndex };
  }

  onSort(column: ColumnDef): void {
    if (column.sortable !== true) return;
    if (this.sortColumn() === column.key) {
      this.sortDirection.set(this.sortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColumn.set(column.key);
      this.sortDirection.set('asc');
    }
    this.sortChange.emit({
      column: this.sortColumn(),
      direction: this.sortDirection(),
    });
  }

  onRowClick(row: any): void {
    this.rowClick.emit(row);
  }

  getSortIcon(column: ColumnDef): string {
    if (this.sortColumn() !== column.key) return '';
    return this.sortDirection();
  }

  getAlignClass(align?: string): string {
    switch (align) {
      case 'center':
        return 'text-center';
      case 'right':
        return 'text-right';
      default:
        return 'text-left';
    }
  }

  getGridTemplate(): string {
    return this.columns()
      .map((col) => col.width || '1fr')
      .join(' ');
  }

  getSizeClass(element: 'header' | 'row' | 'cell'): string {
    const sizes = {
      sm: { header: 'py-1 px-2', row: 'py-1 px-2', cell: 'py-1 px-2' },
      md: { header: 'py-3 px-4', row: 'py-3 px-4', cell: 'py-3 px-4' },
      lg: { header: 'py-4 px-6', row: 'py-4 px-6', cell: 'py-4 px-6' },
    };
    return sizes[this.size()][element];
  }

  getVariantClass(): string {
    switch (this.variant()) {
      case 'striped':
        return 'table-striped';
      case 'bordered':
        return 'table-bordered';
      default:
        return '';
    }
  }
}
