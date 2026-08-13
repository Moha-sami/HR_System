// Placeholder forgot-password component. Wired through AuthLayout; full form render pending.

import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<p>Forgot password (placeholder)</p>`,
})
export class ForgotPassword {}
