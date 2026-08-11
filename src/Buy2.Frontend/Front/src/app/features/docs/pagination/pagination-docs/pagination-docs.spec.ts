import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PaginationDocs } from './pagination-docs';

describe('PaginationDocs', () => {
  let component: PaginationDocs;
  let fixture: ComponentFixture<PaginationDocs>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PaginationDocs],
    }).compileComponents();

    fixture = TestBed.createComponent(PaginationDocs);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
