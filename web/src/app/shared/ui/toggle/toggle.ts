import { ChangeDetectionStrategy, Component, model } from '@angular/core';

@Component({
  selector: 'button[uiToggle]',
  template: '<span class="visually-hidden"><ng-content /></span>',
  styleUrl: './toggle.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'ui-toggle',
    type: 'button',
    role: 'switch',
    '[attr.aria-checked]': 'checked()',
    '[class.ui-toggle--on]': 'checked()',
    '(click)': 'checked.set(!checked())',
  },
})
export class UiToggle {
  readonly checked = model(false);
}
