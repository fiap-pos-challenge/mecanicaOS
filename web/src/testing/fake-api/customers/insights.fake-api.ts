import { FakeBackendState, FakeFeatureApi } from '../fake-api/fake-backend';
import { fieldError, FakeRouter, ok, readBody, validation } from '../fake-api/fake-http';
import { authorize, isPrincipal } from '../fake-api/fake-session';
import { messages } from '../../../app/core/i18n/messages.pt-BR';

export const INSIGHT_PATHS = {
  summaries: '/api/admin/customers/summaries',
  history: '/api/admin/customers/:id/service-history',
} as const;

const DAY_MS = 24 * 60 * 60 * 1000;
const services = [
  'Revisão 40 mil km',
  'Troca de óleo e filtros',
  'Kit freio dianteiro',
  'Alinhamento e balanceamento',
  'Diagnóstico de injeção',
  'Troca da correia dentada',
];
const payments = ['Pix', 'Cartão de crédito', 'Cartão de débito', 'Dinheiro'];

export interface FakeServiceHistoryEntry {
  readonly number: number;
  readonly description: string;
  readonly total: number;
  readonly status: 'Delivered' | 'InProgress';
  readonly openedAt: string;
  readonly closedAt: string | null;
  readonly paymentMethod: string | null;
}

export function seedOf(id: string): number {
  let hash = 2166136261;
  for (const character of id) {
    hash = Math.imul(hash ^ character.charCodeAt(0), 16777619) >>> 0;
  }

  return hash;
}

export function serviceHistoryOf(id: string, now: number = Date.now()): FakeServiceHistoryEntry[] {
  const seed = seedOf(id);
  const count = seed % 5;
  const open = seed % 7 === 0;
  const recent = seed % 3 !== 0;
  return Array.from({ length: count }, (_, index) => {
    const value = seedOf(`${id}:${index}`);
    const daysAgo = (recent ? 10 : 120) + index * 47 + (value % 30);
    const openedAt = new Date(now - daysAgo * DAY_MS).toISOString();
    const isOpen = open && index === 0;
    return {
      number: 3000 + (value % 900),
      description: services[value % services.length] ?? 'Serviço',
      total: 150 + (value % 2400) + (value % 100) / 100,
      status: isOpen ? 'InProgress' : 'Delivered',
      openedAt,
      closedAt: isOpen ? null : openedAt,
      paymentMethod: isOpen ? null : (payments[value % payments.length] ?? 'Pix'),
    };
  });
}

export function createInsightsFakeApi(state: FakeBackendState): FakeFeatureApi {
  const users = (): FakeBackendState['database']['users'] => state.database.users;

  const summaryOf = (id: string): Record<string, unknown> => {
    const history = serviceHistoryOf(id);
    const delivered = history.filter((entry) => entry.status === 'Delivered');
    const totalSpent =
      Math.round(delivered.reduce((sum, entry) => sum + entry.total, 0) * 100) / 100;
    const lastVisitAt = history[0]?.openedAt ?? null;
    const vehicleCount = state.database.vehicles.filter(
      (vehicle) => vehicle.customerId === id,
    ).length;
    const openServiceOrders = history.length - delivered.length;
    const daysSinceVisit = lastVisitAt ? (Date.now() - Date.parse(lastVisitAt)) / DAY_MS : Infinity;
    return {
      customerId: id,
      vehicleCount,
      lastVisitAt,
      totalSpent,
      averageTicket:
        delivered.length > 0 ? Math.round((totalSpent / delivered.length) * 100) / 100 : 0,
      serviceOrderCount: history.length,
      openServiceOrders,
      situation:
        openServiceOrders > 0 ? 'OpenServiceOrder' : daysSinceVisit > 90 ? 'NoReturn' : 'Active',
      fleet: vehicleCount >= 3,
    };
  };

  const router = new FakeRouter()
    .on('POST', INSIGHT_PATHS.summaries, (request) => {
      const principal = authorize(request, users(), ['Admin', 'Manager', 'Attendant']);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const ids = readBody(request)['customerIds'];
      if (!Array.isArray(ids) || ids.length > 500 || ids.some((id) => typeof id !== 'string')) {
        return validation([
          fieldError('customerIds', 'common.validationFailed', messages.fake.invalidField),
        ]);
      }

      return ok({ items: (ids as string[]).map(summaryOf) });
    })
    .on('GET', INSIGHT_PATHS.history, (request, params) => {
      const principal = authorize(request, users(), ['Admin', 'Manager', 'Attendant']);
      if (!isPrincipal(principal)) {
        return principal;
      }

      return ok({ items: serviceHistoryOf(params['id'] ?? '') });
    });

  return new FakeFeatureApi('insights', router, state.scenarios);
}
