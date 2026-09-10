import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA, Pipe, type PipeTransform } from '@angular/core';
import { NavigationEnd, Router, ActivatedRoute, UrlTree } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { of, Subject } from 'rxjs';
import { Layout } from './layout';
import { BreadcrumbComponent } from '../breadcrumb/breadcrumb';

@Pipe({ name: 'translate', standalone: true })
class MockTranslatePipe implements PipeTransform {
  transform(value: string): string {
    return value;
  }
}

/**
 * Ticket #325: Scheduling sidebar group — expandable, auto-expanding on
 * scheduling routes, three disabled coming-soon children, one live entry.
 */
describe('Layout scheduling group', () => {
  let fixture: ComponentFixture<Layout>;
  let component: Layout;
  let routerEvents: Subject<unknown>;
  let navigated: string[][];
  let routerStub: {
    url: string;
    events: Subject<unknown>;
    navigate: (commands: string[]) => Promise<boolean>;
    navigateByUrl: (url: unknown) => Promise<boolean>;
    createUrlTree: () => UrlTree;
    serializeUrl: () => string;
    isActive: () => boolean;
  };

  async function setup(url: string): Promise<void> {
    Object.defineProperty(window, 'innerWidth', { configurable: true, value: 1440 });
    routerEvents = new Subject<unknown>();
    navigated = [];
    routerStub = {
      url,
      events: routerEvents,
      navigate: (commands: string[]) => {
        navigated.push(commands);
        return Promise.resolve(true);
      },
      navigateByUrl: () => Promise.resolve(true),
      createUrlTree: () => ({} as UrlTree),
      serializeUrl: () => '',
      isActive: () => false,
    };

    await TestBed.configureTestingModule({
      imports: [Layout],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        { provide: Router, useValue: routerStub },
        { provide: ActivatedRoute, useValue: { snapshot: { params: {}, queryParams: {} } } },
        {
          provide: TranslateService,
          useValue: { instant: (key: string) => key, onLangChange: of({}), use: () => of({}) },
        },
      ],
    })
      .overrideComponent(Layout, {
        remove: { imports: [TranslatePipe] },
        add: { imports: [MockTranslatePipe] },
      })
      .overrideComponent(BreadcrumbComponent, {
        remove: { imports: [TranslatePipe] },
        add: { imports: [MockTranslatePipe] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(Layout);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  function childLabels(): string[] {
    return Array.from(fixture.nativeElement.querySelectorAll('.nav-group-children .nav-child-label')).map(
      (el) => (el as HTMLElement).textContent?.trim() ?? '',
    );
  }

  it('should auto-expand the Scheduling group on a scheduling route', async () => {
    await setup('/scheduling/shift-templates');

    expect(component.isGroupExpanded('/scheduling')).toBe(true);
    expect(childLabels()).toEqual([
      'LAYOUT.NAV.SHIFT_MANAGEMENT',
      'LAYOUT.NAV.EMPLOYEE_ASSIGNMENT',
      'LAYOUT.NAV.SHIFT_MARKET',
      'LAYOUT.NAV.SHIFT_TEMPLATES',
    ]);
  });

  it('should stay collapsed outside scheduling routes', async () => {
    await setup('/employees');

    expect(component.isGroupExpanded('/scheduling')).toBe(false);
    expect(fixture.nativeElement.querySelector('.nav-group-children')).toBeNull();
  });

  it('should toggle the group on parent click', async () => {
    await setup('/employees');

    const toggle = fixture.nativeElement.querySelector('.nav-group-toggle') as HTMLElement;
    toggle.click();
    fixture.detectChanges();
    expect(component.isGroupExpanded('/scheduling')).toBe(true);

    toggle.click();
    fixture.detectChanges();
    expect(component.isGroupExpanded('/scheduling')).toBe(false);
  });

  it('should disable the unfinished children and keep Shift Templates live', async () => {
    await setup('/scheduling/shift-templates');

    const disabled = Array.from(
      fixture.nativeElement.querySelectorAll('.nav-group-children .nav-child.is-disabled'),
    ).map((el) => (el as HTMLElement).getAttribute('aria-disabled'));
    expect(disabled).toEqual(['true', 'true', 'true']);

    const live = fixture.nativeElement.querySelector(
      '.nav-group-children .nav-child:not(.is-disabled) .nav-child-label',
    ) as HTMLElement;
    expect(live.textContent?.trim()).toBe('LAYOUT.NAV.SHIFT_TEMPLATES');
  });

  it('should navigate natively on live children and keep disabled children inert', async () => {
    await setup('/scheduling/shift-templates');

    const live = fixture.nativeElement.querySelector(
      '.nav-group-children a.nav-child:not(.is-disabled)',
    ) as HTMLElement;
    expect(live.tagName).toBe('A');

    const disabled = Array.from(
      fixture.nativeElement.querySelectorAll('.nav-group-children .nav-child.is-disabled'),
    ) as HTMLElement[];
    expect(disabled.length).toBe(3);
    for (const el of disabled) {
      expect(el.tagName).toBe('SPAN');
    }
  });

  it('should close the sidebar on mobile when a live child is clicked', async () => {
    await setup('/scheduling/shift-templates');
    component.isMobileDevice.set(true);
    component.isSidebarOpen.set(true);
    fixture.detectChanges();

    const live = fixture.nativeElement.querySelector(
      '.nav-group-children a.nav-child:not(.is-disabled)',
    ) as HTMLElement;
    live.click();
    expect(component.isSidebarOpen()).toBe(false);
  });

  it('should mark the child matching the current route as active', async () => {
    await setup('/scheduling/shift-templates');

    const active = fixture.nativeElement.querySelector(
      '.nav-group-children .nav-child.is-active .nav-child-label',
    ) as HTMLElement;
    expect(active.textContent?.trim()).toBe('LAYOUT.NAV.SHIFT_TEMPLATES');
  });

  it('should auto-expand when navigating into a scheduling route later', async () => {
    await setup('/employees');
    expect(component.isGroupExpanded('/scheduling')).toBe(false);

    routerStub.url = '/scheduling/shift-market';
    routerEvents.next(new NavigationEnd(1, '/scheduling/shift-market', '/scheduling/shift-market'));
    fixture.detectChanges();

    expect(component.isGroupExpanded('/scheduling')).toBe(true);
  });
});
