import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Construction as ConstructionIcon } from 'lucide';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiIcon } from '../icon/icon';

@Component({
  selector: 'ui-pending-notice',
  imports: [UiIcon],
  template: `
    <ui-icon class="icon" [icon]="icon" [size]="20" />
    <div>
      <p class="title">{{ title() }}</p>
      <p class="body">{{ message() }}</p>
    </div>
  `,
  styleUrl: './pending-notice.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { role: 'status', 'data-testid': 'pending-notice' },
})
export class UiPendingNotice {
  readonly title = input<string>(messages.errors.featurePendingTitle);
  readonly message = input<string>(messages.errors.featurePending);

  protected readonly icon = ConstructionIcon;
}
