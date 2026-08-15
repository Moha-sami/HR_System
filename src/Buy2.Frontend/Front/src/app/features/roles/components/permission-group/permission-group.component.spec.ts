import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { PermissionGroupComponent } from './permission-group.component';
import { PERMISSION_GROUPS } from '../../models/permission';

describe('PermissionGroupComponent', () => {
  let fixture: ComponentFixture<PermissionGroupComponent>;
  let component: PermissionGroupComponent;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [PermissionGroupComponent],
    });
    fixture = TestBed.createComponent(PermissionGroupComponent);
    component = fixture.componentInstance;
    component.group = PERMISSION_GROUPS[0]; // Employee Management
    component.permissions = [];
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render group title', () => {
    const h3 = fixture.nativeElement.querySelector('h3');
    expect(h3.textContent).toContain('Employee Management');
  });

  it('should render toggle pills for each toggle', () => {
    const buttons = fixture.nativeElement.querySelectorAll('button.toggle');
    expect(buttons.length).toBe(PERMISSION_GROUPS[0].toggles.length);
  });

  it('should emit permissionsChange when toggle clicked', () => {
    let emitted: string[] | null = null;
    component.permissionsChange.subscribe((perms) => {
      emitted = perms;
    });

    component.togglePermission('add');
    expect(emitted).toEqual(['employee.add']);

    component.permissions = ['employee.add'];
    fixture.detectChanges();
    component.togglePermission('add');
    expect(emitted).toEqual([]);
  });

  it('should show active state for toggles in permissions', () => {
    component.permissions = ['employee.add'];
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button.toggle');
    expect(button.classList).toContain('active');
  });

  it('should show access section stub for groups with access config', () => {
    component.group = PERMISSION_GROUPS[0]; // has access
    fixture.detectChanges();

    const accessSection = fixture.nativeElement.querySelector('.access-section');
    expect(accessSection).toBeTruthy();
    expect(accessSection.textContent).toContain('AccessTypeTabsComponent stub');
  });
});
