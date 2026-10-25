import { messages } from '../../../app/core/i18n/messages.pt-BR';
import {
  allowedGroupings,
  ReportDefinition,
  ReportGrouping,
  ReportMeasure,
  ReportPeriod,
} from '../../../app/features/reports/data/report.models';
import { FakeBackendState, FakeFeatureApi } from '../fake-api/fake-backend';
import { FakeRouter, ok, problem, readBody, readText } from '../fake-api/fake-http';
import { authorize, FakeRole, isPrincipal } from '../fake-api/fake-session';

export const ASSISTANT_PATH = '/api/admin/assistant/report-definitions';

const viewers: readonly FakeRole[] = ['Admin', 'Manager'];

const MONTHS = [
  'janeiro',
  'fevereiro',
  'março',
  'abril',
  'maio',
  'junho',
  'julho',
  'agosto',
  'setembro',
  'outubro',
  'novembro',
  'dezembro',
];

const MEASURES: readonly [RegExp, ReportMeasure][] = [
  [/ticket/, 'averageTicket'],
  [/margem/, 'margin'],
  [/comiss/, 'commission'],
  [/peça|peca/, 'partsOut'],
  [/quant|quantas|número de os|numero de os/, 'orderCount'],
  [/fatur|vend|receita/, 'revenue'],
];

const GROUPINGS: readonly [RegExp, ReportGrouping][] = [
  [/mecânic|mecanic/, 'mechanic'],
  [/forma de pagamento|pagamento/, 'paymentMethod'],
  [/categoria/, 'category'],
  [/serviço|servico/, 'service'],
  [/cliente/, 'customer'],
  [/semana/, 'week'],
  [/mês|mes|mensal/, 'month'],
];

export interface FakeAssistant {
  questions: string[];
}

export function createAssistantSeed(): FakeAssistant {
  return { questions: [] };
}

function pad(value: number): string {
  return String(value).padStart(2, '0');
}

export function interpretQuestion(
  question: string,
  today = new Date(),
): { readonly definition: ReportDefinition; readonly explanation: string } | null {
  const text = question.toLowerCase();
  const measure = MEASURES.find(([pattern]) => pattern.test(text))?.[1];
  if (!measure) {
    return null;
  }

  const wanted = GROUPINGS.find(([pattern]) => pattern.test(text))?.[1] ?? 'month';
  const groupBy = allowedGroupings(measure).includes(wanted) ? wanted : 'month';
  const year = today.getUTCFullYear();
  const month = MONTHS.findIndex((name) => text.includes(name));
  let period: ReportPeriod = { preset: 'yearToDate', from: null, to: null };
  if (month >= 0) {
    const last = new Date(Date.UTC(year, month + 1, 0)).getUTCDate();
    period = {
      preset: 'custom',
      from: `${year}-${pad(month + 1)}-01`,
      to: `${year}-${pad(month + 1)}-${pad(last)}`,
    };
  } else if (/mês passado|mes passado/.test(text)) {
    period = { preset: 'lastMonth', from: null, to: null };
  } else if (/trimestre/.test(text)) {
    period = { preset: 'lastQuarter', from: null, to: null };
  } else if (/este mês|esse mês|este mes/.test(text)) {
    period = { preset: 'thisMonth', from: null, to: null };
  }

  const overTime = groupBy === 'month' || groupBy === 'week';
  const additive = !['averageTicket', 'margin'].includes(measure);
  const chart = overTime
    ? 'line'
    : additive && /parte|divid|particip/.test(text)
      ? 'donut'
      : 'column';
  const labels = messages.reports;
  return {
    definition: { measure, groupBy, period, chart },
    explanation: `${labels.measures[measure]} por ${labels.groupings[groupBy].toLowerCase()}, ${labels.periods[period.preset].toLowerCase()}.`,
  };
}

export function createAssistantFakeApi(state: FakeBackendState): FakeFeatureApi {
  const router = new FakeRouter().on('POST', ASSISTANT_PATH, (request) => {
    const principal = authorize(request, state.database.users, viewers);
    if (!isPrincipal(principal)) {
      return principal;
    }

    const question = readText(readBody(request), 'question');
    state.database.assistant.questions.push(question);
    const answer = interpretQuestion(question);
    return answer
      ? ok(answer)
      : problem(422, 'assistant.notUnderstood', messages.fake.notUnderstood);
  });

  return new FakeFeatureApi('assistant', router, state.scenarios);
}
