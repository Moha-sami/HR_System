import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { Observable, of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { RecognitionFormComponent } from './recognition-form.component';
import { RecognitionService } from '../../services/recognition.service';
import type { Recognition, RecognitionStatus } from '../../models/recognition.models';

describe('Recognition shared form', () => {
  const record: Recognition = { id: 'a', employeeId: '1', title: 'Original', description: 'Description', points: 10, status: 'archived', publishAt: null,
    createdAt: '2020-01-01T00:00:00Z', updatedAt: '2020-01-01T00:00:00Z', createdBy: 'original@example.com', updatedBy: 'original@example.com' };
  let service: { identity: ReturnType<typeof vi.fn>; employees: ReturnType<typeof vi.fn>; get: ReturnType<typeof vi.fn>; create: ReturnType<typeof vi.fn>; update: ReturnType<typeof vi.fn> };
  beforeEach(() => {
    service = { identity: vi.fn(() => 'current@example.com'), employees: vi.fn(() => of([{ id: '1', firstName: 'A', lastName: 'B' }])),
      get: vi.fn(() => of(record)), create: vi.fn(value => of({ id: 'new', ...value })), update: vi.fn((id, value) => of({ id, ...value })) };
    TestBed.configureTestingModule({ imports: [RecognitionFormComponent], providers: [provideTranslateService(),
      { provide: RecognitionService, useValue: service }, { provide: Router, useValue: { navigate: vi.fn() } },
    ] });
  });
  function form(edit = false) {
    const fixture = TestBed.createComponent(RecognitionFormComponent);
    if (edit) fixture.componentRef.setInput('id', 'a');
    const component = fixture.componentInstance; component.ngOnInit();
    component.form.patchValue({ employeeId: '1', title: ' Changed ', description: ' Current description ', points: 0 });
    component.form.markAsDirty(); return component;
  }
  it('creates archive with all trimmed current values in one POST', () => {
    const component = form(); component.save('archived');
    expect(service.create).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ status: 'archived', title: 'Changed', description: 'Current description', points: 0, employeeId: '1', createdBy: 'current@example.com' }));
    expect(service.update).not.toHaveBeenCalled();
  });
  it('updates archive in one PATCH and preserves original creation metadata', () => {
    const component = form(true); component.save('archived');
    expect(service.update).toHaveBeenCalledExactlyOnceWith('a', expect.objectContaining({ status: 'archived', title: 'Changed', createdBy: record.createdBy, createdAt: record.createdAt, updatedBy: 'current@example.com' }));
    expect(service.create).not.toHaveBeenCalled();
  });
  it.each(['draft', 'archived', 'primary'] as const)('validates all required fields for %s', action => {
    const component = form(); component.form.controls.title.setValue('   '); component.save(action);
    expect(service.create).not.toHaveBeenCalled(); expect(component.invalid('title')).toBe(true);
  });
  it('preserves archived status on ordinary save', () => {
    form(true).save('primary'); expect(service.update.mock.calls[0][1].status).toBe('archived');
  });
  it('previews without persistence or clearing dirty values', () => {
    const component = form(); component.showPreview(); expect(component.preview()?.title).toBe('Changed');
    expect(component.form.dirty).toBe(true); expect(service.create).not.toHaveBeenCalled(); expect(service.update).not.toHaveBeenCalled();
  });
  it('waits for create draft save before allowing leave', () => {
    const pending = new Subject<Recognition>(); service.create.mockReturnValue(pending);
    const component = form(); const result: boolean[] = [];
    (component.canDeactivate() as Observable<boolean>).subscribe(value => result.push(value));
    component.save('leave'); expect(result).toEqual([]); expect(service.create.mock.calls[0][0].status).toBe('draft');
    pending.next(record); expect(result).toEqual([true]);
  });
  it.each(['draft', 'archived', 'published', 'scheduled'] as RecognitionStatus[])('preserves %s when saving before leaving edit', status => {
    service.get.mockReturnValue(of({ ...record, status })); const component = form(true);
    component.save('leave'); expect(service.update.mock.calls[0][1].status).toBe(status);
  });
  it('stays dirty and cancels navigation when save fails', () => {
    const pending = new Subject<Recognition>(); service.update.mockReturnValue(pending);
    const component = form(true); const result: boolean[] = [];
    (component.canDeactivate() as Observable<boolean>).subscribe(value => result.push(value));
    component.save('leave'); pending.error(new Error('offline'));
    expect(result).toEqual([false]); expect(component.form.dirty).toBe(true); expect(component.error()).toBe('RECOGNITIONS.SAVE_ERROR');
  });
});
