import {
  Component,
  inject,
  input,
  output,
  signal,
  type OnDestroy,
  type OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { Subject, debounceTime, takeUntil } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ShiftManagementService } from '../../data-access/services/shift-management.service';
import { ShiftTemplateService } from '../../data-access/services/shift-template.service';
import type { ShiftTemplateListItem } from '../../data-access/models/shift-template.models';
import type {
  ApplyTemplateWarning,
  TemplateKeepMode,
} from '../../data-access/models/shift-management.models';
import { ButtonComponent } from '@app/shared/components/button/button.component';
import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { ModalBodyComponent } from '@app/shared/components/modal/modal-body.component';

/**
 * Ticket C: template picker modal for the board. Lists shift templates,
 * applies the chosen one to the board's day with a Keep/Replace choice
 * (default Keep existing). Backend conflicts render inline; the board
 * behind stays untouched until a successful apply.
 */
@Component({
  selector: 'app-template-picker',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslatePipe,
    ButtonComponent,
    ModalComponent,
    ModalBodyComponent,
  ],
  templateUrl: './template-picker.component.html',
})
export class TemplatePickerComponent implements OnInit, OnDestroy {
  private readonly managementService = inject(ShiftManagementService);
  private readonly templateService = inject(ShiftTemplateService);
  private readonly destroy$ = new Subject<void>();

  readonly siteId = input.required<number>();
  readonly date = input.required<string>();

  readonly applied = output<void>();
  readonly closed = output<void>();

  readonly templates = signal<ShiftTemplateListItem[]>([]);
  readonly loading = signal(false);
  readonly loadError = signal(false);
  readonly selectedTemplateId = signal<number | null>(null);
  readonly keepMode = signal<TemplateKeepMode>('existing');
  readonly searchTerm = signal('');
  readonly applyError = signal<string | null>(null);
  readonly applying = signal(false);
  readonly resultWarnings = signal<ApplyTemplateWarning[] | null>(null);

  private readonly search$ = new Subject<string>();

  constructor() {
    this.search$
      .pipe(debounceTime(300), takeUntilDestroyed())
      .subscribe((term) => {
        this.searchTerm.set(term);
        this.loadTemplates();
      });
  }

  ngOnInit(): void {
    this.loadTemplates();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSearchInput(value: string): void {
    this.search$.next(value);
  }

  selectTemplate(id: number): void {
    this.selectedTemplateId.set(id);
    this.applyError.set(null);
  }

  setKeepMode(mode: TemplateKeepMode): void {
    this.keepMode.set(mode);
  }

  apply(): void {
    const templateId = this.selectedTemplateId();
    if (templateId === null) {
      this.applyError.set('SHIFT_MANAGEMENT.PICKER.TEMPLATE_REQUIRED');
      return;
    }
    this.applyError.set(null);
    this.applying.set(true);
    this.managementService
      .applyTemplate(this.siteId(), this.date(), { templateId, keep: this.keepMode() })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.applying.set(false);
          if (res.warnings.length > 0) {
            this.resultWarnings.set(res.warnings);
            return;
          }
          this.applied.emit();
        },
        error: (err: { error?: { message?: string } }) => {
          this.applying.set(false);
          this.applyError.set(err?.error?.message ?? 'SHIFT_MANAGEMENT.PICKER.APPLY_ERROR');
        },
      });
  }

  doneAfterWarnings(): void {
    this.applied.emit();
  }

  private loadTemplates(): void {
    this.loading.set(true);
    this.loadError.set(false);
    const searchTerm = this.searchTerm().trim();
    this.templateService
      .getTemplates({ pageNumber: 1, pageSize: 100, ...(searchTerm ? { searchTerm } : {}) })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.templates.set(res.items);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.loadError.set(true);
        },
      });
  }
}
