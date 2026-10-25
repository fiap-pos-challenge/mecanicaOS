import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'ui-card',
  templateUrl: './card.html',
  styleUrl: './card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiCard {
  readonly title = input<string | null>(null);
  readonly subtitle = input<string | null>(null);
  readonly headingLevel = input<2 | 3>(3);
}
