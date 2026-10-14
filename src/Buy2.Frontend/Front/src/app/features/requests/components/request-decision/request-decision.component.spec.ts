import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RequestDecision } from './request-decision';

describe('RequestDecision', () => {
  let component: RequestDecision;
  let fixture: ComponentFixture<RequestDecision>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RequestDecision],
    }).compileComponents();

    fixture = TestBed.createComponent(RequestDecision);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
