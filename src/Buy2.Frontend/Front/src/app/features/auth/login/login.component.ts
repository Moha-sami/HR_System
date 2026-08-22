import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, type NgForm } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe, RouterLink],
  templateUrl: './login.component.html',
})
export class LoginComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly showPassword = signal(false);
  readonly remember = signal(false);

  email = '';
  password = '';

  async onSubmit(form: NgForm): Promise<void> {
    if (form.invalid) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    try {
      await firstValueFrom(this.authService.login({ email: this.email, password: this.password }));
      this.router.navigate(['/']);
    } catch (err: any) {
      const message = err?.error?.message || err?.error || err?.message || 'Login failed. Please try again.';
      this.error.set(typeof message === 'string' ? message : 'Login failed. Please try again.');
    } finally {
      this.loading.set(false);
    }
  }
}
