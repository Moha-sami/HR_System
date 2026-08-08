import { Component, ViewChild, signal, type TemplateRef, type AfterViewInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  TableComponent,
  type ColumnDef,
  type CellContext,
} from '../../../shared/components/table/table.component';

@Component({
  selector: 'app-table-docs',
  standalone: true,
  imports: [RouterLink, TableComponent],
  templateUrl: './table-docs.component.html',
  styleUrl: './table-docs.component.css',
})
export class TableDocsComponent implements AfterViewInit {
  // ──────────────────────────────────────────────────────
  // Template refs for custom cell examples
  // ──────────────────────────────────────────────────────
  @ViewChild('avatarTemplate') avatarTemplate!: TemplateRef<CellContext>;
  @ViewChild('statusTemplate') statusTemplate!: TemplateRef<CellContext>;
  @ViewChild('actionsTemplate') actionsTemplate!: TemplateRef<CellContext>;

  customTemplates = signal<Map<string, TemplateRef<CellContext>>>(new Map());

  ngAfterViewInit(): void {
    const map = new Map<string, TemplateRef<CellContext>>();
    map.set('avatarTemplate', this.avatarTemplate);
    map.set('statusTemplate', this.statusTemplate);
    map.set('actionsTemplate', this.actionsTemplate);
    this.customTemplates.set(map);
  }

  // ──────────────────────────────────────────────────────
  // Example 1: Basic Key-Value
  // ──────────────────────────────────────────────────────
  salaryColumns: ColumnDef[] = [
    { key: 'label', label: 'Detail', align: 'left' },
    { key: 'value', label: 'Amount', align: 'right' },
  ];

  salaryRows = [
    { label: 'Basic Salary', value: '5,000.00' },
    { label: 'Housing Allowance', value: '1,500.00' },
    { label: 'Transport Allowance', value: '500.00' },
    { label: 'Medical Insurance', value: '200.00' },
    { label: 'Social Insurance', value: '350.00' },
  ];

  // ──────────────────────────────────────────────────────
  // Example 2: Employee Table
  // ──────────────────────────────────────────────────────
  employeeColumns: ColumnDef[] = [
    { key: 'name', label: 'Name', align: 'left' },
    { key: 'position', label: 'Position', align: 'left' },
    { key: 'department', label: 'Department', align: 'left' },
    { key: 'salary', label: 'Salary', align: 'right' },
  ];

  employeeRows = [
    {
      name: 'Ahmed Mohamed',
      position: 'Senior Developer',
      department: 'Engineering',
      salary: '8,000',
    },
    { name: 'Sara Ali', position: 'UI/UX Designer', department: 'Design', salary: '6,500' },
    { name: 'Omar Hassan', position: 'Project Manager', department: 'Management', salary: '9,000' },
    {
      name: 'Fatma Ibrahim',
      position: 'HR Specialist',
      department: 'Human Resources',
      salary: '5,500',
    },
    {
      name: 'Khaled Youssef',
      position: 'DevOps Engineer',
      department: 'Engineering',
      salary: '7,500',
    },
  ];

  // ──────────────────────────────────────────────────────
  // Example 3: Custom Widths
  // ──────────────────────────────────────────────────────
  widthColumns: ColumnDef[] = [
    { key: 'code', label: 'Code', align: 'left', width: '100px' },
    { key: 'name', label: 'Product Name', align: 'left', width: '40%' },
    { key: 'category', label: 'Category', align: 'left', width: '150px' },
    { key: 'price', label: 'Price', align: 'right', width: '100px' },
    { key: 'stock', label: 'Stock', align: 'center', width: '80px' },
  ];

  productRows = [
    {
      code: 'P001',
      name: 'Laptop Dell XPS 15',
      category: 'Electronics',
      price: '1,200',
      stock: 45,
    },
    { code: 'P002', name: 'Wireless Mouse', category: 'Accessories', price: '25', stock: 200 },
    { code: 'P003', name: 'USB-C Hub', category: 'Accessories', price: '45', stock: 150 },
  ];

  // ──────────────────────────────────────────────────────
  // Example 4: Sortable Columns
  // ──────────────────────────────────────────────────────
  sortableColumns: ColumnDef[] = [
    { key: 'name', label: 'Name', align: 'left', sortable: true },
    { key: 'position', label: 'Position', align: 'left', sortable: true },
    { key: 'department', label: 'Department', align: 'left', sortable: false },
    { key: 'salary', label: 'Salary', align: 'right', sortable: true },
  ];

  // ──────────────────────────────────────────────────────
  // Example 5: Custom Cell Templates
  // ──────────────────────────────────────────────────────
  customColumns: ColumnDef[] = [
    { key: 'name', label: 'User', template: 'avatarTemplate' },
    { key: 'age', label: 'Age', align: 'center' },
    { key: 'phone', label: 'Phone' },
    { key: 'status', label: 'Status', align: 'center', template: 'statusTemplate' },
    {
      key: 'actions',
      label: 'Actions',
      align: 'center',
      template: 'actionsTemplate',
      width: '150px',
    },
  ];

  customRows = [
    {
      name: 'Ali Mohamed',
      age: 22,
      phone: '+20 123 456 7890',
      status: 'Active',
      avatar: 'https://i.pravatar.cc/150?img=1',
    },
    {
      name: 'Heba Ahmed',
      age: 28,
      phone: '+20 987 654 3210',
      status: 'Pending',
      avatar: 'https://i.pravatar.cc/150?img=5',
    },
    {
      name: 'Omar Hassan',
      age: 35,
      phone: '+20 555 123 4567',
      status: 'Inactive',
      avatar: 'https://i.pravatar.cc/150?img=3',
    },
  ];

  // ──────────────────────────────────────────────────────
  // Event Handlers
  // ──────────────────────────────────────────────────────
  lastClickedRow: any = null;
  lastSortChange: any = null;

  onRowClick(row: any): void {
    this.lastClickedRow = row;
  }

  onSortChange(event: { column: string; direction: 'asc' | 'desc' }): void {
    this.lastSortChange = event;
  }

  onEdit(row: any): void {
    console.log('Edit:', row);
  }

  onDelete(row: any): void {
    console.log('Delete:', row);
  }
}
