import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { EmployeeDetailService } from '../../../../services/employee-detail.service';

@Component({
  selector: 'app-payroll-tab',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './payroll-tab.component.html',
})
export class PayrollTabComponent {
  private readonly employeeDetailService = inject(EmployeeDetailService);

  readonly employee = this.employeeDetailService.detailEmployee;
  readonly comingSoon = true;
}
