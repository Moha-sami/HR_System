import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { EmployeeDetailService } from '../../../../services/employee-detail.service';

@Component({
  selector: 'app-documents-tab',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './documents-tab.component.html',
})
export class DocumentsTabComponent {
  private readonly employeeDetailService = inject(EmployeeDetailService);

  readonly employee = this.employeeDetailService.detailEmployee;
  readonly comingSoon = true;
}
