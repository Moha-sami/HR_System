import { Component, inject, effect, signal } from '@angular/core';
import { CommonModule, DOCUMENT } from '@angular/common';
import { Router, NavigationEnd, RouterLink } from '@angular/router';
import { filter } from 'rxjs/operators';
import { TranslatePipe } from '@ngx-translate/core';

interface BreadcrumbItem {
  label: string;
  url: string;
  isActive: boolean;
  translationKey?: string;
}

const routeTranslationKeys: Record<string, string> = {
  '/dashboard': 'LAYOUT.NAV.DASHBOARD',
  '/employees': 'LAYOUT.NAV.EMPLOYEE_MANAGEMENT',
  '/employees/add': 'COMMON.CREATE',
  '/employees/edit': 'COMMON.EDIT',
  '/jobs': 'LAYOUT.NAV.JOB_MANAGEMENT',
  '/jobs/create': 'COMMON.CREATE',
  '/jobs/edit': 'COMMON.EDIT',
  '/roles': 'LAYOUT.NAV.ROLE_MANAGMENT',
  '/roles/create': 'ROLES.CREATE.TITLE',
  '/roles/edit': 'ROLES.EDIT.TITLE',
  '/rewards': 'LAYOUT.NAV.REWARD_MANAGEMENT',
  '/points': 'LAYOUT.NAV.POINTS_MANAGEMENT',
  '/sites': 'LAYOUT.NAV.SITE_MANAGEMENT',
  '/requests': 'LAYOUT.NAV.REQUEST_MANAGEMENT',
  '/attendance': 'LAYOUT.NAV.TIME_AND_ATTENDANCE',
  '/notifications': 'LAYOUT.NAV.NOTIFICATIONS',
  '/scheduling': 'LAYOUT.NAV.SCHEDULING',
};

@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslatePipe],
  templateUrl: './breadcrumb.html',
})
export class BreadcrumbComponent {
  private router = inject(Router);
  private document = inject(DOCUMENT);
  items = signal<BreadcrumbItem[]>([]);

  get isRtl(): boolean {
    return this.document.documentElement.dir === 'rtl';
  }

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
      currentPath = `${currentPath}/${segment}`;
      const isLast = index === segments.length - 1;
      const translationKey = routeTranslationKeys[currentPath];
      breadcrumbs.push({
        label: '',
        url: currentPath,
        isActive: isLast,
        translationKey: translationKey || undefined,
      });
    });

    this.items.set(breadcrumbs);
  }
}
