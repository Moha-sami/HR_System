import { Component, signal, inject } from '@angular/core';
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
export class Layout {
  private router = inject(Router);

  // ===== STATE =====
  isSidebarOpen = signal(true);
  selectedLanguage = 'en';

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

  get currentPageTitle(): string {
    const currentUrl = this.router.url;
    const item = this.menuItems.find(item => currentUrl.includes(item.route));
    return item ? item.label : 'Dashboard';
  }

  // ===== METHODS =====
  toggleSidebar() {
    this.isSidebarOpen.update(prev => !prev);
  }

  logout() {
    this.router.navigate(['/auth/login']);
  }
}
