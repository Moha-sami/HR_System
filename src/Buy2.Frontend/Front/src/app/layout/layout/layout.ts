import { Component, signal, inject, HostListener, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, FormsModule],
  templateUrl: './layout.html',
  styleUrls: ['./layout.css']
})
export class Layout implements OnInit {
  private router = inject(Router);

  // ===== STATE =====
  isSidebarOpen = signal(true);
  selectedLanguage = 'en';
  isMobileDevice = signal(false);

  // ===== LOGO PATH =====
  logoPath = '/assets/buy2logo.png';

  // ===== MENU ITEMS =====
  menuItems = [
    { icon: 'svg-dashboard', label: 'Dashboard', route: '/dashboard' },
    { icon: 'svg-employee', label: 'Employee Management', route: '/employees' },
    { icon: 'svg-job', label: 'Job Management', route: '/jobs' },
    { icon: 'svg-user', label: 'User Management', route: '/users/add' },
    { icon: 'svg-reward', label: 'Reward Management', route: '/rewards' },
    { icon: 'svg-points', label: 'Points Management', route: '/points' },
    { icon: 'svg-site', label: 'Site Management', route: '/sites' },
    { icon: 'svg-request', label: 'Request Management', route: '/requests', hasArrow: true },
    { icon: 'svg-time', label: 'Time & Attendance', route: '/attendance' },
    { icon: 'svg-notifications', label: 'Notifications', route: '/notifications' },
    { icon: 'svg-scheduling', label: 'Scheduling', route: '/scheduling', hasArrow: true },
  ];

  // ===== LIFECYCLE =====
  ngOnInit() {
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


}
