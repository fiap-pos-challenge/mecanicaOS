import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import {
  LayoutDashboard as LayoutDashboardIcon,
  Menu as MenuIcon,
  Plus as PlusIcon,
  SquareKanban as SquareKanbanIcon,
  Wrench as WrenchIcon,
} from 'lucide';
import { UiIcon } from '../../shared/ui/icon/icon';
import { messages } from '../../core/i18n/messages.pt-BR';
import { AuthStore } from '../../features/auth/state/auth.store';
import { LayoutState } from '../layout-state';

@Component({
  selector: 'app-bottom-nav',
  imports: [RouterLink, RouterLinkActive, UiIcon],
  templateUrl: './bottom-nav.html',
  styleUrl: './bottom-nav.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppBottomNav {
  protected readonly layout = inject(LayoutState);
  protected readonly text = messages.bottomNav;
  private readonly auth = inject(AuthStore);
  private readonly allLinks = [
    {
      path: '/dashboard',
      label: messages.bottomNav.dashboard,
      icon: LayoutDashboardIcon,
      primary: false,
      permission: 'dashboard.view',
    },
    {
      path: '/service-orders',
      label: messages.bottomNav.serviceOrders,
      icon: SquareKanbanIcon,
      primary: false,
      permission: 'serviceOrders.view',
    },
    {
      path: '/quotes',
      label: messages.bottomNav.quote,
      icon: PlusIcon,
      primary: true,
      permission: 'quotes.manage',
    },
    {
      path: '/mechanic',
      label: messages.bottomNav.mechanic,
      icon: WrenchIcon,
      primary: false,
      permission: 'mechanic.view',
    },
  ] as const;
  protected readonly links = computed(() => {
    this.auth.role();
    return this.allLinks.filter((link) => this.auth.can(link.permission));
  });
  protected readonly menuIcon = MenuIcon;
}
