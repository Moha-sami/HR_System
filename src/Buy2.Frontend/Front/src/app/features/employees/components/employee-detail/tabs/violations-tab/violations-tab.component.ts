import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { EmployeeService } from '../../../../services/employee.service';

@Component({
  selector: 'app-violations-tab',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './violations-tab.component.html',
})
export class ViolationsTabComponent {
  private readonly employeeService = inject(EmployeeService);

  readonly employee = this.employeeService.detailEmployeeSignal;
  readonly comingSoon = true;
}
