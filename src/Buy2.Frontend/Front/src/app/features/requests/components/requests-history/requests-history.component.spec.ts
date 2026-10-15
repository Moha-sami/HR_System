import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RequestsHistoryComponent } from './requests-history.component';

describe('RequestsHistoryComponent', () => {
  let component: RequestsHistoryComponent;
  let fixture: ComponentFixture<RequestsHistoryComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RequestsHistoryComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(RequestsHistoryComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
