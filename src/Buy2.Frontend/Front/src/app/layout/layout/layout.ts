import { Component, signal, inject, HostListener, type OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { type AppLanguage, LanguageService } from '../../core/services/language.service';
import { BreadcrumbComponent } from '../breadcrumb/breadcrumb';

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

  // ===== STATE =====
  isSidebarOpen = signal(true);
  selectedLanguage = 'en';
  isMobileDevice = signal(false);

  // ===== LOGO PATH =====
  logoPath = '/buy2logo.png';


  // ===== MENU ITEMS =====
  menuItems = [
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
    { icon: 'svg-scheduling', labelKey: 'LAYOUT.NAV.SCHEDULING', route: '/scheduling', hasArrow: true },
  ];

  // ===== LIFECYCLE =====
  ngOnInit() {
    this.selectedLanguage = this.languageService.currentLanguage();
    this.checkScreenSize();
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
