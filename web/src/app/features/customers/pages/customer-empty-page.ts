import { ChangeDetectionStrategy, Component } from '@angular/core';
import { UserRound as UserRoundIcon } from 'lucide';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiCard } from '../../../shared/ui/card/card';
import { UiEmptyState } from '../../../shared/ui/empty-state/empty-state';

@Component({
  selector: 'app-customer-empty-page',
  imports: [UiCard, UiEmptyState],
  template: `
    <ui-card>
      <ui-empty-state
        [icon]="icon"
        [title]="text.selectTitle"
        [description]="text.selectHint"
        [headingLevel]="2"
      />
    </ui-card>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CustomerEmptyPage {
  protected readonly text = messages.customers.list;
  protected readonly icon = UserRoundIcon;
}
