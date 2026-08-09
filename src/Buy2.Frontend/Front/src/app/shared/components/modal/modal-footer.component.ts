import { Component } from '@angular/core';

@Component({
  selector: 'app-modal-footer',
  standalone: true,
  template: `
    <footer class="modal__footer">
      <ng-content />
    </footer>
  `,
  styles: `
    .modal__footer {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 12px;
      padding: 16px 24px;
    }
  `,
})
export class ModalFooterComponent {}
