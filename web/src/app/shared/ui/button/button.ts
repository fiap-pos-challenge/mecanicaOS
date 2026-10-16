import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  inject,
  input,
} from '@angular/core';
import { messages } from '../../../core/i18n/messages.pt-BR';

export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger';
export type ButtonSize = 'sm' | 'md';

@Component({
  selector: 'button[uiButton], a[uiButton]',
  templateUrl: './button.html',
  styleUrl: './button.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'classes()',
    '[attr.aria-busy]': 'loading() || null',
    '[attr.aria-disabled]': 'loading() || null',
    '[attr.disabled]': '(disabled() || loading()) && isButton ? "" : null',
    '(click)': 'onClick($event)',
  },
})
export class UiButton {
  readonly variant = input<ButtonVariant>('secondary');
  readonly size = input<ButtonSize>('md');
  readonly loading = input(false);
  readonly disabled = input(false);

  protected readonly isButton = inject(ElementRef).nativeElement instanceof HTMLButtonElement;

  protected readonly loadingLabel = messages.ui.loading;
  protected readonly classes = computed(
    () =>
      `ui-button ui-button--${this.variant()} ui-button--${this.size()}${this.loading() ? ' ui-button--loading' : ''}`,
  );

  protected onClick(event: Event): void {
    if (this.loading()) {
      event.preventDefault();
      event.stopImmediatePropagation();
    }
  }
}
