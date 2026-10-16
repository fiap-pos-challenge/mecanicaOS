export type ChartKind = 'column' | 'line' | 'donut' | 'table';

export type ChartTone = 'accent' | 'amber' | 'ok' | 'bad' | 'violet' | 'muted';

export const CHART_TONES: readonly ChartTone[] = [
  'accent',
  'amber',
  'violet',
  'ok',
  'bad',
  'muted',
];

export interface ChartSeries {
  readonly name: string;
  readonly tone: ChartTone;
  readonly values: readonly number[];
}

export interface ChartData {
  readonly categories: readonly string[];
  readonly series: readonly ChartSeries[];
}

export interface ChartPalette {
  readonly fg: string;
  readonly muted: string;
  readonly line: string;
  readonly surface: string;
  readonly tones: Readonly<Record<ChartTone, string>>;
}

export type ChartOption = Readonly<Record<string, unknown>>;

export type ValueFormat = (value: number) => string;

const TOKEN_OF: Readonly<Record<keyof Omit<ChartPalette, 'tones'> | ChartTone, string>> = {
  fg: '--fg',
  muted: '--muted',
  line: '--line',
  surface: '--surface',
  accent: '--accent',
  amber: '--amber',
  ok: '--ok',
  bad: '--bad',
  violet: '--violet',
};

export function readPalette(read: (token: string) => string): ChartPalette {
  const value = (key: keyof typeof TOKEN_OF) => read(TOKEN_OF[key]).trim();
  return {
    fg: value('fg'),
    muted: value('muted'),
    line: value('line'),
    surface: value('surface'),
    tones: {
      accent: value('accent'),
      amber: value('amber'),
      ok: value('ok'),
      bad: value('bad'),
      violet: value('violet'),
      muted: value('muted'),
    },
  };
}

export function hasChartData(data: ChartData): boolean {
  return (
    data.categories.length > 0 && data.series.some((series) => series.values.some((v) => v !== 0))
  );
}

export function describeChart(label: string, data: ChartData, format: ValueFormat): string {
  const parts = data.categories.map((category, index) => {
    const values = data.series.map((series) =>
      data.series.length > 1
        ? `${series.name} ${format(series.values[index] ?? 0)}`
        : format(series.values[index] ?? 0),
    );
    return `${category}: ${values.join(', ')}`;
  });
  return [label, ...parts].join('; ');
}

function axisOption(palette: ChartPalette, format: ValueFormat, categories: readonly string[]) {
  return {
    grid: { left: 8, right: 16, top: 32, bottom: 8, containLabel: true },
    xAxis: {
      type: 'category',
      data: [...categories],
      axisLine: { lineStyle: { color: palette.line } },
      axisLabel: { color: palette.muted, hideOverlap: true },
      axisTick: { show: false },
    },
    yAxis: {
      type: 'value',
      axisLabel: { color: palette.muted, formatter: format },
      splitLine: { lineStyle: { color: palette.line } },
    },
  };
}

export function buildChartOption(
  kind: Exclude<ChartKind, 'table'>,
  data: ChartData,
  palette: ChartPalette,
  format: ValueFormat,
  stacked = false,
): ChartOption {
  const common = {
    animation: false,
    aria: { enabled: false },
    textStyle: { color: palette.fg, fontFamily: 'IBM Plex Sans, sans-serif' },
    tooltip: {
      renderMode: 'richText',
      trigger: kind === 'donut' ? 'item' : 'axis',
      valueFormatter: format,
      backgroundColor: palette.surface,
      borderColor: palette.line,
      textStyle: { color: palette.fg },
    },
    legend: {
      show: kind === 'donut' || data.series.length > 1,
      top: 0,
      textStyle: { color: palette.muted },
    },
  };

  if (kind === 'donut') {
    const series = data.series[0];
    return {
      ...common,
      color: CHART_TONES.map((tone) => palette.tones[tone]),
      series: [
        {
          type: 'pie',
          name: series?.name ?? '',
          radius: ['48%', '72%'],
          center: ['50%', '58%'],
          avoidLabelOverlap: true,
          label: { color: palette.fg, formatter: '{b}' },
          itemStyle: { borderColor: palette.surface, borderWidth: 2 },
          data: data.categories.map((category, index) => ({
            name: category,
            value: series?.values[index] ?? 0,
          })),
        },
      ],
    };
  }

  return {
    ...common,
    ...axisOption(palette, format, data.categories),
    color: data.series.map((series) => palette.tones[series.tone]),
    series: data.series.map((series) => ({
      type: kind === 'column' ? 'bar' : 'line',
      name: series.name,
      data: [...series.values],
      ...(stacked ? { stack: 'total' } : {}),
      ...(kind === 'line' ? { symbol: 'circle', symbolSize: 6, lineStyle: { width: 2 } } : {}),
      ...(kind === 'column' ? { barMaxWidth: 32 } : {}),
    })),
  };
}
