import { Component } from '@angular/core';

@Component({
  selector: 'app-modal-body',
  standalone: true,
  template: `
    <main class="modal__body">
      <ng-content />
    </main>
  `,
  styles: `
    .modal__body {
      padding: 24px;
      overflow-y: auto;
    }
  `,
})
export class ModalBodyComponent {}
