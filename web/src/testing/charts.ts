import { Provider } from '@angular/core';
import { CHART_RENDERER, ChartInstance } from '../app/shared/ui/chart/chart-renderer';
import { ChartOption } from '../app/shared/ui/chart/chart.models';

export class FakeChart implements ChartInstance {
  readonly options: ChartOption[] = [];
  resized = 0;
  disposed = false;

  constructor(readonly element: HTMLElement) {}

  setOption(option: ChartOption): void {
    this.options.push(option);
  }

  resize(): void {
    this.resized += 1;
  }

  dispose(): void {
    this.disposed = true;
  }
}

export const fakeCharts: FakeChart[] = [];

export function provideFakeCharts(): Provider {
  return {
    provide: CHART_RENDERER,
    useValue: (element: HTMLElement) => {
      const chart = new FakeChart(element);
      fakeCharts.push(chart);
      return Promise.resolve(chart);
    },
  };
}
