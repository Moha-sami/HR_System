import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { RecognitionViewComponent } from './recognition-view.component';
import { RecognitionService } from '../../services/recognition.service';
import type { Recognition } from '../../models/recognition.models';

describe('Recognition view actions', () => {
  const item: Recognition = { id: '1', employeeId: '1', title: 'Thanks', description: 'Great work', points: null, status: 'published', publishAt: null,
    createdAt: '2026-01-01T10:00:00Z', updatedAt: '2026-01-01T10:00:00Z', createdBy: 'hr@example.com', updatedBy: 'hr@example.com' };
  let service: { get: ReturnType<typeof vi.fn>; employees: ReturnType<typeof vi.fn>; delete: ReturnType<typeof vi.fn>; update: ReturnType<typeof vi.fn>; identity: ReturnType<typeof vi.fn> };
  const navigate = vi.fn();
  beforeEach(() => {
    navigate.mockClear();
    service = { get: vi.fn(() => of(item)), employees: vi.fn(() => of([])), delete: vi.fn(() => of({})), update: vi.fn(() => of({ ...item, status: 'archived' })), identity: vi.fn(() => 'current@example.com') };
    TestBed.configureTestingModule({ imports: [RecognitionViewComponent], providers: [provideTranslateService(),
      { provide: Router, useValue: { navigate } }, { provide: RecognitionService, useValue: service }] });
  });
  function view() { const fixture = TestBed.createComponent(RecognitionViewComponent); fixture.componentRef.setInput('id', '1'); fixture.detectChanges(); return fixture.componentInstance; }
  it('requires confirmation and waits for acknowledgement before navigating after deletion', () => {
    const pending = new Subject<unknown>(); service.delete.mockReturnValue(pending);
    const component = view(); component.ask('delete'); expect(service.delete).not.toHaveBeenCalled();
    component.confirm(); component.confirm(); expect(service.delete).toHaveBeenCalledTimes(1); expect(navigate).not.toHaveBeenCalled();
    pending.next({}); expect(component.success()).toBe('delete'); expect(navigate).not.toHaveBeenCalled();
    component.finish(); expect(navigate).toHaveBeenCalledWith(['/recognitions']);
  });
  it('archives with only status and update metadata, then stays on the view', () => {
    const component = view(); component.ask('archive'); component.confirm();
    expect(service.update).toHaveBeenCalledWith('1', { status: 'archived', updatedAt: expect.any(String), updatedBy: 'current@example.com' });
    expect(component.item()?.status).toBe('archived'); component.finish(); expect(navigate).not.toHaveBeenCalled();
  });
});
