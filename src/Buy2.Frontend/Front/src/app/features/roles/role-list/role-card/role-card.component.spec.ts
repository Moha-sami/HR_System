import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { RoleCardComponent } from './role-card.component';
import { RoleService } from '../../services/role.service';
import { MOCK_ROLES } from '../../services/role.service';

class MockRoleService {
  remove = vi.fn().mockReturnValue(of(void 0));
}

describe('RoleCardComponent', () => {
  let fixture: ComponentFixture<RoleCardComponent>;
  let component: RoleCardComponent;
  let roleService: RoleService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [RoleCardComponent],
      providers: [{ provide: RoleService, useClass: MockRoleService }],
    });
    fixture = TestBed.createComponent(RoleCardComponent);
    component = fixture.componentInstance;
    component.role = MOCK_ROLES[0];
    roleService = TestBed.inject(RoleService);
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render role name and permission count', () => {
    const text = fixture.nativeElement.textContent;
    expect(text).toContain(MOCK_ROLES[0].roleName);
    expect(text).toContain(`${MOCK_ROLES[0].permissions.length} permissions`);
  });

  it('should call RoleService.remove on delete', () => {
    const button = fixture.nativeElement.querySelector('button:last-child');
    button.click();
    fixture.detectChanges();
    const menuButton = fixture.nativeElement.querySelector('button[aria-label="Delete"]');
    menuButton.click();
    expect(roleService.remove).toHaveBeenCalledWith(MOCK_ROLES[0].id);
  });

  it('should show confirm dialog on delete', () => {
    const button = fixture.nativeElement.querySelector('button:last-child');
    button.click();
    fixture.detectChanges();
    const menuButton = fixture.nativeElement.querySelector('button[aria-label="Delete"]');
    menuButton.click();
    expect(window.confirm).toHaveBeenCalled();
  });

  it('should toggle menu on button click', () => {
    const button = fixture.nativeElement.querySelector('button:last-child');
    button.click();
    fixture.detectChanges();
    expect(component.menuOpen).toBeTruthy();
    button.click();
    fixture.detectChanges();
    expect(component.menuOpen).toBeFalsy();
  });
});
