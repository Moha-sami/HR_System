import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

export type DropdownMode = 'single' | 'multiple';

export interface DropdownOption {
  id: number | string;
  name: string;
  disabled?: boolean;
}

@Component({
  selector: 'app-dropdown',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './dropdown.component.html',
  styleUrl: './dropdown.component.css',
})
export class DropdownComponent {
  @Input({ required: true }) options: DropdownOption[] = [];
  @Input({ required: true }) mode: DropdownMode = 'single';
  @Input() placeholder = '';
  @Input() disabled = false;
  @Input() selectedValue: number | string | readonly (number | string)[] = '';
  @Input() label = '';
  @Input() error = '';
  @Input() id = '';

  @Output() selectionChange = new EventEmitter<number | string | readonly (number | string)[]>();

  isOpen = signal(false);

  get selectedOptions(): DropdownOption[] {
    const values = Array.isArray(this.selectedValue) ? this.selectedValue : [this.selectedValue];
    return this.options.filter((opt) => values.includes(opt.id));
  }

  get displayText(): string {
    if (this.mode === 'single') {
      const opt = this.options.find((o) => o.id === this.selectedValue);
      return opt?.name ?? this.placeholder;
    }
    if (this.selectedOptions.length === 0) {
      return this.placeholder;
    }
    if (this.selectedOptions.length <= 2) {
      return this.selectedOptions.map((o) => o.name).join(', ');
    }
    return `${this.selectedOptions.length} ${this.placeholder}`;
  }

  toggleOpen(): void {
    if (!this.disabled) {
      this.isOpen.update((v) => !v);
    }
  }

  selectOption(option: DropdownOption): void {
    if (option.disabled) return;

    if (this.mode === 'single') {
      this.selectionChange.emit(option.id);
      this.isOpen.set(false);
    } else {
      const current = Array.isArray(this.selectedValue) ? [...this.selectedValue] : [];
      const index = current.findIndex((v) => v === option.id);
      const updated = index >= 0 ? current.filter((v) => v !== option.id) : [...current, option.id];
      this.selectionChange.emit(updated);
    }
  }

  isSelected(option: DropdownOption): boolean {
    if (this.mode === 'single') {
      return this.selectedValue === option.id;
    }
    return Array.isArray(this.selectedValue) && this.selectedValue.includes(option.id);
  }

  onBlur(): void {
    // Small delay to allow click events to fire
    setTimeout(() => this.isOpen.set(false), 150);
  }
}
