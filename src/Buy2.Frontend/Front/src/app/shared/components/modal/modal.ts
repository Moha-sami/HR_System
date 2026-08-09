import { Component, input, output} from '@angular/core';

@Component({
  selector: 'app-modal',
  imports: [],
  templateUrl: './modal.html',
  styleUrl: './modal.css',
})
export class Modal {
  constructor(){}

  size = input<'small' | 'medium' | 'large'>('medium');
  showHeader = input(true);
  showActions = input(true);

  closeOnBackdrop = input(true);
  closeOnEscape = input(true);
  closed = output<void>();

  close(): void {
    this.closed.emit();
  }

  onBackdropClick(): void {
    if (this.closeOnBackdrop()) {
      this.close();
    }
  }

  onEscape(): void {
    if (this.closeOnEscape()) {
      this.close();
    }
  }
}


// to use it i have to add the following function in the .ts file of parent component:

// import the modal componant first then:

// showModal = signal(false);
// openModal(): void {
//   this.showModal.set(true);
// }
// closeModal(): void {
//   this.showModal.set(false);
// }

// and use this in the .html file of parent component:

// @if (showModal()) {

//   <app-modal
//     size="small"
//     (closed)="closeModal()"
//     [closeOnBackdrop]="false/true" depends on the need
//     [showHeader]="false/true" depends on the need
//     [showActions]="false/true" depends on the need
//   >

//     <div modal-header>
//       content of the header if needed
//     </div>


//     <div modal-body>
//       content of the body required
//     </div>


//     <div modal-actions>
//       content of the actions if needed
//     </div>

//   </app-modal> }