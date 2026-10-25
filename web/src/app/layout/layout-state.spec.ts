import { TestBed } from '@angular/core/testing';
import { MemoryStorage, provideFakeBreakpoints } from '../../testing/layout';
import { PREFERENCE_STORAGE, PREFERENCE_STORAGE_KEYS } from '../core/preferences/ui-preferences';
import { LayoutState } from './layout-state';

describe('LayoutState', () => {
  function setup(storage = new MemoryStorage(), mobile = false) {
    const breakpoints = provideFakeBreakpoints(mobile);
    TestBed.configureTestingModule({
      providers: [breakpoints.provider, { provide: PREFERENCE_STORAGE, useValue: storage }],
    });
    return { layout: TestBed.inject(LayoutState), storage, breakpoints };
  }

  it('restores the collapsed menu preference', () => {
    const storage = new MemoryStorage();
    storage.setItem(PREFERENCE_STORAGE_KEYS.sidebar, 'collapsed');

    expect(setup(storage).layout.collapsed()).toBe(true);
  });

  it('toggles and remembers the collapsed menu', () => {
    const { layout, storage } = setup();

    layout.toggleCollapsed();
    expect(layout.collapsed()).toBe(true);
    expect(storage.getItem(PREFERENCE_STORAGE_KEYS.sidebar)).toBe('collapsed');

    layout.toggleCollapsed();
    expect(storage.getItem(PREFERENCE_STORAGE_KEYS.sidebar)).toBe('expanded');
  });

  it('opens and closes the drawer and tracks the mobile breakpoint', () => {
    const { layout, breakpoints } = setup();

    layout.openDrawer();
    expect(layout.drawerOpen()).toBe(true);
    layout.closeDrawer();
    expect(layout.drawerOpen()).toBe(false);

    expect(layout.isMobile()).toBe(false);
    breakpoints.observer.setMobile(true);
    expect(layout.isMobile()).toBe(true);
  });
});
