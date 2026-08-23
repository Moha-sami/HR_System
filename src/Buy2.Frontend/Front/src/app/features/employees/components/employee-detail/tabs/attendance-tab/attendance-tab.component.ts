import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { EmployeeService } from '../../../../services/employee.service';

@Component({
  selector: 'app-attendance-tab',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './attendance-tab.component.html',
})
export class AttendanceTabComponent {
  private readonly employeeService = inject(EmployeeService);

  readonly employee = this.employeeService.detailEmployeeSignal;
  readonly comingSoon = true;
}
