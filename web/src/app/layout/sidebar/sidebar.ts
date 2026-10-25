import { CdkTrapFocus } from '@angular/cdk/a11y';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import {
  PanelLeftClose as PanelLeftCloseIcon,
  PanelLeftOpen as PanelLeftOpenIcon,
  X as XIcon,
} from 'lucide';
import { UiIcon } from '../../shared/ui/icon/icon';
import { messages } from '../../core/i18n/messages.pt-BR';
import { UiIconButton } from '../../shared/ui/icon-button/icon-button';
import { LayoutState } from '../layout-state';
import { AuthStore } from '../../features/auth/state/auth.store';
import { sectionsFor } from '../navigation';

@Component({
  selector: 'app-sidebar',
  imports: [CdkTrapFocus, RouterLink, RouterLinkActive, UiIcon, UiIconButton],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(document:keydown.escape)': 'onEscape()',
  },
})
export class AppSidebar {
  protected readonly layout = inject(LayoutState);
  protected readonly auth = inject(AuthStore);
  protected readonly sections = computed(() => {
    this.auth.role();
    return sectionsFor((permission) => this.auth.can(permission));
  });
  protected readonly text = messages.shell;
  protected readonly icons = {
    close: XIcon,
    collapse: PanelLeftCloseIcon,
    expand: PanelLeftOpenIcon,
  };

  protected readonly compact = computed(() => this.layout.collapsed() && !this.layout.isMobile());
  protected readonly visible = computed(() => !this.layout.isMobile() || this.layout.drawerOpen());

  protected onEscape(): void {
    if (this.layout.isMobile() && this.layout.drawerOpen()) {
      this.close();
    }
  }

  protected close(): void {
    this.layout.closeDrawer();
  }
}
