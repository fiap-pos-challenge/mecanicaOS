import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'button[uiIconButton]',
  template: '<ng-content />',
  styleUrl: './icon-button.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'ui-icon-button',
    type: 'button',
    '[attr.aria-label]': 'label()',
    '[attr.title]': 'label()',
    '[class.ui-icon-button--outlined]': 'outlined()',
  },
})
export class UiIconButton {
  readonly label = input.required<string>();
  readonly outlined = input(false);
}
