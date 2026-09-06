import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { RecognitionListComponent } from './recognition-list.component';
import { RecognitionService } from '../../services/recognition.service';
import type { Recognition } from '../../models/recognition.models';

describe('Recognition list rendering and pagination', () => {
  it('searches before pagination and synchronizes the visible paginator after resetting filters', async () => {
    const items: Recognition[] = Array.from({ length: 12 }, (_, i) => ({ id: String(i), employeeId: '1', title: `Award ${i}`, description: 'Thanks',
      points: 10, status: i === 11 ? 'archived' : 'published', publishAt: null, createdAt: '2026-01-01T10:00:00Z', updatedAt: '2026-01-01T10:00:00Z', createdBy: i === 11 ? 'special@example.com' : 'hr@example.com', updatedBy: 'hr@example.com' }));
    TestBed.configureTestingModule({ imports: [RecognitionListComponent], providers: [provideTranslateService(),
      { provide: Router, useValue: { navigate: vi.fn() } },
      { provide: RecognitionService, useValue: { list: () => of(items), employees: () => of([{ id: '1', firstName: 'Ahmed', lastName: 'Ali' }]) } },
    ] });
    const fixture = TestBed.createComponent(RecognitionListComponent); fixture.detectChanges(); await fixture.whenStable();
    const component = fixture.componentInstance;
    expect(component.rows().length).toBe(8); expect(component.totalPages()).toBe(2);
    component.page.set(2); fixture.detectChanges(); await fixture.whenStable();
    expect(component.paginator()?.currentPage()).toBe(2);
    component.search('special@'); fixture.detectChanges(); await fixture.whenStable();
    expect(component.rows().map(row => row.id)).toEqual(['11']);
    expect(component.page()).toBe(1); expect(component.paginator()?.currentPage()).toBe(1);
    component.search('Ahmed'); expect(component.filtered().length).toBe(12);
    component.filter('archived'); expect(component.rows().map(row => row.id)).toEqual(['11']);
    component.filter(''); component.search('Award 10'); expect(component.rows()[0].id).toBe('10');
    expect(fixture.nativeElement.querySelector('app-table')).toBeTruthy();
  });
});
