import { Component, inject, input, output, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { ModalBodyComponent } from '../../../../shared/components/modal/modal-body.component';
import { ModalComponent } from '../../../../shared/components/modal/modal.component';
import { RecognitionService } from '../../services/recognition.service';

@Component({
  selector: 'app-recognition-comments-modal',
  standalone: true,
  imports: [ReactiveFormsModule, TranslatePipe, ModalComponent, ModalBodyComponent],
  templateUrl: './recognition-comments-modal.component.html',
  styleUrl: './recognition-comments-modal.component.css',
})
export class RecognitionCommentsModalComponent {
  private readonly service = inject(RecognitionService);

  readonly recognitionId = input.required<string>();
  readonly closed = output<void>();
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly loadError = signal(false);
  readonly submitError = signal(false);
  readonly comments = signal<never[]>([]);
  readonly comment = new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(1000)] });

  ngOnInit(): void {
    this.loadComments();
  }

  loadComments(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.service.getComments(this.recognitionId()).subscribe({
      next: (comments) => {
        this.comments.set(comments);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.loadError.set(true);
      },
    });
  }

  submit(): void {
    const content = this.comment.value.trim();
    if (!content || this.comment.invalid || this.submitting()) {
      this.comment.markAsTouched();
      return;
    }
    this.submitting.set(true);
    this.submitError.set(false);
    this.service.addComment(this.recognitionId(), content).subscribe({
      next: () => {
        this.comment.reset();
        this.submitting.set(false);
        this.loadComments();
      },
      error: () => {
        this.submitting.set(false);
        this.submitError.set(true);
      },
    });
  }
}
