import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-button',
  standalone: true,
  templateUrl: './button.component.html',
  styleUrl: './button.component.css',
})
export class ButtonComponent {
  variant = input<'primary' | 'primary-light' | 'secondary' | 'danger' | 'ghost'>('primary');
  size = input<'sm' | 'md' | 'lg'>('md');
  shape = input<'default' | 'full' | 'circle'>('default');
  disabled = input<boolean>(false);
  loading = input<boolean>(false);
  type = input<'button' | 'submit' | 'reset'>('button');

  clicked = output<void>();

  buttonClasses(): string {
    const base =
      'inline-flex items-center justify-center gap-2 font-medium transition-colors focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed';

    const variants: Record<string, string> = {
      primary: 'bg-primary-600 text-white hover:bg-primary-700 focus:ring-primary-500',
      'primary-light':
        'bg-primary-overlay text-primary-600 hover:bg-primary-100 focus:ring-primary-400',
      secondary:
        'bg-secondary-100 text-secondary-800 hover:bg-secondary-200 focus:ring-secondary-400',
      danger: 'bg-error-700 text-white hover:bg-error-800 focus:ring-error-500',
      ghost: 'bg-transparent text-primary-700 hover:bg-primary-50 focus:ring-primary-400',
    };

    const sizes: Record<string, string> = {
      sm: 'px-3 py-1.5 text-sm',
      md: 'px-4 py-2 text-base',
      lg: 'px-6 py-3 text-lg',
    };

    const shapes: Record<string, string> = {
      default: 'rounded-lg',
      full: 'rounded-lg w-full',
      circle: 'rounded-full',
    };

    return `${base} ${variants[this.variant()]} ${sizes[this.size()]} ${shapes[this.shape()]}`;
  }
}
