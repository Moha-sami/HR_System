import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { EmployeeDetailService } from '../../../../services/employee-detail.service';

@Component({
  selector: 'app-attendance-tab',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './attendance-tab.component.html',
})
export class AttendanceTabComponent {
  private readonly employeeDetailService = inject(EmployeeDetailService);

  readonly employee = this.employeeDetailService.detailEmployee;
  readonly comingSoon = true;
}
