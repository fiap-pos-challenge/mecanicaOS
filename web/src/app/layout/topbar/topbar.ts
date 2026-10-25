import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import {
  Bell as BellIcon,
  LogOut as LogOutIcon,
  Menu as MenuIcon,
  Monitor as MonitorIcon,
  Moon as MoonIcon,
  Search as SearchIcon,
  Sun as SunIcon,
} from 'lucide';
import { UiIcon } from '../../shared/ui/icon/icon';
import { messages } from '../../core/i18n/messages.pt-BR';
import { ThemeService } from '../../core/theme/theme.service';
import { AuthStore } from '../../features/auth/state/auth.store';
import { NotificationCenter } from '../../features/notifications/components/notification-center';
import { NotificationsStore } from '../../features/notifications/state/notifications.store';
import { UiAvatar } from '../../shared/ui/avatar/avatar';
import { UiIconButton } from '../../shared/ui/icon-button/icon-button';
import { LayoutState } from '../layout-state';

const themeIcons = { light: SunIcon, dark: MoonIcon, system: MonitorIcon } as const;

@Component({
  selector: 'app-topbar',
  imports: [NotificationCenter, UiAvatar, UiIcon, UiIconButton],
  templateUrl: './topbar.html',
  styleUrl: './topbar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(document:keydown)': 'onDocumentKeydown($event)',
  },
})
export class AppTopbar {
  protected readonly layout = inject(LayoutState);
  protected readonly theme = inject(ThemeService);
  protected readonly text = messages.shell;
  protected readonly auth = inject(AuthStore);
  protected readonly notifications = inject(NotificationsStore);
  protected readonly notificationText = messages.notifications;
  protected readonly userText = messages.auth.user;
  protected readonly roles = messages.roles;
  protected readonly icons = {
    menu: MenuIcon,
    search: SearchIcon,
    bell: BellIcon,
    logout: LogOutIcon,
  };
  protected readonly notificationsOpen = signal(false);
  protected readonly userMenuOpen = signal(false);

  protected readonly themeIcon = computed(() => themeIcons[this.theme.mode()]);
  protected readonly themeLabel = computed(() =>
    this.text.theme.change(this.text.theme[this.theme.mode()], this.text.theme[this.theme.next()]),
  );

  private readonly search = viewChild.required<ElementRef<HTMLInputElement>>('search');
  private readonly bell = viewChild.required('bell', { read: ElementRef<HTMLButtonElement> });
  private readonly account = viewChild.required<ElementRef<HTMLButtonElement>>('account');

  protected toggleNotifications(): void {
    this.userMenuOpen.set(false);
    this.notificationsOpen.update((open) => !open);
    if (this.notificationsOpen()) {
      void this.notifications.load();
    }
  }

  protected toggleUserMenu(): void {
    this.notificationsOpen.set(false);
    this.userMenuOpen.update((open) => !open);
  }

  protected logout(): void {
    this.userMenuOpen.set(false);
    this.auth.logout();
  }

  protected onDocumentKeydown(event: KeyboardEvent): void {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      this.search().nativeElement.focus();
      return;
    }

    if (event.key === 'Escape' && this.notificationsOpen()) {
      this.notificationsOpen.set(false);
      this.bell().nativeElement.focus();
    }

    if (event.key === 'Escape' && this.userMenuOpen()) {
      this.userMenuOpen.set(false);
      this.account().nativeElement.focus();
    }
  }
}
