import { messages } from '../../../app/core/i18n/messages.pt-BR';
import { NOTIFICATION_TYPES } from '../../../app/features/notifications/data/notification.models';
import { FakeBackendState, FakeFeatureApi } from '../fake-api/fake-backend';
import {
  fieldError,
  FakeRouter,
  noContent,
  ok,
  problem,
  readBody,
  validation,
} from '../fake-api/fake-http';
import { fakeId } from '../fake-api/fake-ids';
import { authorize, FAKE_ROLES, isPrincipal } from '../fake-api/fake-session';
import { FakeRequest } from '../fake-request';

export const NOTIFICATION_PATHS = {
  list: '/api/admin/notifications',
  readAll: '/api/admin/notifications/read-all',
  read: '/api/admin/notifications/:id/read',
  preferences: '/api/admin/notifications/preferences',
} as const;

export interface FakeNotification {
  id: string;
  type: string;
  title: string;
  body: string;
  createdAt: string;
  link: string | null;
  aiSuggested: boolean;
}

export interface FakePreference {
  type: string;
  inApp: boolean;
  email: boolean;
  whatsApp: boolean;
}

export interface FakeNotifications {
  items: FakeNotification[];
  readBy: Record<string, string[]>;
  preferences: Record<string, FakePreference[]>;
}

export function createNotificationsSeed(): FakeNotifications {
  return {
    readBy: {},
    preferences: {},
    items: [
      {
        id: 'notification-payment',
        type: 'PaymentConfirmed',
        title: 'Pagamento da OS 3324 confirmado',
        body: 'Pix recebido pelo portal do cliente.',
        createdAt: '2026-10-16T09:12:00Z',
        link: '/service-orders',
        aiSuggested: false,
      },
      {
        id: 'notification-stock',
        type: 'StockLow',
        title: 'Reposição sugerida: pastilha de freio',
        body: 'A previsão indica falta em 4 dias. Confira e aprove o pedido.',
        createdAt: '2026-10-16T08:40:00Z',
        link: '/inventory',
        aiSuggested: true,
      },
      {
        id: 'notification-automation',
        type: 'AutomationFailed',
        title: 'Falha ao enviar WhatsApp da OS 3318',
        body: 'O item está na fila de erros.',
        createdAt: '2026-10-16T08:10:00Z',
        link: '/automations',
        aiSuggested: false,
      },
    ],
  };
}

export function pushFakeNotification(
  state: FakeBackendState,
  notification: Omit<FakeNotification, 'id' | 'createdAt'>,
): FakeNotification {
  const item = { ...notification, id: fakeId(), createdAt: new Date().toISOString() };
  state.database.notifications.items.unshift(item);
  return item;
}

export function createNotificationsFakeApi(state: FakeBackendState): FakeFeatureApi {
  const db = (): FakeNotifications => state.database.notifications;
  const guard = (request: FakeRequest) => authorize(request, state.database.users, FAKE_ROLES);
  const readList = (userId: string) => (db().readBy[userId] ??= []);

  const router = new FakeRouter()
    .on('GET', NOTIFICATION_PATHS.list, (request) => {
      const principal = guard(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const read = readList(principal.id);
      return ok({ items: db().items.map((item) => ({ ...item, read: read.includes(item.id) })) });
    })
    .on('POST', NOTIFICATION_PATHS.readAll, (request) => {
      const principal = guard(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      db().readBy[principal.id] = db().items.map((item) => item.id);
      return noContent();
    })
    .on('POST', NOTIFICATION_PATHS.read, (request, params) => {
      const principal = guard(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const item = db().items.find((candidate) => candidate.id === params['id']);
      if (!item) {
        return problem(404, 'notifications.notFound', messages.errors.notFound);
      }

      const read = readList(principal.id);
      if (!read.includes(item.id)) {
        read.push(item.id);
      }

      return noContent();
    })
    .on('GET', NOTIFICATION_PATHS.preferences, (request) => {
      const principal = guard(request);
      return isPrincipal(principal)
        ? ok({ items: db().preferences[principal.id] ?? [] })
        : principal;
    })
    .on('PUT', NOTIFICATION_PATHS.preferences, (request) => {
      const principal = guard(request);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const items = readBody(request)['items'];
      const valid =
        Array.isArray(items) &&
        items.every((item: unknown) => {
          const record = item as Readonly<Record<string, unknown>>;
          return (
            typeof item === 'object' &&
            item !== null &&
            NOTIFICATION_TYPES.some((type) => type === record['type']) &&
            ['inApp', 'email', 'whatsApp'].every((key) => typeof record[key] === 'boolean')
          );
        });
      if (!valid) {
        return validation([
          fieldError('items', 'notifications.preferences', messages.errors.validation),
        ]);
      }

      db().preferences[principal.id] = items as FakePreference[];
      return ok({ items: db().preferences[principal.id] });
    });

  return new FakeFeatureApi('notifications', router, state.scenarios);
}
