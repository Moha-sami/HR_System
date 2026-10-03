import { Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ShiftManagementService } from '../../data-access/services/shift-management.service';
import type { ShiftCandidatePreview } from '../../data-access/models/shift-management.models';
import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { ModalBodyComponent } from '@app/shared/components/modal/modal-body.component';

/**
 * Ticket A: shared candidate preview modal. Opens whenever employeeId is
 * non-null and loads GET /shifts/candidates/{id}/preview. The board (ticket B)
 * reuses it for strip-card clicks.
 */
@Component({
  selector: 'app-candidate-preview',
  standalone: true,
  imports: [CommonModule, TranslatePipe, ModalComponent, ModalBodyComponent],
  templateUrl: './candidate-preview.component.html',
})
export class CandidatePreviewComponent {
  private readonly managementService = inject(ShiftManagementService);
  private readonly destroyRef = inject(DestroyRef);

  readonly employeeId = input<number | null>(null);
  readonly closed = output<void>();

  readonly preview = signal<ShiftCandidatePreview | null>(null);
  readonly loading = signal(false);
  readonly loadError = signal(false);

  constructor() {
    effect(() => {
      const id = this.employeeId();
      if (id === null) {
        this.preview.set(null);
        this.loadError.set(false);
        return;
      }
      this.loading.set(true);
      this.loadError.set(false);
      this.managementService
        .getCandidatePreview(id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (res) => {
            this.preview.set(res);
            this.loading.set(false);
          },
          error: () => {
            this.loading.set(false);
            this.loadError.set(true);
          },
        });
    });
  }

  close(): void {
    this.closed.emit();
  }
}
