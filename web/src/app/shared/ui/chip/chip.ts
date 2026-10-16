import { ChangeDetectionStrategy, Component, model } from '@angular/core';

@Component({
  selector: 'button[uiChip]',
  template: '<ng-content />',
  styleUrl: './chip.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'ui-chip',
    type: 'button',
    '[attr.aria-pressed]': 'selected()',
    '[class.ui-chip--selected]': 'selected()',
    '(click)': 'selected.set(!selected())',
  },
})
export class UiChip {
  readonly selected = model(false);
}
