import {
  Component,
  input,
  output,
  type ElementRef,
  viewChild,
  type AfterViewInit,
  type OnInit,
  type OnDestroy,
} from '@angular/core';
import { ButtonComponent } from '../button/button.component';
import { ModalHeaderComponent } from './modal-header.component';
import { ModalBodyComponent } from './modal-body.component';
import { ModalFooterComponent } from './modal-footer.component';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [ButtonComponent],
  templateUrl: './modal.component.html',
  styleUrl: './modal.component.css',
})
export class ModalComponent implements OnInit, AfterViewInit, OnDestroy {
  modalEl = viewChild<ElementRef>('modalEl');
  private escapeHandler = (e: KeyboardEvent) => this.onEscape(e);

  size = input<'small' | 'medium' | 'large'>('medium');
  showHeader = input(true);
  showActions = input(true);
  showCloseButton = input(true);

  closeOnBackdrop = input(true);
  closeOnEscape = input(true);
  closed = output<void>();

  close(): void {
    this.closed.emit();
  }

  ngOnInit(): void {
    document.addEventListener('keydown', this.escapeHandler);
  }

  ngAfterViewInit(): void {
    this.modalEl()?.nativeElement.focus();
  }

  ngOnDestroy(): void {
    document.removeEventListener('keydown', this.escapeHandler);
  }

  onBackdropClick(): void {
    if (this.closeOnBackdrop()) {
      this.close();
    }
  }

  private onEscape(e: KeyboardEvent): void {
    if (e.key === 'Escape' && this.closeOnEscape()) {
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
