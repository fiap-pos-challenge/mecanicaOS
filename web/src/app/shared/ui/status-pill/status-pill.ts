import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type StatusTone = 'ok' | 'warn' | 'bad' | 'info' | 'violet' | 'neutral';

@Component({
  selector: 'ui-status-pill',
  template: '<ng-content />',
  styleUrl: './status-pill.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class]': '"ui-status-pill ui-status-pill--" + tone()' },
})
export class UiStatusPill {
  readonly tone = input<StatusTone>('neutral');
}
