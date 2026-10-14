import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SubmittedRequests } from './submitted-requests';

describe('SubmittedRequests', () => {
  let component: SubmittedRequests;
  let fixture: ComponentFixture<SubmittedRequests>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SubmittedRequests],
    }).compileComponents();

    fixture = TestBed.createComponent(SubmittedRequests);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
