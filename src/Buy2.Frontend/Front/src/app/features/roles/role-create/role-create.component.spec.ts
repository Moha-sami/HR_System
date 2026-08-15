import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { RoleCreateComponent } from './role-create.component';
import { RoleService } from '../services/role.service';
import { Router } from '@angular/router';
import { PERMISSION_GROUPS } from '../models/permission';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import type { Role } from '../models/role';

class MockRoleService {
  create = vi.fn().mockReturnValue(of({ id: 1, roleName: 'Test Role', permissions: [] } as Role));
}

class MockRouter {
  navigate = vi.fn();
}

describe('RoleCreateComponent', () => {
  let fixture: ComponentFixture<RoleCreateComponent>;
  let component: RoleCreateComponent;
  let roleService: RoleService;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [RoleCreateComponent, FormsModule, RouterLink],
      providers: [
        { provide: RoleService, useClass: MockRoleService },
        { provide: Router, useClass: MockRouter },
      ],
    });
    fixture = TestBed.createComponent(RoleCreateComponent);
    component = fixture.componentInstance;
    roleService = TestBed.inject(RoleService);
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should disable Save button when roleName is empty', () => {
    component.roleName = '';
    fixture.detectChanges();
    const button = fixture.nativeElement.querySelector('button[type="submit"]');
    expect(button.disabled).toBeTruthy();
  });

  it('should enable Save button when roleName is not empty', () => {
    component.roleName = 'Admin';
    fixture.detectChanges();
    const button = fixture.nativeElement.querySelector('button[type="submit"]');
    expect(button.disabled).toBeFalsy();
  });

  it('should call RoleService.create and navigate on submit', () => {
    component.roleName = 'Admin';
    component.permissions = ['employee.add'];
    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('form');
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(roleService.create).toHaveBeenCalledWith({
      roleName: 'Admin',
      permissions: ['employee.add'],
    });
    expect(router.navigate).toHaveBeenCalledWith(['/roles']);
  });

  it('should navigate back on Discard', () => {
    const button = fixture.nativeElement.querySelector('button:not([type="submit"])');
    button.click();
    expect(router.navigate).toHaveBeenCalledWith(['/roles']);
  });

  it('should render permission groups', () => {
    const groups = fixture.nativeElement.querySelectorAll('app-permission-group');
    expect(groups.length).toBe(PERMISSION_GROUPS.length);
  });
});
