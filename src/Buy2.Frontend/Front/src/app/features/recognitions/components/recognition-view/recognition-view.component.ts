import { Component, inject, input, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { ButtonComponent } from '../../../../shared/components/button/button.component';
import { ModalComponent } from '../../../../shared/components/modal/modal.component';
import { ModalBodyComponent } from '../../../../shared/components/modal/modal-body.component';
import { RecognitionContentComponent } from '../recognition-content/recognition-content.component';
import { RecognitionService } from '../../services/recognition.service';
import type { Recognition } from '../../models/recognition.models';

@Component({ selector: 'app-recognition-view', standalone: true,
  imports: [TranslatePipe, ButtonComponent, ModalComponent, ModalBodyComponent, RecognitionContentComponent],
  templateUrl: './recognition-view.component.html', styleUrl: '../../recognitions.css' })
export class RecognitionViewComponent implements OnInit {
  readonly id = input.required<string>();
  private readonly service = inject(RecognitionService);
  private readonly router = inject(Router);
  readonly item = signal<Recognition | null>(null);
  readonly name = signal('');
  readonly loading = signal(false);
  readonly loadError = signal(false);
  readonly busy = signal(false);
  readonly confirmation = signal<'delete' | 'archive' | null>(null);
  readonly success = signal<'delete' | 'archive' | null>(null);
  readonly error = signal('');
  ngOnInit() { this.load(); }
  load() {
    this.loading.set(true); this.loadError.set(false);
    forkJoin({ item: this.service.get(this.id()), employees: this.service.employees() }).subscribe({
      next: ({ item, employees }) => { this.item.set(item); const employee = employees.find(e => e.id === String(item.employeeId)); this.name.set(employee ? `${employee.firstName} ${employee.lastName}` : ''); this.loading.set(false); },
      error: () => { this.loading.set(false); this.loadError.set(true); },
    });
  }
  back() { void this.router.navigate(['/recognitions']); }
  edit() { void this.router.navigate(['/recognitions/edit', this.id()]); }
  ask(action: 'delete' | 'archive') { this.error.set(''); this.confirmation.set(action); }
  close() { if (!this.busy()) this.confirmation.set(null); }
  confirm() {
    const action = this.confirmation(); if (!action || this.busy()) return;
    if (action === 'archive' && this.item()?.status === 'archived') return;
    this.busy.set(true); this.error.set('');
    if (action === 'delete') {
      this.service.delete(this.id()).subscribe({ next: () => this.done('delete'), error: () => this.failed('DELETE_ERROR') });
    } else {
      this.service.update(this.id(), { status: 'archived', updatedAt: new Date().toISOString(), updatedBy: this.service.identity() }).subscribe({
        next: item => { this.item.set(item); this.done('archive'); }, error: () => this.failed('ARCHIVE_ERROR'),
      });
    }
  }
  private done(action: 'delete' | 'archive') { this.busy.set(false); this.confirmation.set(null); this.success.set(action); }
  private failed(key: string) { this.busy.set(false); this.error.set('RECOGNITIONS.' + key); }
  finish() { const deleted = this.success() === 'delete'; this.success.set(null); if (deleted) this.back(); }
}
