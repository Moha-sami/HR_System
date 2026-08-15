import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { AccessTypeTabsComponent } from './access-type-tabs.component';
import { PERMISSION_GROUPS } from '../../models/permission';

describe('AccessTypeTabsComponent', () => {
  let fixture: ComponentFixture<AccessTypeTabsComponent>;
  let component: AccessTypeTabsComponent;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AccessTypeTabsComponent],
    });
    fixture = TestBed.createComponent(AccessTypeTabsComponent);
    component = fixture.componentInstance;
    component.group = PERMISSION_GROUPS[0]; // Employee Management (has access config)
    component.permissions = [];
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render chips for each access type', () => {
    const buttons = fixture.nativeElement.querySelectorAll('button:not(:last-child)');
    expect(buttons.length).toBe(PERMISSION_GROUPS[0].access?.types.length);
  });

  it('should emit permissionsChange when chip clicked', () => {
    let emitted: string[] | null = null;
    component.permissionsChange.subscribe((perms) => {
      emitted = perms;
    });

    component.setAccessType('region');
    expect(emitted).toEqual(['employee.access.type.region']);
  });

  it('should remove old access type tokens when new chip clicked', () => {
    component.permissions = ['employee.access.type.all', 'employee.group.cairo'];
    fixture.detectChanges();

    let emitted: string[] | null = null;
    component.permissionsChange.subscribe((perms) => {
      emitted = perms;
    });

    component.setAccessType('region');
    expect(emitted).toEqual(['employee.access.type.region']); // all + cairo removed
  });

  it('should show active state for active access type', () => {
    component.permissions = ['employee.access.type.region'];
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    expect(button.classList).toContain('bg-blue-600');
  });

  it('should call openGroupsModal when Choose Groups clicked', () => {
    vi.spyOn(console, 'warn');
    const button = fixture.nativeElement.querySelector('button:last-child');
    button.click();
    expect(console.warn).toHaveBeenCalledWith(
      '[AccessTypeTabsComponent] Groups modal not implemented yet',
    );
  });
});
