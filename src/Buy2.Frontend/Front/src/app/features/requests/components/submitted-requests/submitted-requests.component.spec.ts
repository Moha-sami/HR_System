import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SubmittedRequestsComponent } from './submitted-requests.component';

describe('SubmittedRequestsComponent', () => {
  let component: SubmittedRequestsComponent;
  let fixture: ComponentFixture<SubmittedRequestsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SubmittedRequestsComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(SubmittedRequestsComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
