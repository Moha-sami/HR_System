import { Component, inject, input, signal, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { Subject, Observable } from 'rxjs';
import { ButtonComponent } from '../../../../shared/components/button/button.component';
import { ModalComponent } from '../../../../shared/components/modal/modal.component';
import { ModalBodyComponent } from '../../../../shared/components/modal/modal-body.component';
import { RecognitionContentComponent } from '../recognition-content/recognition-content.component';
import { RecognitionService } from '../../services/recognition.service';
import type { Recognition, RecognitionEmployee, RecognitionInput, RecognitionStatus } from '../../models/recognition.models';
import { FILE_ACCEPT, MAX_FILE_MIB, localDateTime, publishTimestamp, requiredTrimmed, saveStatus, scheduledStatus, validateFile, validPoints } from '../../utils/recognition.utils';

type SaveAction = 'draft' | 'archived' | 'primary' | 'leave';
@Component({
  selector: 'app-recognition-form', standalone: true,
  imports: [ReactiveFormsModule, TranslatePipe, ButtonComponent, ModalComponent, ModalBodyComponent, RecognitionContentComponent],
  templateUrl: './recognition-form.component.html', styleUrl: '../../recognitions.css',
})
export class RecognitionFormComponent implements OnInit, OnDestroy {
  readonly id = input<string>();
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(RecognitionService);
  private readonly router = inject(Router);
  private existing: Recognition | null = null;
  private pendingLeave: Subject<boolean> | null = null;
  private reader: FileReader | null = null;
  readonly employees = signal<RecognitionEmployee[]>([]);
  readonly loading = signal(false);
  readonly loadError = signal(false);
  readonly lookupError = signal(false);
  readonly saving = signal(false);
  readonly reading = signal(false);
  readonly dragging = signal(false);
  readonly preview = signal<Recognition | null>(null);
  readonly unsaved = signal(false);
  readonly success = signal('');
  readonly error = signal('');
  readonly fileError = signal('');
  readonly dateError = signal(false);
  readonly maxFileMiB = MAX_FILE_MIB;
  readonly accept = FILE_ACCEPT;
  readonly form = this.fb.nonNullable.group({
    employeeId: ['', requiredTrimmed], title: ['', requiredTrimmed], description: ['', requiredTrimmed],
    points: this.fb.control<number | null>(null, validPoints), attachmentUrl: '', attachmentName: '', publishDate: '', publishTime: '',
  });
  ngOnInit() {
    this.loadEmployees();
    if (this.id()) this.load();
  }
  ngOnDestroy() { this.reader?.abort(); this.resolveLeave(false); }
  loadEmployees() {
    this.lookupError.set(false);
    this.service.employees().subscribe({ next: items => this.employees.set(items), error: () => this.lookupError.set(true) });
  }
  load() {
    this.loading.set(true); this.loadError.set(false);
    this.service.get(this.id()!).subscribe({
      next: item => {
        this.existing = item;
        const when = localDateTime(item.publishAt);
        this.form.patchValue({ employeeId: String(item.employeeId), title: item.title, description: item.description, points: item.points,
          attachmentUrl: item.attachmentUrl || '', attachmentName: item.attachmentName || '', publishDate: when.date, publishTime: when.time });
        this.form.markAsPristine(); this.loading.set(false);
      }, error: () => { this.loadError.set(true); this.loading.set(false); },
    });
  }
  invalid(name: string) { const c = this.form.get(name); return !!c && c.invalid && c.touched; }
  name() { const employee = this.employees().find(e => e.id === this.form.controls.employeeId.value); return employee ? `${employee.firstName} ${employee.lastName}` : ''; }
  back() { void this.router.navigate(['/recognitions']); }
  canDeactivate(): boolean | Observable<boolean> {
    if (this.saving() || this.reading()) return false;
    if (!this.form.dirty) return true;
    if (!this.pendingLeave) this.pendingLeave = new Subject<boolean>();
    this.unsaved.set(true);
    return this.pendingLeave.asObservable();
  }
  closeUnsaved() { if (!this.saving()) { this.unsaved.set(false); this.resolveLeave(false); } }
  discard() { if (!this.saving()) { this.form.markAsPristine(); this.unsaved.set(false); this.resolveLeave(true); } }
  private resolveLeave(allow: boolean) { this.pendingLeave?.next(allow); this.pendingLeave?.complete(); this.pendingLeave = null; }
  showPreview() {
    let at: string | null = null;
    try { at = publishTimestamp(this.form.controls.publishDate.value, this.form.controls.publishTime.value); } catch { /* Incomplete preview dates remain empty. */ }
    this.preview.set({ id: this.id() || '', ...this.payload(this.existing?.status || 'draft', at) });
  }
  selected(event: Event) {
    const element = event.target as HTMLInputElement;
    if (element.files?.[0]) this.file(element.files[0]);
    element.value = '';
  }
  drop(event: DragEvent) { event.preventDefault(); this.dragging.set(false); if (event.dataTransfer?.files[0]) this.file(event.dataTransfer.files[0]); }
  drag(event: DragEvent) { event.preventDefault(); this.dragging.set(true); }
  private file(file: File) {
    if (this.saving() || this.reading()) return;
    const error = validateFile(file);
    this.fileError.set(error || '');
    if (error) return;
    this.reading.set(true);
    const reader = this.reader = new FileReader();
    reader.onload = () => { this.form.patchValue({ attachmentUrl: String(reader.result), attachmentName: file.name }); this.fileError.set(''); this.form.markAsDirty(); this.reading.set(false); };
    reader.onerror = () => { this.fileError.set('RECOGNITIONS.FILE_READ_ERROR'); this.reading.set(false); };
    reader.onabort = () => this.reading.set(false);
    reader.readAsDataURL(file);
  }
  removeFile(input: HTMLInputElement) { this.form.patchValue({ attachmentUrl: '', attachmentName: '' }); input.value = ''; this.fileError.set(''); this.form.markAsDirty(); }
  save(action: SaveAction) {
    if (this.saving() || this.reading() || this.loadError() || this.loading()) return;
    this.error.set(''); this.dateError.set(false); this.form.markAllAsTouched();
    if (this.form.invalid || this.fileError()) { if (action === 'leave') this.closeUnsaved(); return; }
    let publishAt: string | null;
    try { publishAt = publishTimestamp(this.form.controls.publishDate.value, this.form.controls.publishTime.value); }
    catch { this.dateError.set(true); if (action === 'leave') this.closeUnsaved(); return; }
    let status: RecognitionStatus;
    if (action === 'draft' || action === 'archived') status = action;
    else if (action === 'leave') status = this.existing?.status || 'draft';
    else status = this.existing ? saveStatus(this.existing.status, publishAt) : scheduledStatus(publishAt);
    const payload = this.payload(status, publishAt);
    this.saving.set(true);
    const request = this.existing ? this.service.update(this.existing.id, payload) : this.service.create(payload);
    request.subscribe({
      next: item => {
        this.existing = item; this.saving.set(false); this.form.markAsPristine();
        if (action === 'leave') { this.unsaved.set(false); this.resolveLeave(true); return; }
        this.success.set(action === 'archived' ? 'ARCHIVE_SUCCESS' : action === 'draft' ? 'DRAFT_SUCCESS' : this.id() ? 'UPDATE_SUCCESS' : status === 'scheduled' ? 'SCHEDULE_SUCCESS' : 'PUBLISH_SUCCESS');
      },
      error: () => {
        this.saving.set(false); this.error.set('RECOGNITIONS.SAVE_ERROR');
        if (action === 'leave') { this.unsaved.set(false); this.resolveLeave(false); }
      },
    });
  }
  private payload(status: RecognitionStatus, publishAt: string | null): RecognitionInput {
    const value = this.form.getRawValue(); const now = new Date().toISOString(); const identity = this.service.identity();
    return { employeeId: value.employeeId, title: value.title.trim(), description: value.description.trim(), points: value.points,
      status, publishAt, attachmentUrl: value.attachmentUrl, attachmentName: value.attachmentName,
      createdAt: this.existing?.createdAt || now, updatedAt: now, createdBy: this.existing?.createdBy || identity, updatedBy: identity };
  }
  finish() { this.success.set(''); this.back(); }
}
