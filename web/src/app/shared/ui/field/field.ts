import {
  ChangeDetectionStrategy,
  Component,
  computed,
  Directive,
  inject,
  input,
} from '@angular/core';

let nextFieldId = 0;

@Component({
  selector: 'ui-field',
  templateUrl: './field.html',
  styleUrl: './field.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiField {
  readonly label = input.required<string>();
  readonly hint = input<string | null>(null);
  readonly error = input<string | null>(null);
  readonly required = input(false);

  readonly controlId = `ui-field-${++nextFieldId}`;
  readonly hintId = `${this.controlId}-hint`;
  readonly errorId = `${this.controlId}-error`;

  readonly describedBy = computed(() => {
    if (this.error()) {
      return this.errorId;
    }

    return this.hint() ? this.hintId : null;
  });
}

@Directive({
  selector: 'input[uiControl], select[uiControl], textarea[uiControl]',
  host: {
    class: 'ui-control',
    '[id]': 'field.controlId',
    '[attr.aria-describedby]': 'field.describedBy()',
    '[attr.aria-invalid]': 'field.error() ? "true" : null',
    '[attr.aria-required]': 'field.required() || null',
  },
})
export class UiControl {
  protected readonly field = inject(UiField);
}
