import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { PaginationDocs } from './pagination-docs';

describe('PaginationDocs', () => {
  let component: PaginationDocs;
  let fixture: ComponentFixture<PaginationDocs>;

  beforeEach(async () => {
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [PaginationDocs],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(PaginationDocs);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
