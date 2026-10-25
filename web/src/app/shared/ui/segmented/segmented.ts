import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  input,
  model,
  viewChildren,
} from '@angular/core';

export interface SegmentedOption<T extends string> {
  readonly value: T;
  readonly label: string;
}

@Component({
  selector: 'ui-segmented',
  templateUrl: './segmented.html',
  styleUrl: './segmented.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiSegmented<T extends string> {
  readonly options = input.required<readonly SegmentedOption<T>[]>();
  readonly label = input.required<string>();
  readonly value = model.required<T>();

  private readonly radios = viewChildren<ElementRef<HTMLButtonElement>>('radio');

  protected select(value: T): void {
    this.value.set(value);
  }

  protected onKeydown(event: KeyboardEvent): void {
    const step = { ArrowRight: 1, ArrowDown: 1, ArrowLeft: -1, ArrowUp: -1 }[event.key];
    if (step === undefined) {
      return;
    }

    event.preventDefault();
    const options = this.options();
    const current = options.findIndex((option) => option.value === this.value());
    const next = (current + step + options.length) % options.length;
    const option = options[next];
    if (option) {
      this.select(option.value);
      this.radios()[next]?.nativeElement.focus();
    }
  }
}
