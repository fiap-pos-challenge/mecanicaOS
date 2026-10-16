import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Construction as ConstructionIcon } from 'lucide';
import { messages } from '../../core/i18n/messages.pt-BR';
import { UiCard } from '../../shared/ui/card/card';
import { UiEmptyState } from '../../shared/ui/empty-state/empty-state';

@Component({
  selector: 'app-under-construction-page',
  imports: [UiCard, UiEmptyState],
  template: `
    <h1 class="page-title">{{ screen() }}</h1>
    <ui-card>
      <ui-empty-state [icon]="icon" [title]="text.title" [description]="text.description" />
    </ui-card>
  `,
  styleUrl: './page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnderConstructionPage {
  readonly screen = input.required<string>();

  protected readonly text = messages.pages.underConstruction;
  protected readonly icon = ConstructionIcon;
}
