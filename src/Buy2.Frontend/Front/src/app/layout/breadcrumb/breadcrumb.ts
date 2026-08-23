import { Component, inject, effect, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, NavigationEnd, RouterLink } from '@angular/router';
import { filter } from 'rxjs/operators';

interface BreadcrumbItem {
  label: string;
  url: string;
  isActive: boolean;
}

const routeLabels: Record<string, string> = {
  dashboard: 'Dashboard',
  employees: 'Employee Management',
  'employees/add': 'Add Employee',
  'employees/edit': 'Edit Employee',
  jobs: 'Job Management',
  'jobs/create': 'Create Job',
  'jobs/edit': 'Edit Job',
  roles: 'Role Management',
  'roles/create': 'Create Role',
  'roles/edit': 'Edit Role',
  rewards: 'Reward Management',
  points: 'Points Management',
  sites: 'Site Management',
  requests: 'Request Management',
  attendance: 'Time & Attendance',
  notifications: 'Notifications',
  scheduling: 'Scheduling',
};

@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './breadcrumb.html',
})
export class BreadcrumbComponent {
  private router = inject(Router);
  items = signal<BreadcrumbItem[]>([]);

  constructor() {
    effect(() => {
      this.router.events
        .pipe(filter((event) => event instanceof NavigationEnd))
        .subscribe(() => this.buildBreadcrumbs());
      this.buildBreadcrumbs();
    });
  }

  private buildBreadcrumbs(): void {
    const url = this.router.url;
    const segments = url.split('/').filter(Boolean);

    if (segments.length === 0) {
      this.items.set([]);
      return;
    }

    const breadcrumbs: BreadcrumbItem[] = [];

    let currentPath = '';
    segments.forEach((segment, index) => {
      currentPath += '/' + segment;
      const isLast = index === segments.length - 1;
      const label = routeLabels[currentPath] || this.formatLabel(segment);
      breadcrumbs.push({ label, url: currentPath, isActive: isLast });
    });

    this.items.set(breadcrumbs);
  }

  private formatLabel(segment: string): string {
    return segment
      .split('-')
      .map((w) => w.charAt(0).toUpperCase() + w.slice(1))
      .join(' ');
  }
}
