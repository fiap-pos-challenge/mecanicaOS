import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import type { IconNode } from 'lucide';

type IconElement = IconNode[number];

@Component({
  selector: 'ui-icon',
  templateUrl: './icon.html',
  styleUrl: './icon.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiIcon {
  readonly icon = input.required<IconNode>();
  readonly size = input(18);
  readonly strokeWidth = input(2);

  protected attr(element: IconElement, name: string): string | number | null {
    return element[1][name] ?? null;
  }
}
