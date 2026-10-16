import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Inbox as InboxIcon } from 'lucide';
import type { IconNode } from 'lucide';
import { UiIcon } from '../icon/icon';

@Component({
  selector: 'ui-empty-state',
  imports: [UiIcon],
  templateUrl: './empty-state.html',
  styleUrl: './empty-state.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiEmptyState {
  readonly title = input.required<string>();
  readonly description = input<string | null>(null);
  readonly icon = input<IconNode>(InboxIcon);
  readonly headingLevel = input<2 | 3>(2);
}
