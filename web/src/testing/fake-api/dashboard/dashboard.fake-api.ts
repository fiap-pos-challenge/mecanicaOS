import { FakeBackendState, FakeFeatureApi } from '../fake-api/fake-backend';
import {
  FakeRouter,
  noContent,
  ok,
  problem,
  readBody,
  readText,
  validation,
  fieldError,
} from '../fake-api/fake-http';
import { authorize, FakeRole, isPrincipal } from '../fake-api/fake-session';
import { FakeRequest } from '../fake-request';
import { messages } from '../../../app/core/i18n/messages.pt-BR';

export const DASHBOARD_PATHS = {
  snapshot: '/api/admin/dashboard',
  layout: '/api/admin/dashboard/layout',
  action: '/api/admin/dashboard/attention/:id/actions',
} as const;

export const FAKE_DASHBOARD_CARDS = ['kpis', 'dailyRevenue', 'attention', 'topServices', 'yard'];

export interface FakeCardSetting {
  id: string;
  visible: boolean;
}

export interface FakeAttention {
  id: string;
  kind: 'LowStock' | 'QuoteWithoutAnswer' | 'LateOrder' | 'MaintenanceDue';
  severity: 'bad' | 'warn' | 'info';
  title: string;
  detail: string;
  action: 'CreatePurchaseOrder' | 'SendNewDeadline' | 'SendReminder' | null;
  target: Readonly<Record<string, string>> | null;
  aiSuggested: boolean;
}

export interface FakeDashboard {
  layouts: Record<string, FakeCardSetting[]>;
  attention: FakeAttention[];
  actions: { itemId: string; action: string }[];
}

const viewers: readonly FakeRole[] = ['Admin', 'Manager', 'Attendant'];

const DAYS: Readonly<Record<string, number>> = { today: 1, week: 7, month: 30 };

const SCALE: Readonly<Record<string, number>> = { today: 1, week: 7, month: 30 };

export function createDashboardSeed(lateOrderId: string | null): FakeDashboard {
  return {
    layouts: {},
    actions: [],
    attention: [
      {
        id: 'attention-stock',
        kind: 'LowStock',
        severity: 'bad',
        title: 'Pastilha de freio dianteira',
        detail: '2 un. · mínimo 8 · 3 OS dependem dela',
        action: 'CreatePurchaseOrder',
        target: { kind: 'inventory' },
        aiSuggested: true,
      },
      {
        id: 'attention-quote',
        kind: 'QuoteWithoutAnswer',
        severity: 'warn',
        title: 'Orçamento nº 1042 sem resposta',
        detail: 'Enviado há 3 dias · lembrete automático amanhã às 9h',
        action: 'SendReminder',
        target: null,
        aiSuggested: false,
      },
      {
        id: 'attention-late',
        kind: 'LateOrder',
        severity: 'warn',
        title: 'OS nº 3318 passou do prazo',
        detail: 'Honda Civic · prometida para hoje cedo · avisar o cliente?',
        action: 'SendNewDeadline',
        target: lateOrderId ? { kind: 'serviceOrder', id: lateOrderId } : null,
        aiSuggested: false,
      },
      {
        id: 'attention-maintenance',
        kind: 'MaintenanceDue',
        severity: 'info',
        title: '6 clientes com revisão vencendo',
        detail: 'Previsão pelo km médio rodado de cada veículo',
        action: null,
        target: { kind: 'customers' },
        aiSuggested: true,
      },
    ],
  };
}

function day(offset: number): string {
  const date = new Date();
  date.setUTCHours(12, 0, 0, 0);
  date.setUTCDate(date.getUTCDate() - offset);
  return date.toISOString().slice(0, 10);
}

export function createDashboardFakeApi(state: FakeBackendState): FakeFeatureApi {
  const db = (): FakeDashboard => state.database.dashboard;
  const guard = (request: FakeRequest) => authorize(request, state.database.users, viewers);
  const router = new FakeRouter()
    .on('GET', DASHBOARD_PATHS.snapshot, (request) => {
      const principal = guard(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const period = request.query.get('period') ?? 'today';
      const days = DAYS[period];
      const scale = SCALE[period];
      if (days === undefined || scale === undefined) {
        return validation([fieldError('period', 'dashboard.period', messages.fake.invalidPeriod)]);
      }

      const orders = state.database.serviceOrders.orders;
      const statuses = ['Received', 'InDiagnosis', 'WaitingApproval', 'InProgress', 'Completed'];
      return ok({
        kpis: [
          { id: 'revenue', value: 291_400 * scale, previous: 259_200 * scale, detail: null },
          {
            id: 'openOrders',
            value: orders.filter((order) => order.status !== 'Delivered').length,
            previous: null,
            detail: orders.filter((order) => order.tags.includes('MissingPart')).length,
          },
          { id: 'approvalRate', value: 72, previous: 68, detail: null },
          { id: 'receivable', value: 638_000, previous: null, detail: 2 },
        ],
        dailyRevenue: Array.from({ length: days }, (_, index) => ({
          date: day(days - 1 - index),
          services: 180_000 + ((index * 37_000) % 120_000),
          parts: 90_000 + ((index * 23_000) % 80_000),
        })),
        topServices: [
          { name: 'Troca de óleo', quantity: 9 * scale },
          { name: 'Alinhamento 3D', quantity: 7 * scale },
          { name: 'Diagnóstico eletrônico', quantity: 5 * scale },
          { name: 'Freio dianteiro', quantity: 4 * scale },
          { name: 'Revisão 40 mil km', quantity: 2 * scale },
        ],
        yard: statuses.map((status) => ({
          status,
          count: orders.filter((order) => order.status === status).length,
        })),
        attention: db().attention,
      });
    })
    .on('GET', DASHBOARD_PATHS.layout, (request) => {
      const principal = guard(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      return ok({ cards: db().layouts[principal.id] ?? [] });
    })
    .on('PUT', DASHBOARD_PATHS.layout, (request) => {
      const principal = guard(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const cards = readBody(request)['cards'];
      const valid =
        Array.isArray(cards) &&
        cards.every(
          (card: unknown) =>
            typeof card === 'object' &&
            card !== null &&
            FAKE_DASHBOARD_CARDS.includes(readText(card as Record<string, unknown>, 'id')) &&
            typeof (card as Record<string, unknown>)['visible'] === 'boolean',
        );
      if (!valid) {
        return validation([fieldError('cards', 'dashboard.layout', messages.fake.invalidLayout)]);
      }

      db().layouts[principal.id] = (cards as FakeCardSetting[]).map((card) => ({
        id: card.id,
        visible: card.visible,
      }));
      return ok({ cards: db().layouts[principal.id] });
    })
    .on('POST', DASHBOARD_PATHS.action, (request, params) => {
      const principal = guard(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const item = db().attention.find((candidate) => candidate.id === params['id']);
      const action = readText(readBody(request), 'action');
      if (!item) {
        return problem(404, 'dashboard.attentionNotFound', messages.errors.notFound);
      }

      if (item.action !== action) {
        return problem(422, 'dashboard.actionNotAllowed', messages.fake.actionNotAllowed);
      }

      db().attention = db().attention.filter((candidate) => candidate.id !== item.id);
      db().actions.push({ itemId: item.id, action });
      return noContent();
    });

  return new FakeFeatureApi('dashboard', router, state.scenarios);
}
