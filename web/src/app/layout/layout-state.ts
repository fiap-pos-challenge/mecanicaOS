import { BreakpointObserver } from '@angular/cdk/layout';
import { inject, Injectable, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { UiPreferences } from '../core/preferences/ui-preferences';

export const MOBILE_QUERY = '(max-width: 780px)';

@Injectable({ providedIn: 'root' })
export class LayoutState {
  private readonly preferences = inject(UiPreferences);

  readonly isMobile = toSignal(
    inject(BreakpointObserver)
      .observe(MOBILE_QUERY)
      .pipe(map((state) => state.matches)),
    { initialValue: false },
  );

  readonly collapsed = signal(this.preferences.read('sidebar') === 'collapsed');
  readonly drawerOpen = signal(false);

  toggleCollapsed(): void {
    const collapsed = !this.collapsed();
    this.collapsed.set(collapsed);
    this.preferences.write('sidebar', collapsed ? 'collapsed' : 'expanded');
  }

  openDrawer(): void {
    this.drawerOpen.set(true);
  }

  closeDrawer(): void {
    this.drawerOpen.set(false);
  }
}
