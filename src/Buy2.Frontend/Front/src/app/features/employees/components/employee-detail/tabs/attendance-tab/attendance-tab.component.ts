import { Component, inject, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { EmployeeDetailService } from '../../../../services/employee-detail.service';
import {
  AttendanceDayStatus,
  type AttendanceDayDto,
} from '../../../../models/view-employee/employee-attendance';

@Component({
  selector: 'app-attendance-tab',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './attendance-tab.component.html',
})
export class AttendanceTabComponent {
  private readonly employeeDetailService = inject(EmployeeDetailService);

  // Expose enum to template
  readonly AttendanceDayStatus = AttendanceDayStatus;

  readonly employee = this.employeeDetailService.detailEmployee;
  readonly attendanceCalendar = this.employeeDetailService.attendanceCalendar;
  readonly attendanceLoading = this.employeeDetailService.attendanceLoading;
  readonly attendanceError = this.employeeDetailService.attendanceError;

  // Current month/year state
  readonly currentMonth = signal<number>(new Date().getMonth() + 1); // 1-12
  readonly currentYear = signal<number>(new Date().getFullYear());

  // Computed calendar days for the grid
  readonly calendarDays = computed(() => {
    const calendar = this.attendanceCalendar();
    if (!calendar) return [];
    return calendar.days;
  });

  // Get first day of month for calendar offset
  readonly firstDayOffset = computed(() => {
    const date = new Date(this.currentYear(), this.currentMonth() - 1, 1);
    return date.getDay(); // 0 = Sunday, 6 = Saturday
  });

  // Get number of days in month
  readonly daysInMonth = computed(() => {
    return new Date(this.currentYear(), this.currentMonth(), 0).getDate();
  });

  // Generate calendar grid (including empty cells for offset)
  readonly calendarGrid = computed(() => {
    const offset = this.firstDayOffset();
    const daysInMonth = this.daysInMonth();
    const calendarDays = this.calendarDays();
    const grid: (AttendanceDayDto | null)[] = [];

    // Empty cells for days before month starts
    for (let i = 0; i < offset; i++) {
      grid.push(null);
    }

    // Actual days of the month
    for (let day = 1; day <= daysInMonth; day++) {
      const dateStr = `${this.currentYear()}-${String(this.currentMonth()).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
      const dayData = calendarDays.find((d) => d.date.startsWith(dateStr));
      grid.push(dayData || null);
    }

    return grid;
  });

  // Month name for display
  readonly monthName = computed(() => {
    const date = new Date(this.currentYear(), this.currentMonth() - 1);
    return date.toLocaleString('default', { month: 'long' });
  });

  // Day headers for calendar (starting from Sunday)
  readonly dayHeaders = ['SUN', 'MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT'];

  // Month options for dropdown
  readonly months = [
    { value: 1, label: 'January' },
    { value: 2, label: 'February' },
    { value: 3, label: 'March' },
    { value: 4, label: 'April' },
    { value: 5, label: 'May' },
    { value: 6, label: 'June' },
    { value: 7, label: 'July' },
    { value: 8, label: 'August' },
    { value: 9, label: 'September' },
    { value: 10, label: 'October' },
    { value: 11, label: 'November' },
    { value: 12, label: 'December' },
  ];

  // Year options for dropdown (current year ± 2 years)
  readonly years = computed(() => {
    const current = new Date().getFullYear();
    const start = current - 2;
    const end = current + 2;
    const arr: number[] = [];
    for (let y = start; y <= end; y++) {
      arr.push(y);
    }
    return arr;
  });

  // Get day number for calendar grid index
  getDayNumber(index: number): number {
    const offset = this.firstDayOffset();
    const dayNum = index - offset + 1;
    return dayNum > 0 && dayNum <= this.daysInMonth() ? dayNum : 0;
  }

  // Handle month change from dropdown
  onMonthChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.currentMonth.set(Number(select.value));
    this.loadAttendanceData();
  }

  // Handle year change from dropdown
  onYearChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.currentYear.set(Number(select.value));
    this.loadAttendanceData();
  }

  // Navigate to previous month
  previousMonth(): void {
    if (this.currentMonth() === 1) {
      this.currentMonth.set(12);
      this.currentYear.update((y) => y - 1);
    } else {
      this.currentMonth.update((m) => m - 1);
    }
    this.loadAttendanceData();
  }

  // Navigate to next month
  nextMonth(): void {
    if (this.currentMonth() === 12) {
      this.currentMonth.set(1);
      this.currentYear.update((y) => y + 1);
    } else {
      this.currentMonth.update((m) => m + 1);
    }
    this.loadAttendanceData();
  }

  // Load attendance data when month/year changes
  loadAttendanceData(): void {
    const emp = this.employee();
    if (emp) {
      this.employeeDetailService.loadAttendanceCalendar(
        emp.id,
        this.currentMonth(),
        this.currentYear(),
      );
    }
  }

  // Effect to load data when employee changes
  constructor() {
    effect(() => {
      const emp = this.employee();
      if (emp) {
        this.loadAttendanceData();
      }
    });
  }

  // Status color mapping based on Figma design
  getStatusColor(status: AttendanceDayStatus): string {
    switch (status) {
      case AttendanceDayStatus.OnTime:
        return '#E5F8EB'; // Green background
      case AttendanceDayStatus.Late:
        return '#FFEBEB'; // Red background
      case AttendanceDayStatus.ApprovedLeave:
        return '#E5F3FF'; // Blue background
      case AttendanceDayStatus.UnapprovedLeave:
        return '#FFEBEB'; // Red background
      case AttendanceDayStatus.PartialLeave:
        return '#FFF5E8'; // Orange background
      case AttendanceDayStatus.NoAttendance:
        return '#FFEBEB'; // Red background
      case AttendanceDayStatus.PublicHoliday:
        return '#EBEDF3'; // Grey background
      case AttendanceDayStatus.AttendanceNotRequired:
        return '#EBEDF3'; // Grey background
      default:
        return '#FFFFFF';
    }
  }

  getStatusTextColor(status: AttendanceDayStatus): string {
    switch (status) {
      case AttendanceDayStatus.OnTime:
        return '#00BA34'; // Green text
      case AttendanceDayStatus.Late:
        return '#E92C2C'; // Red text
      case AttendanceDayStatus.ApprovedLeave:
        return '#2D2C79'; // Blue text
      case AttendanceDayStatus.UnapprovedLeave:
        return '#E92C2C'; // Red text
      case AttendanceDayStatus.PartialLeave:
        return '#6E6E6E'; // Grey text
      case AttendanceDayStatus.NoAttendance:
        return '#E92C2C'; // Red text
      case AttendanceDayStatus.PublicHoliday:
        return '#969696'; // Grey text
      case AttendanceDayStatus.AttendanceNotRequired:
        return '#6E6E6E'; // Grey text
      default:
        return '#585757';
    }
  }

  getStatusIcon(status: AttendanceDayStatus): string {
    switch (status) {
      case AttendanceDayStatus.OnTime:
        return 'tabler-icon-checks';
      case AttendanceDayStatus.Late:
        return 'tabler-icon-circle-dashed-x';
      case AttendanceDayStatus.ApprovedLeave:
        return 'tabler-icon-checks';
      case AttendanceDayStatus.UnapprovedLeave:
        return 'tabler-icon-circle-dashed-x';
      case AttendanceDayStatus.PartialLeave:
        return '';
      case AttendanceDayStatus.NoAttendance:
        return 'tabler-icon-circle-dashed-x';
      case AttendanceDayStatus.PublicHoliday:
        return 'tabler-icon-beach';
      case AttendanceDayStatus.AttendanceNotRequired:
        return '';
      default:
        return '';
    }
  }

  // Format minutes to "Xh Ym" format
  formatMinutes(minutes: number): string {
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    if (hours > 0 && mins > 0) {
      return `${hours}h ${mins}m`;
    } else if (hours > 0) {
      return `${hours}h`;
    } else {
      return `${mins}m`;
    }
  }

  // Format decimal hours to "Xh Ym"
  formatHours(hours: number): string {
    const totalMinutes = Math.round(hours * 60);
    return this.formatMinutes(totalMinutes);
  }

  // Get day entries for a specific day (up to 5 time entries)
  getDayEntries(
    dayData: AttendanceDayDto | null,
  ): { label: string; value: string; color: string }[] {
    if (!dayData) return [];

    const entries: { label: string; value: string; color: string }[] = [];

    // Hours worked
    if (dayData.hoursWorked > 0) {
      entries.push({
        label: 'Hours worked',
        value: this.formatHours(dayData.hoursWorked),
        color: '#585757', // Dark grey
      });
    }

    // Hours left
    if (dayData.hoursLeft > 0) {
      entries.push({
        label: 'Hours Left',
        value: this.formatHours(dayData.hoursLeft),
        color: '#585757',
      });
    }

    // OT Hours
    if (dayData.otHours > 0) {
      entries.push({
        label: 'OT Hours',
        value: this.formatHours(dayData.otHours),
        color: '#FF9F2D', // Orange
      });
    }

    // Break time
    if (dayData.breakTime > 0) {
      entries.push({
        label: 'Break time',
        value: `${dayData.breakTime}m`,
        color: '#585757',
      });
    }

    // Lateness
    if (dayData.latenessMinutes && dayData.latenessMinutes > 0) {
      entries.push({
        label: 'Lateness',
        value: `${dayData.latenessMinutes}m`,
        color: '#E92C2C', // Red
      });
    }

    // If no entries but has status, show status text
    if (entries.length === 0 && dayData.status !== AttendanceDayStatus.OnTime) {
      entries.push({
        label: '',
        value: this.getStatusLabel(dayData.status, dayData.leaveType),
        color: this.getStatusTextColor(dayData.status),
      });
    }

    return entries.slice(0, 5); // Max 5 entries per day
  }

  getStatusLabel(status: AttendanceDayStatus, leaveType?: string | null): string {
    switch (status) {
      case AttendanceDayStatus.OnTime:
        return '';
      case AttendanceDayStatus.Late:
        return 'Late';
      case AttendanceDayStatus.ApprovedLeave:
        return leaveType || 'Approved Leave';
      case AttendanceDayStatus.UnapprovedLeave:
        return leaveType || 'Unapproved Leave';
      case AttendanceDayStatus.PartialLeave:
        return leaveType || 'Partial Leave';
      case AttendanceDayStatus.NoAttendance:
        return 'No Attendance';
      case AttendanceDayStatus.PublicHoliday:
        return 'Public Holiday';
      case AttendanceDayStatus.AttendanceNotRequired:
        return 'Attendance not required';
      default:
        return '';
    }
  }

  // Get day cell background color
  getDayCellBgColor(dayData: AttendanceDayDto | null): string {
    if (!dayData) return '#FFFFFF';
    if (
      dayData.status === AttendanceDayStatus.PublicHoliday ||
      dayData.status === AttendanceDayStatus.AttendanceNotRequired
    ) {
      return '#EBEDF3';
    }
    if (dayData.status === AttendanceDayStatus.OnTime) {
      return '#E5F8EB';
    }
    return '#FFFFFF';
  }

  getDayCellBorderColor(dayData: AttendanceDayDto | null): string {
    if (!dayData) return '#E8E8E8';
    if (
      dayData.status === AttendanceDayStatus.PublicHoliday ||
      dayData.status === AttendanceDayStatus.AttendanceNotRequired
    ) {
      return '#E8E8E8';
    }
    return '#E8E8E8';
  }
}
