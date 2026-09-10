import { Component, signal, inject, HostListener, DestroyRef, type OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { type AppLanguage, LanguageService } from '../../core/services/language.service';
import { BreadcrumbComponent } from '../breadcrumb/breadcrumb';

export interface NavChild {
  labelKey: string;
  route: string;
  disabled?: boolean;
}

export interface MenuItem {
  icon: string;
  labelKey: string;
  route: string;
  hasArrow?: boolean;
  children?: NavChild[];
}

function stripQueryFragment(url: string): string {
  return url.split('?')[0].split('#')[0];
}

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    FormsModule,
    TranslatePipe,
    BreadcrumbComponent,
  ],
  templateUrl: './layout.html',
  styleUrls: ['./layout.css']
})
export class Layout implements OnInit {
  private router = inject(Router);
  private languageService = inject(LanguageService);
  private destroyRef = inject(DestroyRef);

  // ===== STATE =====
  isSidebarOpen = signal(true);
  selectedLanguage = 'en';
  isMobileDevice = signal(false);

  // ===== LOGO PATH =====
  logoPath = '/buy2logo.png';


  // ===== MENU ITEMS =====
  menuItems: MenuItem[] = [
    { icon: 'svg-dashboard', labelKey: 'LAYOUT.NAV.DASHBOARD', route: '/dashboard' },
    { icon: 'svg-employee', labelKey: 'LAYOUT.NAV.EMPLOYEE_MANAGEMENT', route: '/employees' },
    { icon: 'svg-job', labelKey: 'LAYOUT.NAV.JOB_MANAGEMENT', route: '/jobs' },
    { icon: 'svg-role', labelKey: 'LAYOUT.NAV.ROLE_MANAGMENT', route: '/roles' },
    { icon: 'svg-reward', labelKey: 'LAYOUT.NAV.REWARD_MANAGEMENT', route: '/rewards' },
    { icon: 'svg-points', labelKey: 'LAYOUT.NAV.POINTS_MANAGEMENT', route: '/points' },
    { icon: 'svg-site', labelKey: 'LAYOUT.NAV.SITE_MANAGEMENT', route: '/sites' },
    { icon: 'svg-request', labelKey: 'LAYOUT.NAV.REQUEST_MANAGEMENT', route: '/requests', hasArrow: true },
    { icon: 'svg-time', labelKey: 'LAYOUT.NAV.TIME_AND_ATTENDANCE', route: '/attendance' },
    { icon: 'svg-reward', labelKey: 'LAYOUT.NAV.RECOGNITIONS', route: '/recognitions' },
    { icon: 'svg-notifications', labelKey: 'LAYOUT.NAV.NOTIFICATIONS', route: '/news' },
    {
      icon: 'svg-scheduling',
      labelKey: 'LAYOUT.NAV.SCHEDULING',
      route: '/scheduling',
      hasArrow: true,
      children: [
        { labelKey: 'LAYOUT.NAV.SHIFT_MANAGEMENT', route: '/scheduling/shift-management', disabled: true },
        { labelKey: 'LAYOUT.NAV.EMPLOYEE_ASSIGNMENT', route: '/scheduling/employee-assignment', disabled: true },
        { labelKey: 'LAYOUT.NAV.SHIFT_MARKET', route: '/scheduling/shift-market', disabled: true },
        { labelKey: 'LAYOUT.NAV.SHIFT_TEMPLATES', route: '/scheduling/shift-templates' },
      ],
    },
  ];

  // ===== EXPANDABLE GROUPS =====
  expandedGroups = signal<Record<string, boolean>>({});

  // ===== LIFECYCLE =====
  ngOnInit() {
    this.selectedLanguage = this.languageService.currentLanguage();
    this.checkScreenSize();
    this.autoExpandForRoute(this.router.url);
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((event) => this.autoExpandForRoute((event as NavigationEnd).urlAfterRedirects));
  }

  // ===== SCREEN SIZE HANDLER =====
  @HostListener('window:resize')
  checkScreenSize() {
    const isMobile = window.innerWidth <= 1024;
    this.isMobileDevice.set(isMobile);

    if (isMobile) {
      this.isSidebarOpen.set(false);
    } else {
      this.isSidebarOpen.set(true);
    }
  }

  // ===== METHODS =====
  toggleSidebar() {
    this.isSidebarOpen.update(prev => !prev);
  }

  toggleGroup(route: string) {
    this.expandedGroups.update(groups => ({ ...groups, [route]: !groups[route] }));
  }

  isGroupExpanded(route: string): boolean {
    return this.expandedGroups()[route] === true;
  }

  isChildActive(route: string): boolean {
    const url = stripQueryFragment(this.router.url);
    return url === route || url.startsWith(`${route}/`);
  }

  trackChild(_index: number, child: NavChild): string {
    return child.route;
  }

  private autoExpandForRoute(url: string) {
    const path = stripQueryFragment(url);
    for (const item of this.menuItems) {
      if (item.children && (path === item.route || path.startsWith(`${item.route}/`))) {
        this.expandedGroups.update(groups => ({ ...groups, [item.route]: true }));
      }
    }
  }

  closeSidebarOnMobile() {
    if (this.isMobileDevice()) {
      this.isSidebarOpen.set(false);
    }
  }

  isMobile(): boolean {
    return this.isMobileDevice();
  }

  logout() {
    this.router.navigate(['/auth/login']);
  }

  changeLanguage(language: AppLanguage): void {
    this.selectedLanguage = language;
    this.languageService.changeLanguage(language).subscribe();
  }


}
