import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { RoleListComponent } from './role-list.component';
import { RoleService } from '../services/role.service';
import { MOCK_ROLES } from '../services/role.service';

class MockRoleService {
  roles = vi.fn().mockReturnValue(MOCK_ROLES);
  loading = vi.fn().mockReturnValue(false);
  error = vi.fn().mockReturnValue(null);
  loadAll = vi.fn();
}

describe('RoleListComponent', () => {
  let fixture: ComponentFixture<RoleListComponent>;
  let component: RoleListComponent;
  let roleService: RoleService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [RoleListComponent],
      providers: [{ provide: RoleService, useClass: MockRoleService }],
    });
    fixture = TestBed.createComponent(RoleListComponent);
    component = fixture.componentInstance;
    roleService = TestBed.inject(RoleService);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should call loadAll on init', () => {
    expect(roleService.loadAll).toHaveBeenCalled();
  });

  it('should render loading state', () => {
    roleService.loading.set(true);
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Loading roles...');
  });

  it('should render error state', () => {
    roleService.error.set('Failed to load');
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Error: Failed to load');
  });

  it('should render role cards', () => {
    const cards = fixture.nativeElement.querySelectorAll('app-role-card');
    expect(cards.length).toBe(MOCK_ROLES.length);
  });

  it('should render Create Role button', () => {
    const button = fixture.nativeElement.querySelector('a[routerLink="/roles/create"]');
    expect(button).toBeTruthy();
    expect(button.textContent).toContain('Create Role');
  });
});
