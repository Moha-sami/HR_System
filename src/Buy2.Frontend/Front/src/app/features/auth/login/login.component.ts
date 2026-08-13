// Placeholder login component. Wired through AuthLayout; full form render pending.

import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-login',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<p>Login (placeholder)</p>`,
})
export class Login {}
