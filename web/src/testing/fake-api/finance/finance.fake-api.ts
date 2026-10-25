import { messages } from '../../../app/core/i18n/messages.pt-BR';
import { FakeBackendState, FakeFeatureApi } from '../fake-api/fake-backend';
import { fieldError, FakeRouter, ok, validation } from '../fake-api/fake-http';
import { authorize, isPrincipal } from '../fake-api/fake-session';
import { FakeBoardOrder } from '../service-orders/service-orders.seed';

export const FINANCE_PATHS = {
  summary: '/api/admin/finance',
  platform: '/api/admin/platform',
} as const;

const DAYS: Readonly<Record<string, number>> = { today: 1, week: 7, month: 30, quarter: 90 };
const DAY_MS = 24 * 60 * 60 * 1000;
const COMMISSION_RATE = 0.1;

function cents(value: number): number {
  return Math.round(value * 100);
}

export function createFinanceFakeApi(state: FakeBackendState): FakeFeatureApi {
  const orders = (): readonly FakeBoardOrder[] => state.database.serviceOrders.orders;
  const customerName = (order: FakeBoardOrder): string =>
    state.database.customers.find((customer) => customer.id === order.customerId)?.name ?? '';
  const total = (order: FakeBoardOrder): number =>
    Math.max(
      0,
      order.quoteItems.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0) -
        order.discount,
    );
  const services = (order: FakeBoardOrder): number =>
    order.quoteItems
      .filter((item) => item.kind === 'Service')
      .reduce((sum, item) => sum + item.unitPrice * item.quantity, 0);
  const method = (order: FakeBoardOrder): string =>
    state.database.portal.paidOrders.some((paid) => paid.orderId === order.id) ? 'Pix' : 'Balcão';

  const router = new FakeRouter()
    .on('GET', FINANCE_PATHS.summary, (request) => {
      const principal = authorize(request, state.database.users, ['Admin', 'Manager']);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const period = request.query.get('period') ?? 'month';
      const days = DAYS[period];
      if (days === undefined) {
        return validation([fieldError('period', 'finance.period', messages.fake.invalidPeriod)]);
      }

      const since = Date.now() - days * DAY_MS;
      const delivered = orders().filter(
        (order) => order.status === 'Delivered' && Date.parse(order.updatedAt) >= since,
      );
      const waiting = orders().filter((order) => order.status === 'Completed');
      const received = delivered.reduce((sum, order) => sum + total(order), 0);
      const byMechanic = new Map<string, { orders: number; services: number }>();
      for (const order of delivered) {
        const name = order.mechanicName ?? messages.serviceOrders.unassigned;
        const current = byMechanic.get(name) ?? { orders: 0, services: 0 };
        byMechanic.set(name, {
          orders: current.orders + 1,
          services: current.services + services(order),
        });
      }

      const methods = new Map<string, { amount: number; count: number }>();
      for (const order of delivered) {
        const current = methods.get(method(order)) ?? { amount: 0, count: 0 };
        methods.set(method(order), {
          amount: current.amount + total(order),
          count: current.count + 1,
        });
      }

      const commissions = [...byMechanic.entries()].map(([mechanicName, value]) => ({
        mechanicName,
        orders: value.orders,
        services: cents(value.services),
        commission: cents(value.services * COMMISSION_RATE),
      }));
      const today = new Date().toISOString().slice(0, 10);
      return ok({
        period,
        from: new Date(since).toISOString().slice(0, 10),
        to: today,
        kpis: {
          received: cents(received),
          receivable: cents(waiting.reduce((sum, order) => sum + total(order), 0)),
          receivableCount: waiting.length,
          commissions: commissions.reduce((sum, item) => sum + item.commission, 0),
          averageTicket: delivered.length === 0 ? 0 : cents(received / delivered.length),
          orders: delivered.length,
        },
        byMethod: [...methods.entries()].map(([name, value]) => ({
          method: name,
          amount: cents(value.amount),
          count: value.count,
        })),
        receipts: delivered.map((order) => ({
          serviceOrderId: order.id,
          number: order.number,
          customerName: customerName(order),
          deliveredAt: order.updatedAt,
          method: method(order),
          amount: cents(total(order)),
        })),
        receivables: waiting.map((order) => ({
          serviceOrderId: order.id,
          number: order.number,
          customerName: customerName(order),
          completedAt: order.updatedAt,
          amount: cents(total(order)),
          daysWaiting: Math.max(0, Math.floor((Date.now() - Date.parse(order.updatedAt)) / DAY_MS)),
        })),
        commissions,
      });
    })
    .on('GET', FINANCE_PATHS.platform, (request) => {
      const principal = authorize(request, state.database.users, ['Admin']);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const workshop = state.database.settings.workshop;
      const month = new Date().toISOString().slice(0, 7);
      return ok({
        shopName: workshop.name,
        plan: {
          name: workshop.plan.name,
          renewsAt: workshop.plan.renewsAt,
          users: state.database.users.filter((user) => user.isActive).length,
          maxUsers: workshop.plan.maxUsers,
        },
        usage: {
          customers: state.database.customers.length,
          vehicles: state.database.vehicles.length,
          serviceOrdersThisMonth: orders().filter((order) =>
            order.history[0]?.changedAt.startsWith(month),
          ).length,
          documentsThisMonth: orders()
            .flatMap((order) => order.documents)
            .filter((document) => document.issuedAt.startsWith(month)).length,
          activeAutomations: state.database.automations.rules.filter((rule) => rule.enabled).length,
        },
        modules: [
          { id: 'serviceOrders', name: 'Ordens de serviço', enabled: true },
          { id: 'quotes', name: 'Orçamentos e aprovação', enabled: true },
          { id: 'portal', name: 'Portal do cliente', enabled: true },
          { id: 'inventory', name: 'Estoque e compras', enabled: true },
          { id: 'reports', name: 'Relatórios e assistente', enabled: true },
          { id: 'automations', name: 'Automações', enabled: true },
          { id: 'finance', name: 'Financeiro', enabled: true },
          { id: 'fiscal', name: 'Notas fiscais', enabled: false },
        ],
      });
    });

  return new FakeFeatureApi('finance', router, state.scenarios);
}
