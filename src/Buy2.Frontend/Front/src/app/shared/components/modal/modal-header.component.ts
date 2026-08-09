import { Component } from '@angular/core';

@Component({
  selector: 'app-modal-header',
  standalone: true,
  template: `
    <header class="modal__header">
      <ng-content />
    </header>
  `,
  styles: `
    .modal__header {
      position: relative;
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 60px;
      padding: 16px 60px 16px 24px;
    }
  `,
})
export class ModalHeaderComponent {}
