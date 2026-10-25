import { ChartKind } from '../../../shared/ui/chart/chart.models';

export type ReportMeasure =
  'revenue' | 'orderCount' | 'partsOut' | 'averageTicket' | 'margin' | 'commission';

export const REPORT_MEASURES: readonly ReportMeasure[] = [
  'revenue',
  'orderCount',
  'partsOut',
  'averageTicket',
  'margin',
  'commission',
];

export type ReportGrouping =
  'month' | 'week' | 'mechanic' | 'service' | 'category' | 'paymentMethod' | 'customer';

export const REPORT_GROUPINGS: readonly ReportGrouping[] = [
  'month',
  'week',
  'mechanic',
  'service',
  'category',
  'paymentMethod',
  'customer',
];

export type ReportPeriodPreset =
  'thisMonth' | 'lastMonth' | 'lastQuarter' | 'yearToDate' | 'custom';

export const REPORT_PERIODS: readonly ReportPeriodPreset[] = [
  'thisMonth',
  'lastMonth',
  'lastQuarter',
  'yearToDate',
  'custom',
];

export const REPORT_CHARTS: readonly ChartKind[] = ['column', 'line', 'donut', 'table'];

export interface ReportPeriod {
  readonly preset: ReportPeriodPreset;
  readonly from: string | null;
  readonly to: string | null;
}

export interface ReportDefinition {
  readonly measure: ReportMeasure;
  readonly groupBy: ReportGrouping;
  readonly period: ReportPeriod;
  readonly chart: ChartKind;
}

export type ReportUnit = 'money' | 'count' | 'percent';

export const MEASURE_UNITS: Readonly<Record<ReportMeasure, ReportUnit>> = {
  revenue: 'money',
  orderCount: 'count',
  partsOut: 'count',
  averageTicket: 'money',
  margin: 'percent',
  commission: 'money',
};

export type ReportProblem =
  | 'measureGrouping'
  | 'lineNeedsTime'
  | 'donutNeedsParts'
  | 'donutNotAdditive'
  | 'periodMissing'
  | 'periodOrder'
  | 'periodTooLong';

export const MAX_CUSTOM_DAYS = 366;

const TIME_GROUPINGS: readonly ReportGrouping[] = ['month', 'week'];

const ALLOWED_GROUPINGS: Readonly<Record<ReportMeasure, readonly ReportGrouping[]>> = {
  revenue: REPORT_GROUPINGS,
  orderCount: REPORT_GROUPINGS,
  averageTicket: REPORT_GROUPINGS,
  partsOut: ['month', 'week', 'mechanic', 'service', 'category', 'customer'],
  margin: ['month', 'week', 'mechanic', 'service', 'category', 'customer'],
  commission: ['month', 'week', 'mechanic', 'service'],
};

const ADDITIVE: readonly ReportMeasure[] = ['revenue', 'orderCount', 'partsOut', 'commission'];

const DATE = /^\d{4}-\d{2}-\d{2}$/;

const DAY_MS = 24 * 60 * 60 * 1000;

export function allowedGroupings(measure: ReportMeasure): readonly ReportGrouping[] {
  return ALLOWED_GROUPINGS[measure];
}

function isDate(value: string | null): value is string {
  return value !== null && DATE.test(value) && !Number.isNaN(Date.parse(`${value}T00:00:00Z`));
}

export function validateDefinition(definition: ReportDefinition): readonly ReportProblem[] {
  const problems: ReportProblem[] = [];
  if (!ALLOWED_GROUPINGS[definition.measure].includes(definition.groupBy)) {
    problems.push('measureGrouping');
  }

  const overTime = TIME_GROUPINGS.includes(definition.groupBy);
  if (definition.chart === 'line' && !overTime) {
    problems.push('lineNeedsTime');
  }

  if (definition.chart === 'donut' && overTime) {
    problems.push('donutNeedsParts');
  }

  if (definition.chart === 'donut' && !ADDITIVE.includes(definition.measure)) {
    problems.push('donutNotAdditive');
  }

  const { preset, from, to } = definition.period;
  if (preset === 'custom') {
    if (!isDate(from) || !isDate(to)) {
      problems.push('periodMissing');
    } else {
      const days = (Date.parse(`${to}T00:00:00Z`) - Date.parse(`${from}T00:00:00Z`)) / DAY_MS;
      if (days < 0) {
        problems.push('periodOrder');
      } else if (days + 1 > MAX_CUSTOM_DAYS) {
        problems.push('periodTooLong');
      }
    }
  }

  return problems;
}

export function defaultDefinition(): ReportDefinition {
  return {
    measure: 'revenue',
    groupBy: 'month',
    period: { preset: 'yearToDate', from: null, to: null },
    chart: 'column',
  };
}

export function sameDefinition(a: ReportDefinition, b: ReportDefinition): boolean {
  return (
    a.measure === b.measure &&
    a.groupBy === b.groupBy &&
    a.chart === b.chart &&
    a.period.preset === b.period.preset &&
    a.period.from === b.period.from &&
    a.period.to === b.period.to
  );
}

export function readDefinition(value: unknown): ReportDefinition | null {
  if (typeof value !== 'object' || value === null) {
    return null;
  }

  const record = value as Readonly<Record<string, unknown>>;
  const period = record['period'] as Readonly<Record<string, unknown>> | undefined;
  const measure = REPORT_MEASURES.find((item) => item === record['measure']);
  const groupBy = REPORT_GROUPINGS.find((item) => item === record['groupBy']);
  const chart = REPORT_CHARTS.find((item) => item === record['chart']);
  const preset = REPORT_PERIODS.find((item) => item === period?.['preset']);
  if (!measure || !groupBy || !chart || !preset) {
    return null;
  }

  const date = (key: 'from' | 'to'): string | null => {
    const raw = period?.[key];
    return typeof raw === 'string' ? raw : null;
  };
  return { measure, groupBy, chart, period: { preset, from: date('from'), to: date('to') } };
}

export interface ReportRow {
  readonly label: string;
  readonly value: number;
}

export interface ReportResult {
  readonly rows: readonly ReportRow[];
  readonly unit: ReportUnit;
  readonly total: number | null;
  readonly from: string;
  readonly to: string;
}

export type ReportWeekday =
  'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday' | 'Saturday' | 'Sunday';

export const REPORT_WEEKDAYS: readonly ReportWeekday[] = [
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
  'Sunday',
];

export type ExportFormat = 'xlsx' | 'pdf';

export interface ReportSchedule {
  readonly weekday: ReportWeekday;
  readonly time: string;
  readonly recipients: readonly string[];
  readonly format: ExportFormat;
}

export interface SavedReport {
  readonly id: string;
  readonly name: string;
  readonly definition: ReportDefinition;
  readonly schedule: ReportSchedule | null;
  readonly updatedAt: string;
}

export type ExportStatus = 'Queued' | 'Processing' | 'Ready' | 'Failed';

export interface ExportJob {
  readonly id: string;
  readonly format: ExportFormat;
  readonly status: ExportStatus;
  readonly large: boolean;
  readonly fileUrl: string | null;
  readonly fileName: string | null;
}

export const MAX_RECIPIENTS = 10;

export function parseRecipients(text: string): readonly string[] {
  return [
    ...new Set(
      text
        .split(/[\s,;]+/)
        .map((item) => item.trim().toLowerCase())
        .filter((item) => item.length > 0),
    ),
  ];
}
