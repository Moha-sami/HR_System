import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { RoleEditComponent } from './role-edit.component';
import { RoleService } from '../services/role.service';
import { Router } from '@angular/router';
import { PERMISSION_GROUPS } from '../models/permission';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MOCK_ROLES } from '../services/role.service';

class MockRoleService {
  get = vi.fn().mockReturnValue(of(MOCK_ROLES[0]));
}

class MockRouter {
  navigate = vi.fn();
}

describe('RoleEditComponent', () => {
  let fixture: ComponentFixture<RoleEditComponent>;
  let component: RoleEditComponent;
  let roleService: RoleService;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [RoleEditComponent, FormsModule, RouterLink],
      providers: [
        { provide: RoleService, useClass: MockRoleService },
        { provide: Router, useClass: MockRouter },
      ],
    });
    fixture = TestBed.createComponent(RoleEditComponent);
    component = fixture.componentInstance;
    component.id = '1';
    roleService = TestBed.inject(RoleService);
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load role on init', () => {
    expect(roleService.get).toHaveBeenCalledWith('1');
    expect(component.roleName).toBe(MOCK_ROLES[0].roleName);
    expect(component.permissions).toEqual(MOCK_ROLES[0].permissions);
  });

  it('should disable Update button when roleName is empty', () => {
    component.roleName = '';
    fixture.detectChanges();
    const button = fixture.nativeElement.querySelector('button[type="submit"]');
    expect(button.disabled).toBeTruthy();
  });

  it('should enable Update button when roleName is not empty', () => {
    component.roleName = 'Admin';
    fixture.detectChanges();
    const button = fixture.nativeElement.querySelector('button[type="submit"]');
    expect(button.disabled).toBeFalsy();
  });

  it('should throw error on submit (update stubbed)', () => {
    component.roleName = 'Admin';
    expect(() => component.onSubmit()).toThrowError(/Update endpoint not implemented/);
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
