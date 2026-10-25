import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { messages } from '../../../core/i18n/messages.pt-BR';

@Component({
  selector: 'ui-plate',
  template: '<span class="visually-hidden">{{ label }} </span>{{ normalized() }}',
  styleUrl: './plate.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiPlate {
  readonly value = input.required<string>();

  protected readonly label = messages.ui.plate;
  protected readonly normalized = computed(() =>
    this.value()
      .replace(/[^a-z0-9]/gi, '')
      .toUpperCase(),
  );
}
