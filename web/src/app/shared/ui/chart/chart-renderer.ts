import { InjectionToken } from '@angular/core';
import { ChartOption } from './chart.models';

export interface ChartInstance {
  setOption(option: ChartOption, notMerge: boolean): void;
  resize(): void;
  dispose(): void;
}

export type ChartRenderer = (element: HTMLElement) => Promise<ChartInstance>;

export function clearOnlyContainer(element: HTMLElement): HTMLElement {
  Object.defineProperty(element, 'innerHTML', {
    configurable: true,
    get: () => '',
    set: (value: unknown) => {
      if (value !== '') {
        throw new TypeError('Chart containers only accept being cleared.');
      }

      element.replaceChildren();
    },
  });
  return element;
}

export const CHART_RENDERER = new InjectionToken<ChartRenderer>('CHART_RENDERER', {
  providedIn: 'root',
  factory: () => async (element) =>
    (await import('./echarts-setup')).createChart(clearOnlyContainer(element)),
});
