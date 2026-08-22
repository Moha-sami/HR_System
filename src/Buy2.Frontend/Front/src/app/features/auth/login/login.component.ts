import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, type NgForm } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './login.component.html',
})
export class LoginComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly showPassword = signal(false);
  readonly remember = signal(false);

  email = signal('');
  password = signal('');

  async onSubmit(form: NgForm): Promise<void> {
    if (form.invalid) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    try {
      await this.authService.login({ email: this.email(), password: this.password() }).toPromise();
      this.router.navigate(['/']);
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Login failed. Please try again.';
      this.error.set(message);
    } finally {
      this.loading.set(false);
    }
  }
}
