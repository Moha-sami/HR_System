import { Component, type OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { RoleService } from '../services/role.service';
import { RoleCardComponent } from './role-card/role-card.component';

@Component({
  selector: 'app-role-list',
  standalone: true,
  imports: [RouterLink, RoleCardComponent, TranslatePipe],
  templateUrl: './role-list.component.html',
})
export class RoleListComponent implements OnInit {
  private readonly roleService = inject(RoleService);

  readonly roles = this.roleService.roles;
  readonly loading = this.roleService.loading;
  readonly error = this.roleService.error;

  ngOnInit(): void {
    this.roleService.loadAll();
  }
}
