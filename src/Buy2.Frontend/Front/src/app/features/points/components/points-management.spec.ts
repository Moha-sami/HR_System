import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PointsManagement } from '../points-management';

describe('PointsManagement', () => {
  let component: PointsManagement;
  let fixture: ComponentFixture<PointsManagement>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PointsManagement],
    }).compileComponents();

    fixture = TestBed.createComponent(PointsManagement);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
