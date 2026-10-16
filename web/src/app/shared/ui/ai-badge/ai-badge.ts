import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Sparkles as SparklesIcon } from 'lucide';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiIcon } from '../icon/icon';

@Component({
  selector: 'ui-ai-badge',
  imports: [UiIcon],
  template: `
    <ui-icon [icon]="icon" [size]="14" />
    <span>{{ label() }}</span>
  `,
  styles: `
    :host {
      display: inline-flex;
      gap: var(--space-1);
      align-items: center;
      padding: 2px var(--space-2);
      border: 1px solid var(--violet);
      border-radius: 999px;
      background: var(--violet-soft);
      color: var(--violet);
      font-size: var(--text-xs);
      font-weight: 600;
      white-space: nowrap;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { 'data-ai': 'true' },
})
export class UiAiBadge {
  readonly label = input<string>(messages.ui.ai.badge);
  protected readonly icon = SparklesIcon;
}
