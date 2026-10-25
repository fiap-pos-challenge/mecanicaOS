import { messages } from '../../../app/core/i18n/messages.pt-BR';
import {
  MAX_RECIPIENTS,
  MEASURE_UNITS,
  readDefinition,
  REPORT_WEEKDAYS,
  ReportDefinition,
  ReportGrouping,
  ReportMeasure,
  validateDefinition,
} from '../../../app/features/reports/data/report.models';
import { isValidEmail } from '../../../app/shared/util/email';
import { FakeBackendState, FakeFeatureApi } from '../fake-api/fake-backend';
import {
  accepted,
  created,
  fieldError,
  FakeRouter,
  noContent,
  ok,
  problem,
  readBody,
  readText,
  validation,
} from '../fake-api/fake-http';
import { fakeId, fakeNow } from '../fake-api/fake-ids';
import { authorize, FakeRole, isPrincipal } from '../fake-api/fake-session';
import { FakeRequest } from '../fake-request';

export const REPORT_PATHS = {
  run: '/api/admin/reports/run',
  saved: '/api/admin/reports/saved',
  savedItem: '/api/admin/reports/saved/:id',
  schedule: '/api/admin/reports/saved/:id/schedule',
  exports: '/api/admin/reports/exports',
  exportItem: '/api/admin/reports/exports/:id',
} as const;

export const FAKE_REPORT_FILES = { xlsx: '/reports/exemplo.xlsx', pdf: '/reports/exemplo.pdf' };

export const FAKE_DATA_START = '2026-01-01';

export interface FakeSchedule {
  weekday: string;
  time: string;
  recipients: string[];
  format: 'xlsx' | 'pdf';
}

export interface FakeSavedReport {
  id: string;
  name: string;
  definition: ReportDefinition;
  schedule: FakeSchedule | null;
  updatedAt: string;
}

export interface FakeExportJob {
  id: string;
  format: 'xlsx' | 'pdf';
  status: 'Queued' | 'Processing' | 'Ready' | 'Failed';
  large: boolean;
  polls: number;
}

export interface FakeReports {
  saved: FakeSavedReport[];
  exports: FakeExportJob[];
  runs: ReportDefinition[];
}

const viewers: readonly FakeRole[] = ['Admin', 'Manager'];

const LABELS: Readonly<Record<Exclude<ReportGrouping, 'month' | 'week'>, readonly string[]>> = {
  mechanic: ['Marcos Mecânico', 'Pedro Convidado', 'Rita Inativa'],
  service: ['Troca de óleo', 'Alinhamento 3D', 'Diagnóstico eletrônico', 'Freio dianteiro'],
  category: ['Motor', 'Freios', 'Suspensão', 'Elétrica'],
  paymentMethod: ['Pix', 'Cartão de crédito', 'Cartão de débito', 'Dinheiro', 'Boleto'],
  customer: ['Joana Prado', 'Carlos Mendes', 'Transportes Rota Sul Ltda'],
};

const BASE: Readonly<Record<ReportMeasure, number>> = {
  revenue: 4_500_000,
  orderCount: 42,
  partsOut: 120,
  averageTicket: 62_000,
  margin: 38,
  commission: 450_000,
};

const MONTHS = ['jan', 'fev', 'mar', 'abr', 'mai', 'jun', 'jul', 'ago', 'set', 'out', 'nov', 'dez'];

const DAY_MS = 24 * 60 * 60 * 1000;

export function createReportsSeed(): FakeReports {
  return {
    exports: [],
    runs: [],
    saved: [
      {
        id: fakeId(),
        name: 'Faturamento por mês',
        definition: {
          measure: 'revenue',
          groupBy: 'month',
          period: { preset: 'yearToDate', from: null, to: null },
          chart: 'column',
        },
        schedule: {
          weekday: 'Monday',
          time: '08:00',
          recipients: ['dono@oficina.local'],
          format: 'pdf',
        },
        updatedAt: '2026-09-30T12:00:00Z',
      },
      {
        id: fakeId(),
        name: 'Ticket médio por mecânico',
        definition: {
          measure: 'averageTicket',
          groupBy: 'mechanic',
          period: { preset: 'lastQuarter', from: null, to: null },
          chart: 'table',
        },
        schedule: null,
        updatedAt: '2026-09-15T12:00:00Z',
      },
    ],
  };
}

function iso(date: Date): string {
  return date.toISOString().slice(0, 10);
}

export function periodRange(definition: ReportDefinition, today = new Date()): [string, string] {
  const year = today.getUTCFullYear();
  const month = today.getUTCMonth();
  const utc = (y: number, m: number, d: number) => new Date(Date.UTC(y, m, d));
  switch (definition.period.preset) {
    case 'thisMonth':
      return [iso(utc(year, month, 1)), iso(today)];
    case 'lastMonth':
      return [iso(utc(year, month - 1, 1)), iso(utc(year, month, 0))];
    case 'lastQuarter':
      return [iso(utc(year, month - 3, 1)), iso(utc(year, month, 0))];
    case 'yearToDate':
      return [iso(utc(year, 0, 1)), iso(today)];
    case 'custom':
      return [definition.period.from ?? iso(today), definition.period.to ?? iso(today)];
  }
}

function timeLabels(groupBy: 'month' | 'week', from: string, to: string): string[] {
  const start = Date.parse(`${from}T00:00:00Z`);
  const end = Date.parse(`${to}T00:00:00Z`);
  const labels: string[] = [];
  if (groupBy === 'month') {
    const cursor = new Date(start);
    cursor.setUTCDate(1);
    while (cursor.getTime() <= end) {
      labels.push(`${MONTHS[cursor.getUTCMonth()]}/${String(cursor.getUTCFullYear()).slice(2)}`);
      cursor.setUTCMonth(cursor.getUTCMonth() + 1);
    }

    return labels;
  }

  for (let time = start; time <= end; time += 7 * DAY_MS) {
    const date = new Date(time);
    labels.push(
      `Sem. ${String(date.getUTCDate()).padStart(2, '0')}/${String(date.getUTCMonth() + 1).padStart(2, '0')}`,
    );
  }

  return labels.slice(-12);
}

export function runReport(definition: ReportDefinition, today = new Date()) {
  const [requestedFrom, to] = periodRange(definition, today);
  const from = requestedFrom < FAKE_DATA_START ? FAKE_DATA_START : requestedFrom;
  const unit = MEASURE_UNITS[definition.measure];
  if (from > to || to < FAKE_DATA_START || from > iso(today)) {
    return { rows: [], unit, total: null, from: requestedFrom, to };
  }

  const days = Math.max(1, (Date.parse(to) - Date.parse(from)) / DAY_MS + 1);
  const group = definition.groupBy;
  const labels =
    group === 'month' || group === 'week' ? timeLabels(group, from, to) : LABELS[group];
  const share = unit === 'percent' || definition.measure === 'averageTicket' ? 1 : days / 30;
  const rows = labels.map((label, index) => ({
    label,
    value: Math.round(BASE[definition.measure] * share * (1 + ((index * 7) % 5) / 10)),
  }));
  const additive = !['averageTicket', 'margin'].includes(definition.measure);
  return {
    rows,
    unit,
    total: additive ? rows.reduce((sum, row) => sum + row.value, 0) : null,
    from: requestedFrom,
    to,
  };
}

function scheduleProblems(value: unknown) {
  if (value === null) {
    return [];
  }

  const schedule = (typeof value === 'object' ? value : {}) as Readonly<Record<string, unknown>>;
  const recipients = Array.isArray(schedule['recipients']) ? schedule['recipients'] : [];
  const errors = [];
  if (!REPORT_WEEKDAYS.some((day) => day === schedule['weekday'])) {
    errors.push(
      fieldError('weekday', 'reports.weekday', messages.reports.schedule.weekdayRequired),
    );
  }

  if (!/^([01]\d|2[0-3]):[0-5]\d$/.test(readText(schedule, 'time'))) {
    errors.push(fieldError('time', 'reports.time', messages.reports.schedule.timeInvalid));
  }

  if (
    recipients.length === 0 ||
    recipients.length > MAX_RECIPIENTS ||
    !recipients.every((item: unknown) => typeof item === 'string' && isValidEmail(item))
  ) {
    errors.push(
      fieldError('recipients', 'reports.recipients', messages.reports.schedule.recipientsInvalid),
    );
  }

  if (!['xlsx', 'pdf'].includes(readText(schedule, 'format'))) {
    errors.push(fieldError('format', 'reports.format', messages.reports.schedule.formatRequired));
  }

  return errors;
}

export function createReportsFakeApi(state: FakeBackendState): FakeFeatureApi {
  const db = (): FakeReports => state.database.reports;
  const guard = (request: FakeRequest) => authorize(request, state.database.users, viewers);
  const definitionOf = (request: FakeRequest) => readDefinition(readBody(request)['definition']);
  const incompatible = (definition: ReportDefinition | null) => {
    if (!definition) {
      return validation([
        fieldError('definition', 'reports.definition', messages.reports.problems.invalid),
      ]);
    }

    const problems = validateDefinition(definition);
    return problems.length > 0
      ? problem(422, 'reports.incompatible', messages.reports.problems[problems[0] ?? 'invalid'])
      : null;
  };
  const nameProblem = (name: string, id: string | null) => {
    if (name.length < 3 || name.length > 80) {
      return validation([fieldError('name', 'reports.name', messages.reports.save.nameInvalid)]);
    }

    return db().saved.some(
      (item) => item.id !== id && item.name.toLowerCase() === name.toLowerCase(),
    )
      ? problem(409, 'reports.duplicateName', messages.reports.save.duplicate)
      : null;
  };
  const view = (item: FakeSavedReport) => ({ ...item });
  const job = (item: FakeExportJob) => ({
    id: item.id,
    format: item.format,
    status: item.status,
    large: item.large,
    fileUrl: item.status === 'Ready' ? FAKE_REPORT_FILES[item.format] : null,
    fileName: item.status === 'Ready' ? `relatorio.${item.format}` : null,
  });
  const notFound = () => problem(404, 'reports.notFound', messages.errors.notFound);

  const router = new FakeRouter()
    .on('POST', REPORT_PATHS.run, (request) => {
      const principal = guard(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const definition = definitionOf(request);
      const failure = incompatible(definition);
      if (failure || !definition) {
        return failure ?? notFound();
      }

      db().runs.push(definition);
      return ok(runReport(definition));
    })
    .on('GET', REPORT_PATHS.saved, (request) => {
      const principal = guard(request);
      return isPrincipal(principal) ? ok({ items: db().saved.map(view) }) : principal;
    })
    .on('POST', REPORT_PATHS.saved, (request) => {
      const principal = guard(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const definition = definitionOf(request);
      const name = readText(readBody(request), 'name').trim();
      const failure = incompatible(definition) ?? nameProblem(name, null);
      if (failure || !definition) {
        return failure ?? notFound();
      }

      const saved: FakeSavedReport = {
        id: fakeId(),
        name,
        definition,
        schedule: null,
        updatedAt: fakeNow(),
      };
      db().saved.unshift(saved);
      return created(view(saved));
    })
    .on('PUT', REPORT_PATHS.savedItem, (request, params) => {
      const principal = guard(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const saved = db().saved.find((item) => item.id === params['id']);
      if (!saved) {
        return notFound();
      }

      const definition = definitionOf(request);
      const name = readText(readBody(request), 'name').trim();
      const failure = incompatible(definition) ?? nameProblem(name, saved.id);
      if (failure || !definition) {
        return failure ?? notFound();
      }

      Object.assign(saved, { name, definition, updatedAt: fakeNow() });
      return ok(view(saved));
    })
    .on('DELETE', REPORT_PATHS.savedItem, (request, params) => {
      const principal = guard(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const before = db().saved.length;
      db().saved = db().saved.filter((item) => item.id !== params['id']);
      return db().saved.length === before ? notFound() : noContent();
    })
    .on('PUT', REPORT_PATHS.schedule, (request, params) => {
      const principal = guard(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const saved = db().saved.find((item) => item.id === params['id']);
      if (!saved) {
        return notFound();
      }

      const value = readBody(request)['schedule'] ?? null;
      const errors = scheduleProblems(value);
      if (errors.length > 0) {
        return validation(errors);
      }

      saved.schedule = value as FakeSchedule | null;
      saved.updatedAt = fakeNow();
      return ok(view(saved));
    })
    .on('POST', REPORT_PATHS.exports, (request) => {
      const principal = guard(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const definition = definitionOf(request);
      const format = readText(readBody(request), 'format');
      const failure = incompatible(definition);
      if (failure || !definition) {
        return failure ?? notFound();
      }

      if (format !== 'xlsx' && format !== 'pdf') {
        return validation([
          fieldError('format', 'reports.format', messages.reports.schedule.formatRequired),
        ]);
      }

      const item: FakeExportJob = {
        id: fakeId(),
        format,
        status: 'Queued',
        large: definition.groupBy === 'customer',
        polls: 0,
      };
      db().exports.push(item);
      return accepted(job(item));
    })
    .on('GET', REPORT_PATHS.exportItem, (request, params) => {
      const principal = guard(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const item = db().exports.find((candidate) => candidate.id === params['id']);
      if (!item) {
        return notFound();
      }

      item.polls += 1;
      if (!item.large) {
        item.status = item.polls >= 2 ? 'Ready' : 'Processing';
      }

      return ok(job(item));
    });

  return new FakeFeatureApi('reports', router, state.scenarios);
}
