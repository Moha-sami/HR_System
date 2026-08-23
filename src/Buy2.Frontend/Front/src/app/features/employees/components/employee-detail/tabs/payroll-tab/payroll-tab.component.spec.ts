import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { PayrollTabComponent } from './payroll-tab.component';
import { TranslatePipe } from '@ngx-translate/core';

describe('PayrollTabComponent', () => {
  let component: PayrollTabComponent;
  let fixture: ComponentFixture<PayrollTabComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PayrollTabComponent],
    })
      .overrideComponent(PayrollTabComponent, {
        remove: { imports: [TranslatePipe] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(PayrollTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have comingSoon flag set to true', () => {
    expect(component.comingSoon).toBe(true);
  });
});
