import { Component, signal, type WritableSignal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ModalComponent } from '../../../shared/components/modal/modal.component';
import { ModalHeaderComponent } from '../../../shared/components/modal/modal-header.component';
import { ModalBodyComponent } from '../../../shared/components/modal/modal-body.component';
import { ModalFooterComponent } from '../../../shared/components/modal/modal-footer.component';
import { ButtonComponent } from '../../../shared/components/button/button.component';

@Component({
  selector: 'app-modal-docs',
  standalone: true,
  imports: [
    RouterLink,
    ModalComponent,
    ModalHeaderComponent,
    ModalBodyComponent,
    ModalFooterComponent,
    ButtonComponent,
  ],
  templateUrl: './modal-docs.component.html',
  styleUrl: './modal-docs.component.css',
})
export class ModalDocsComponent {
  // ──────────────────────────────────────────────────────
  // Modal states
  // ──────────────────────────────────────────────────────
  basicModal = signal(false);
  smallModal = signal(false);
  largeModal = signal(false);
  noHeaderModal = signal(false);
  noActionsModal = signal(false);
  noBackdropModal = signal(false);
  formModal = signal(false);
  confirmModal = signal(false);
  lastAction = signal('');

  // ──────────────────────────────────────────────────────
  // Open / Close helpers
  // ──────────────────────────────────────────────────────
  openModal(modal: WritableSignal<boolean>): void {
    modal.set(true);
  }

  closeModal(modal: WritableSignal<boolean>): void {
    modal.set(false);
  }

  onConfirm(): void {
    this.lastAction.set('Confirmed!');
    this.closeModal(this.confirmModal);
  }

  onCancel(): void {
    this.lastAction.set('Cancelled');
    this.closeModal(this.confirmModal);
  }

  onSubmit(): void {
    this.lastAction.set('Form submitted!');
    this.closeModal(this.formModal);
  }
}
