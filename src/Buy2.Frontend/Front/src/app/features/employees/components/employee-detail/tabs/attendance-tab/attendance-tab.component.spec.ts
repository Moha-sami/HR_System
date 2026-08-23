import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { AttendanceTabComponent } from './attendance-tab.component';
import { TranslatePipe } from '@ngx-translate/core';

describe('AttendanceTabComponent', () => {
  let component: AttendanceTabComponent;
  let fixture: ComponentFixture<AttendanceTabComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AttendanceTabComponent],
    })
      .overrideComponent(AttendanceTabComponent, {
        remove: { imports: [TranslatePipe] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(AttendanceTabComponent);
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
