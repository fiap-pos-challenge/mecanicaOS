import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export interface SparklineGeometry {
  readonly path: string;
  readonly last: { readonly x: number; readonly y: number } | null;
}

const PADDING = 4;

export function sparklineGeometry(
  values: readonly number[],
  width: number,
  height: number,
): SparklineGeometry {
  if (values.length === 0) {
    return { path: '', last: null };
  }

  const min = Math.min(...values);
  const range = Math.max(...values) - min || 1;
  const step = values.length > 1 ? (width - PADDING * 2) / (values.length - 1) : 0;
  const points = values.map((value, index) => ({
    x: Math.round((PADDING + index * step) * 10) / 10,
    y: Math.round((height - PADDING - ((value - min) / range) * (height - PADDING * 2)) * 10) / 10,
  }));
  return {
    path: points.map((point, index) => `${index ? 'L' : 'M'}${point.x},${point.y}`).join(''),
    last: points[points.length - 1] ?? null,
  };
}

@Component({
  selector: 'ui-sparkline',
  template: `
    <svg
      [attr.viewBox]="'0 0 ' + width() + ' ' + height()"
      [attr.width]="width()"
      [attr.height]="height()"
      role="img"
      [attr.aria-label]="description()"
      focusable="false"
    >
      <path class="line" [attr.d]="geometry().path" />
      @if (geometry().last; as last) {
        <circle class="dot" [attr.cx]="last.x" [attr.cy]="last.y" r="2.6" />
      }
    </svg>
  `,
  styleUrl: './sparkline.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiSparkline {
  readonly values = input.required<readonly number[]>();
  readonly label = input.required<string>();
  readonly width = input(110);
  readonly height = input(28);

  protected readonly geometry = computed(() =>
    sparklineGeometry(this.values(), this.width(), this.height()),
  );
  protected readonly description = computed(() => `${this.label()}: ${this.values().join(', ')}`);
}
