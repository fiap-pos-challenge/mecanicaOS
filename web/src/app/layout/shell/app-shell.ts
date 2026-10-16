import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { messages } from '../../core/i18n/messages.pt-BR';
import { provideNotificationData } from '../../features/notifications/data/notification.gateway';
import { NotificationsStore } from '../../features/notifications/state/notifications.store';
import { UiToastRegion } from '../../shared/ui/toast/toast';
import { AppBottomNav } from '../bottom-nav/bottom-nav';
import { LayoutState } from '../layout-state';
import { SessionWatch } from '../session-watch/session-watch';
import { AppSidebar } from '../sidebar/sidebar';
import { AppTopbar } from '../topbar/topbar';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, AppSidebar, AppTopbar, AppBottomNav, SessionWatch, UiToastRegion],
  providers: [provideNotificationData(), NotificationsStore],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppShell {
  protected readonly layout = inject(LayoutState);
  protected readonly text = messages.shell;

  constructor() {
    void inject(NotificationsStore).load();
    inject(Router)
      .events.pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.layout.closeDrawer());
  }
}
