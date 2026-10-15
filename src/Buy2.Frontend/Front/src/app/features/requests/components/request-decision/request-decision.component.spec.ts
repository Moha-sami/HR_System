import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { RequestDecisionComponent } from './request-decision.component';

describe('RequestDecisionComponent', () => {
  let component: RequestDecisionComponent;
  let fixture: ComponentFixture<RequestDecisionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RequestDecisionComponent],
      providers: [provideHttpClient(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(RequestDecisionComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
