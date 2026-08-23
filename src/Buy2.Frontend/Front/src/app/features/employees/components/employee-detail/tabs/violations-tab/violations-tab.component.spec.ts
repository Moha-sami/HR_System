import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ViolationsTabComponent } from './violations-tab.component';
import { TranslatePipe } from '@ngx-translate/core';

describe('ViolationsTabComponent', () => {
  let component: ViolationsTabComponent;
  let fixture: ComponentFixture<ViolationsTabComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ViolationsTabComponent],
    })
      .overrideComponent(ViolationsTabComponent, {
        remove: { imports: [TranslatePipe] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(ViolationsTabComponent);
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
