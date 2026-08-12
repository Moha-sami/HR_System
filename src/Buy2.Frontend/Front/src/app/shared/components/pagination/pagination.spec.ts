import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Pagination } from './pagination';

describe('Pagination', () => {
  let component: Pagination;
  let fixture: ComponentFixture<Pagination>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Pagination],
    }).compileComponents();

    fixture = TestBed.createComponent(Pagination);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('starts on page 1 and renders all small page ranges', () => {
    fixture.componentRef.setInput('totalPages', 5);
    fixture.detectChanges();

    expect(component.currentPage()).toBe(1);
    expect(component.pages()).toEqual([1, 2, 3, 4, 5]);
  });

  it('initializes from initialPage and clamps it to the valid page range', () => {
    fixture.componentRef.setInput('totalPages', 10);
    fixture.componentRef.setInput('initialPage', 8);
    fixture.detectChanges();

    expect(component.currentPage()).toBe(8);

    fixture = TestBed.createComponent(Pagination);
    fixture.componentRef.setInput('totalPages', 10);
    fixture.componentRef.setInput('initialPage', 20);
    fixture.detectChanges();

    expect(fixture.componentInstance.currentPage()).toBe(10);
  });

  it('updates its internal page and emits the selected page', () => {
    fixture.componentRef.setInput('totalPages', 10);
    let emittedPage: number | undefined;
    component.pageChanged.subscribe((page) => (emittedPage = page));

    component.changePage(5);

    expect(component.currentPage()).toBe(5);
    expect(emittedPage).toBe(5);
  });

  it('creates ellipses for a middle page without duplicate pages', () => {
    fixture.componentRef.setInput('totalPages', 10);
    component.changePage(5);

    expect(component.pages()).toEqual([1, 'ellipsis', 4, 5, 6, 'ellipsis', 10]);
  });

  it('adapts the page range near the first and last pages', () => {
    fixture.componentRef.setInput('totalPages', 10);
    expect(component.pages()).toEqual([1, 2, 'ellipsis', 10]);

    component.changePage(10);
    expect(component.pages()).toEqual([1, 'ellipsis', 9, 10]);
  });

  it('clamps the current page when totalPages decreases', () => {
    fixture.componentRef.setInput('totalPages', 10);
    component.changePage(8);

    fixture.componentRef.setInput('totalPages', 3);
    fixture.detectChanges();

    expect(component.currentPage()).toBe(3);
  });

  it('uses ghost controls and marks the active page', () => {
    fixture.componentRef.setInput('totalPages', 3);
    fixture.detectChanges();

    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    expect(buttons).toHaveLength(5);
    expect(fixture.nativeElement.querySelector('app-button[aria-current="page"]')).toBeTruthy();
    expect(buttons.every((button) => button.classList.contains('bg-transparent'))).toBe(true);
  });
});
