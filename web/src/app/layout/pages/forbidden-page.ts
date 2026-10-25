import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthStore } from '../../features/auth/state/auth.store';
import { ShieldX as ShieldXIcon } from 'lucide';
import { messages } from '../../core/i18n/messages.pt-BR';
import { UiButton } from '../../shared/ui/button/button';
import { UiCard } from '../../shared/ui/card/card';
import { UiEmptyState } from '../../shared/ui/empty-state/empty-state';

@Component({
  selector: 'app-forbidden-page',
  imports: [RouterLink, UiButton, UiCard, UiEmptyState],
  template: `
    <h1 class="page-title">{{ text.title }}</h1>
    <ui-card>
      <ui-empty-state [icon]="icon" [title]="text.title" [description]="text.description">
        <a uiButton variant="primary" [routerLink]="auth.home()">{{ home }}</a>
      </ui-empty-state>
    </ui-card>
  `,
  styleUrl: './page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ForbiddenPage {
  protected readonly text = messages.pages.forbidden;
  protected readonly auth = inject(AuthStore);
  protected readonly home = messages.shell.home;
  protected readonly icon = ShieldXIcon;
}
