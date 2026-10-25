import { DOCUMENT, NgTemplateOutlet } from '@angular/common';
import {
  afterRenderEffect,
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  effect,
  ElementRef,
  inject,
  input,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { ThemeService } from '../../../core/theme/theme.service';
import { CHART_RENDERER, ChartInstance } from './chart-renderer';
import {
  buildChartOption,
  ChartData,
  ChartKind,
  ChartOption,
  describeChart,
  hasChartData,
  readPalette,
  ValueFormat,
} from './chart.models';

@Component({
  selector: 'ui-chart',
  imports: [NgTemplateOutlet],
  templateUrl: './chart.html',
  styleUrl: './chart.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiChart {
  private readonly document = inject(DOCUMENT);
  private readonly renderer = inject(CHART_RENDERER);
  private readonly canvas = viewChild<ElementRef<HTMLElement>>('canvas');
  private instance: ChartInstance | null = null;
  private element: HTMLElement | null = null;
  private drawing: Promise<void> = Promise.resolve();

  readonly kind = input.required<ChartKind>();
  readonly data = input.required<ChartData>();
  readonly label = input.required<string>();
  readonly format = input<ValueFormat>((value) => value.toLocaleString('pt-BR'));
  readonly stacked = input(false);
  readonly emptyMessage = input<string>(messages.ui.chart.empty);

  protected readonly text = messages.ui.chart;
  protected readonly failed = signal(false);
  protected readonly palette = signal(this.currentPalette());
  protected readonly empty = computed(() => !hasChartData(this.data()));
  protected readonly summary = computed(() =>
    describeChart(this.label(), this.data(), this.format()),
  );
  protected readonly rows = computed(() =>
    this.data().categories.map((category, index) => ({
      category,
      values: this.data().series.map((series) => this.format()(series.values[index] ?? 0)),
    })),
  );
  private readonly option = computed<ChartOption | null>(() => {
    const kind = this.kind();
    if (kind === 'table' || this.empty()) {
      return null;
    }

    return buildChartOption(kind, this.data(), this.palette(), this.format(), this.stacked());
  });

  constructor() {
    const theme = inject(ThemeService);
    effect(() => {
      theme.mode();
      untracked(() => this.palette.set(this.currentPalette()));
    });
    const media = this.document.defaultView?.matchMedia?.('(prefers-color-scheme: dark)');
    const onScheme = () => this.palette.set(this.currentPalette());
    media?.addEventListener('change', onScheme);
    const resize = () => this.instance?.resize();
    this.document.defaultView?.addEventListener('resize', resize);
    inject(DestroyRef).onDestroy(() => {
      media?.removeEventListener('change', onScheme);
      this.document.defaultView?.removeEventListener('resize', resize);
      this.release();
    });
    afterRenderEffect(() => {
      const option = this.option();
      const element = this.canvas()?.nativeElement ?? null;
      this.drawing = this.drawing.then(() => this.draw(element, option));
    });
  }

  whenDrawn(): Promise<void> {
    return this.drawing;
  }

  private async draw(element: HTMLElement | null, option: ChartOption | null): Promise<void> {
    if (element !== this.element) {
      this.release();
    }

    if (!element || !option) {
      return;
    }

    try {
      this.instance ??= await this.renderer(element);
      this.element = element;
      this.instance.setOption(option, true);
      this.failed.set(false);
    } catch {
      this.release();
      this.failed.set(true);
    }
  }

  private release(): void {
    this.instance?.dispose();
    this.instance = null;
    this.element = null;
  }

  private currentPalette() {
    const root = this.document.documentElement;
    const styles = this.document.defaultView?.getComputedStyle(root);
    return readPalette((token) => styles?.getPropertyValue(token) ?? '');
  }
}
