import { TestBed } from '@angular/core/testing';
import { render, screen, within } from '@testing-library/angular';
import { FakeChart, fakeCharts, provideFakeCharts } from '../../../../testing/charts';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { ThemeService } from '../../../core/theme/theme.service';
import { UiChart } from './chart';
import { CHART_RENDERER, clearOnlyContainer } from './chart-renderer';
import {
  buildChartOption,
  ChartData,
  ChartKind,
  ChartPalette,
  ChartSeries,
  describeChart,
  hasChartData,
  readPalette,
} from './chart.models';

const services: ChartSeries = { name: 'Serviços', tone: 'accent', values: [10, 20] };

const data: ChartData = {
  categories: ['Jan', 'Fev'],
  series: [services, { name: 'Peças', tone: 'amber', values: [5, 0] }],
};

const palette: ChartPalette = readPalette((token) => `var(${token})`);

describe('chart models', () => {
  it('reads every token of the palette', () => {
    expect(palette.tones.violet).toBe('var(--violet)');
    expect(palette.line).toBe('var(--line)');
  });

  it('builds stacked columns, lines and a donut with the token colors', () => {
    const columns = buildChartOption('column', data, palette, String, true);
    expect(columns['color']).toEqual(['var(--accent)', 'var(--amber)']);
    expect(columns['series']).toEqual([
      expect.objectContaining({ type: 'bar', stack: 'total', data: [10, 20] }),
      expect.objectContaining({ type: 'bar', stack: 'total', data: [5, 0] }),
    ]);
    expect(columns['tooltip']).toEqual(expect.objectContaining({ renderMode: 'richText' }));

    const line = buildChartOption('line', { ...data, series: [services] }, palette, String);
    expect(line['legend']).toEqual(expect.objectContaining({ show: false }));
    expect(line['series']).toEqual([expect.objectContaining({ type: 'line', symbol: 'circle' })]);

    const donut = buildChartOption('donut', data, palette, String);
    expect(donut['series']).toEqual([
      expect.objectContaining({
        type: 'pie',
        data: [
          { name: 'Jan', value: 10 },
          { name: 'Fev', value: 20 },
        ],
      }),
    ]);
    expect(
      buildChartOption('donut', { categories: [], series: [] }, palette, String)['series'],
    ).toEqual([expect.objectContaining({ name: '', data: [] })]);
  });

  it('describes the data and detects empty charts', () => {
    expect(describeChart('Faturamento', data, String)).toBe(
      'Faturamento; Jan: Serviços 10, Peças 5; Fev: Serviços 20, Peças 0',
    );
    expect(
      describeChart('Uma', { categories: ['A'], series: [{ ...services, values: [] }] }, String),
    ).toBe('Uma; A: 0');
    expect(hasChartData(data)).toBe(true);
    expect(hasChartData({ categories: ['A'], series: [{ ...services, values: [0] }] })).toBe(false);
  });
});

describe('UiChart', () => {
  beforeEach(() => fakeCharts.splice(0));

  async function show(inputs: Partial<{ kind: ChartKind; data: ChartData }> = {}) {
    const view = await render(UiChart, {
      providers: [provideFakeCharts()],
      inputs: { kind: 'column', data, label: 'Faturamento', ...inputs },
    });
    await view.fixture.componentInstance.whenDrawn();
    return view;
  }

  it('draws the chart, keeps a data table and redraws on theme and size changes', async () => {
    const view = await show();
    const chart = fakeCharts[0] as FakeChart;
    expect(chart.options).toHaveLength(1);
    expect(screen.getByRole('img').getAttribute('aria-label')).toContain('Jan: Serviços 10');
    expect(screen.getByText(messages.ui.chart.showTable)).toBeTruthy();
    expect(within(screen.getByRole('table')).getAllByRole('row')).toHaveLength(3);

    TestBed.inject(ThemeService).set('dark');
    view.fixture.detectChanges();
    await view.fixture.componentInstance.whenDrawn();
    window.dispatchEvent(new Event('resize'));
    expect(chart.resized).toBe(1);

    view.fixture.componentRef.setInput('kind', 'table');
    view.fixture.detectChanges();
    await view.fixture.componentInstance.whenDrawn();
    expect(chart.disposed).toBe(true);
    expect(screen.queryByRole('img')).toBeNull();
    expect(screen.getByRole('table')).toBeTruthy();
    view.fixture.destroy();
  });

  it('shows the empty message when every value is zero', async () => {
    await show({ data: { categories: ['A'], series: [{ ...services, values: [0] }] } });
    expect(screen.getByRole('status').textContent).toContain(messages.ui.chart.empty);
    expect(fakeCharts).toHaveLength(0);
  });

  it('falls back to the table when the renderer fails', async () => {
    const view = await render(UiChart, {
      providers: [{ provide: CHART_RENDERER, useValue: () => Promise.reject(new Error('no')) }],
      inputs: { kind: 'line', data, label: 'Faturamento' },
    });
    await view.fixture.componentInstance.whenDrawn();
    view.fixture.detectChanges();
    expect(await screen.findByRole('table')).toBeTruthy();
    expect(screen.queryByRole('img')).toBeNull();
  });
});

describe('clearOnlyContainer', () => {
  it('lets the chart library clear its container and nothing else', () => {
    const element = clearOnlyContainer(document.createElement('div'));
    element.append(document.createElement('canvas'));

    Reflect.set(element, 'innerHTML', '');
    expect(element.childElementCount).toBe(0);
    expect(Reflect.get(element, 'innerHTML')).toBe('');
    expect(() => Reflect.set(element, 'innerHTML', '<img src=x>')).toThrow(TypeError);
  });
});
