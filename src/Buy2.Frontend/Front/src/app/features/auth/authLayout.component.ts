// Two-column auth shell: form on the left with brand at top, hero image on right.
// Child routes (login, forgot-password, etc.) render inside the left panel
// via <router-outlet>.

import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-auth-layout',
  standalone: true,
  imports: [RouterOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './authLayout.component.html',
})
export class AuthLayout {}
