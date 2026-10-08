import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RequestsHistory } from './requests-history';

describe('RequestsHistory', () => {
  let component: RequestsHistory;
  let fixture: ComponentFixture<RequestsHistory>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RequestsHistory],
    }).compileComponents();

    fixture = TestBed.createComponent(RequestsHistory);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
