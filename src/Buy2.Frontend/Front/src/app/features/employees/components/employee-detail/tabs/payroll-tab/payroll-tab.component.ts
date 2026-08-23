import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { EmployeeService } from '../../../../services/employee.service';

@Component({
  selector: 'app-payroll-tab',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './payroll-tab.component.html',
})
export class PayrollTabComponent {
  private readonly employeeService = inject(EmployeeService);

  readonly employee = this.employeeService.detailEmployee;
  readonly comingSoon = true;
}
