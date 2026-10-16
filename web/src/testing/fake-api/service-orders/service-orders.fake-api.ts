import { messages } from '../../../app/core/i18n/messages.pt-BR';
import { FakeBackendState, FakeFeatureApi } from '../fake-api/fake-backend';
import {
  fieldError,
  FakeRouter,
  ok,
  problem,
  readBody,
  readText,
  validation,
} from '../fake-api/fake-http';
import { fakeId, fakeNow } from '../fake-api/fake-ids';
import { authorize, FakePrincipal, FakeRole, isPrincipal } from '../fake-api/fake-session';
import { FakeRequest, FakeResult } from '../fake-request';
import {
  FAKE_MECHANIC_NAME,
  FakeBoardOrder,
  FakeChecklistItem,
  FakeOrderStatus,
  FakeServiceOrders,
} from './service-orders.seed';

export const SERVICE_ORDER_PATHS = {
  collection: '/api/admin/service-orders',
  mine: '/api/admin/service-orders/mine',
  item: '/api/admin/service-orders/:id',
  transitions: '/api/admin/service-orders/:id/transitions',
  timeStart: '/api/admin/service-orders/:id/time-entries/start',
  timeStop: '/api/admin/service-orders/:id/time-entries/stop',
  work: '/api/admin/service-orders/:id/work',
  checklist: '/api/admin/service-orders/:id/checklist',
  photos: '/api/admin/service-orders/:id/photos',
  diagnosis: '/api/admin/service-orders/:id/diagnosis',
  suggestions: '/api/admin/service-orders/:id/diagnosis/suggestions',
  sendToQuote: '/api/admin/service-orders/:id/send-to-quote',
  startService: '/api/admin/service-orders/:id/start-service',
  finishService: '/api/admin/service-orders/:id/finish-service',
} as const;

const staff: readonly FakeRole[] = ['Admin', 'Manager', 'Attendant', 'Mechanic'];
const desk: readonly FakeRole[] = ['Admin', 'Manager', 'Attendant'];
const mechanics: readonly FakeRole[] = ['Admin', 'Manager', 'Mechanic'];
const PHOTO_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
export const MAX_PHOTO_DATA_URL = 7_000_000;
const text = messages.fake;

const ALLOWED: Readonly<Record<FakeOrderStatus, readonly FakeOrderStatus[]>> = {
  Received: ['InDiagnosis'],
  InDiagnosis: ['WaitingApproval'],
  WaitingApproval: ['InProgress', 'InDiagnosis'],
  InProgress: ['Completed'],
  Completed: ['Delivered'],
  Delivered: [],
};

const SUGGESTIONS: readonly {
  readonly keywords: RegExp;
  readonly name: string;
  readonly kind: 'Service' | 'Part';
  readonly confidence: number;
  readonly reason: string;
}[] = [
  {
    keywords: /bieleta|suspens|amortec|coxim/i,
    name: 'Alinhamento 3D',
    kind: 'Service',
    confidence: 0.82,
    reason: 'Folga na suspensão altera a geometria.',
  },
  {
    keywords: /freio|pastilha|disco/i,
    name: 'Pastilha de freio dianteira',
    kind: 'Part',
    confidence: 0.91,
    reason: 'Desgaste citado na descrição.',
  },
  {
    keywords: /óleo|oleo|vazamento/i,
    name: 'Troca de óleo',
    kind: 'Service',
    confidence: 0.77,
    reason: 'Nível ou vazamento de óleo.',
  },
  {
    keywords: /vela|falha|ignição|ignicao/i,
    name: 'Vela de ignição',
    kind: 'Part',
    confidence: 0.86,
    reason: 'Falha de ignição.',
  },
];

export function publicPhoto(photo: { id: string; url: string; caption: string; takenAt: string }): {
  id: string;
  url: string;
  caption: string;
  takenAt: string;
} {
  return { id: photo.id, url: photo.url, caption: photo.caption, takenAt: photo.takenAt };
}

export function moveOrder(
  order: FakeBoardOrder,
  to: FakeOrderStatus,
  changedBy: string,
  note: string | null,
): void {
  order.history.push({
    previousStatus: order.status,
    newStatus: to,
    changedBy,
    note,
    changedAt: fakeNow(),
  });
  order.status = to;
  order.version += 1;
  order.updatedAt = fakeNow();
  if (to === 'InProgress') {
    order.quoteStatus = 'Approved';
  }

  if (to === 'InDiagnosis' && note !== null) {
    order.quoteStatus = 'Rejected';
  }
}

export function createServiceOrdersFakeApi(state: FakeBackendState): FakeFeatureApi {
  const db = (): FakeServiceOrders => state.database.serviceOrders;
  const customerName = (order: FakeBoardOrder): string =>
    state.database.customers.find((customer) => customer.id === order.customerId)?.name ?? '';
  const nameOf = (principal: FakePrincipal): string =>
    state.database.users.find((user) => user.id === principal.id)?.name ??
    (principal.role === 'Mechanic' ? FAKE_MECHANIC_NAME : messages.roles[principal.role]);
  const guard = (request: FakeRequest, roles: readonly FakeRole[]): FakePrincipal | FakeResult =>
    authorize(request, state.database.users, roles);
  const find = (id: string | undefined): FakeBoardOrder | null =>
    db().orders.find((order) => order.id === id) ?? null;
  const notFound = () => problem(404, 'serviceOrders.notFound', text.serviceOrderNotFound);

  const subtotal = (order: FakeBoardOrder): number =>
    order.quoteItems.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0);

  const card = (order: FakeBoardOrder) => ({
    id: order.id,
    number: order.number,
    status: order.status,
    version: order.version,
    customerName: customerName(order),
    licensePlate: order.plate,
    vehicle: order.vehicle,
    summary: order.summary,
    mechanicName: order.mechanicName,
    promisedAt: order.promisedAt,
    total: Math.max(0, Math.round((subtotal(order) - order.discount) * 100) / 100),
    hasQuoteItems: order.quoteItems.length > 0,
    tags: [...order.tags],
    updatedAt: order.updatedAt,
  });

  const detail = (order: FakeBoardOrder) => ({
    ...card(order),
    problemDescription: order.complaint,
    mileage: order.mileage,
    statusHistory: order.history.map((entry) => ({ ...entry })),
    quote:
      order.quoteStatus === null
        ? null
        : {
            id: `${order.id}-quote`,
            number: order.number + 1000,
            version: 1,
            status: order.quoteStatus,
            total: card(order).total,
            items: order.quoteItems.map((item) => ({
              name: item.name,
              quantity: item.quantity,
              total: Math.round(item.unitPrice * item.quantity * 100) / 100,
            })),
          },
    parts: order.parts.map((part) => ({ ...part })),
    photos: order.photos.map(publicPhoto),
    timeEntries: order.timeEntries.map((entry) => ({
      ...entry,
      minutes: Math.round(
        (Date.parse(entry.endedAt ?? fakeNow()) - Date.parse(entry.startedAt)) / 60_000,
      ),
    })),
    documents: order.documents.map((document) => ({ ...document })),
  });

  const work = (order: FakeBoardOrder) => ({
    id: order.id,
    number: order.number,
    status: order.status,
    vehicle: order.vehicle,
    licensePlate: order.plate,
    complaint: order.complaint,
    promisedAt: order.promisedAt,
    checklist: order.checklist.map((item) => ({ ...item })),
    photos: order.photos.map(publicPhoto),
    diagnosis: order.diagnosis,
    sentToQuote: order.sentToQuote,
    timerRunning: order.timeEntries.some((entry) => entry.endedAt === null),
  });

  const running = (order: FakeBoardOrder) =>
    order.timeEntries.find((entry) => entry.endedAt === null) ?? null;

  const withOrder = (
    roles: readonly FakeRole[],
    handler: (order: FakeBoardOrder, principal: FakePrincipal, request: FakeRequest) => FakeResult,
  ) => {
    return (request: FakeRequest, params: Readonly<Record<string, string>>): FakeResult => {
      const principal = guard(request, roles);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const order = find(params['id']);
      return order ? handler(order, principal, request) : notFound();
    };
  };

  const invalidTransition = () =>
    problem(422, 'serviceOrders.invalidTransition', text.serviceOrderInvalidTransition);

  const router = new FakeRouter()
    .on('GET', SERVICE_ORDER_PATHS.collection, (request) => {
      const principal = guard(request, staff);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const items = [...db().orders].sort((a, b) => b.number - a.number).map(card);
      return ok({ items, page: 1, pageSize: items.length, totalItems: items.length });
    })
    .on('GET', SERVICE_ORDER_PATHS.mine, (request) => {
      const principal = guard(request, mechanics);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const name = nameOf(principal);
      return ok({
        items: db()
          .orders.filter(
            (order) =>
              order.mechanicName === name &&
              order.status !== 'Delivered' &&
              order.status !== 'Completed',
          )
          .map(work),
      });
    })
    .on(
      'GET',
      SERVICE_ORDER_PATHS.item,
      withOrder(staff, (order) => ok(detail(order))),
    )
    .on(
      'POST',
      SERVICE_ORDER_PATHS.transitions,
      withOrder(desk, (order, principal, request) => {
        const body = readBody(request);
        const to = body['to'] as FakeOrderStatus;
        const reason = readText(body, 'reason');
        if (body['version'] !== order.version) {
          return problem(409, 'serviceOrders.versionConflict', text.serviceOrderVersionConflict);
        }

        if (!ALLOWED[order.status].includes(to)) {
          return invalidTransition();
        }

        if (to === 'WaitingApproval' && order.quoteItems.length === 0) {
          return problem(
            422,
            'serviceOrders.quoteWithoutItems',
            text.serviceOrderQuoteWithoutItems,
          );
        }

        const rejecting = order.status === 'WaitingApproval' && to === 'InDiagnosis';
        if (rejecting && reason.length < 5) {
          return validation([
            fieldError('reason', 'serviceOrders.rejectionReasonRequired', text.quoteRejectReason),
          ]);
        }

        moveOrder(order, to, nameOf(principal), rejecting ? reason : null);
        return ok(detail(order));
      }),
    )
    .on(
      'POST',
      SERVICE_ORDER_PATHS.timeStart,
      withOrder(mechanics, (order, principal) => {
        if (order.status !== 'InProgress' && order.status !== 'InDiagnosis') {
          return invalidTransition();
        }

        if (!running(order)) {
          order.timeEntries.push({
            id: fakeId(),
            mechanicName: nameOf(principal),
            startedAt: fakeNow(),
            endedAt: null,
          });
        }

        return ok(detail(order));
      }),
    )
    .on(
      'POST',
      SERVICE_ORDER_PATHS.timeStop,
      withOrder(mechanics, (order) => {
        const entry = running(order);
        if (entry) {
          entry.endedAt = fakeNow();
        }

        return ok(detail(order));
      }),
    )
    .on(
      'GET',
      SERVICE_ORDER_PATHS.work,
      withOrder(staff, (order) => ok(work(order))),
    )
    .on(
      'PUT',
      SERVICE_ORDER_PATHS.checklist,
      withOrder(mechanics, (order, _principal, request) => {
        const items = readBody(request)['items'];
        if (!Array.isArray(items)) {
          return validation([
            fieldError('items', 'serviceOrders.invalidChecklist', text.invalidField),
          ]);
        }

        for (const raw of items as { id?: unknown; state?: unknown }[]) {
          const item = order.checklist.find((candidate) => candidate.id === raw.id);
          const state = raw.state as FakeChecklistItem['state'];
          if (
            item &&
            (state === null || state === 'Ok' || state === 'Attention' || state === 'Replace')
          ) {
            item.state = state;
          }
        }

        return ok(work(order));
      }),
    )
    .on(
      'POST',
      SERVICE_ORDER_PATHS.photos,
      withOrder(mechanics, (order, _principal, request) => {
        const body = readBody(request);
        const contentType = readText(body, 'contentType');
        const dataUrl = readText(body, 'dataUrl');
        const clientId = readText(body, 'clientId') || null;
        if (!PHOTO_TYPES.includes(contentType) || !dataUrl.startsWith(`data:${contentType}`)) {
          return validation([
            fieldError('contentType', 'serviceOrders.photoType', text.serviceOrderPhotoType),
          ]);
        }

        if (dataUrl.length > MAX_PHOTO_DATA_URL) {
          return validation([
            fieldError('dataUrl', 'serviceOrders.photoSize', text.serviceOrderPhotoSize),
          ]);
        }

        if (!order.photos.some((photo) => clientId !== null && photo.clientId === clientId)) {
          order.photos.push({
            id: fakeId(),
            clientId,
            url: dataUrl,
            caption: readText(body, 'fileName'),
            takenAt: fakeNow(),
          });
        }

        return ok(work(order));
      }),
    )
    .on(
      'PUT',
      SERVICE_ORDER_PATHS.diagnosis,
      withOrder(mechanics, (order, _principal, request) => {
        order.diagnosis = readText(readBody(request), 'description');
        return ok(work(order));
      }),
    )
    .on(
      'POST',
      SERVICE_ORDER_PATHS.suggestions,
      withOrder(mechanics, (_order, _principal, request) => {
        const description = readText(readBody(request), 'description');
        if (description.length < 3) {
          return validation([
            fieldError(
              'description',
              'serviceOrders.invalidDescription',
              text.serviceOrderDescription,
            ),
          ]);
        }

        const catalog = state.database.catalog;
        return ok({
          items: SUGGESTIONS.filter((suggestion) => suggestion.keywords.test(description)).flatMap(
            (suggestion) => {
              const match =
                suggestion.kind === 'Service'
                  ? catalog.services.find((item) => item.name === suggestion.name)
                  : catalog.parts.find((item) => item.name === suggestion.name);
              return match
                ? [
                    {
                      kind: suggestion.kind,
                      itemId: match.id,
                      name: match.name,
                      confidence: suggestion.confidence,
                      reason: suggestion.reason,
                    },
                  ]
                : [];
            },
          ),
        });
      }),
    )
    .on(
      'POST',
      SERVICE_ORDER_PATHS.sendToQuote,
      withOrder(mechanics, (order, _principal, request) => {
        if (order.status !== 'InDiagnosis') {
          return problem(
            422,
            'serviceOrders.itemsOnlyInDiagnosis',
            text.serviceOrderItemsOnlyInDiagnosis,
          );
        }

        const body = readBody(request);
        order.diagnosis = readText(body, 'description') || order.diagnosis;
        const items = Array.isArray(body['items']) ? (body['items'] as { itemId?: unknown }[]) : [];
        const catalog = state.database.catalog;
        for (const raw of items) {
          const service = catalog.services.find((item) => item.id === raw.itemId);
          const part = catalog.parts.find((item) => item.id === raw.itemId);
          const name = service?.name ?? part?.name;
          if (name && !order.quoteItems.some((item) => item.name === name)) {
            order.quoteItems.push({
              name,
              kind: service ? 'Service' : 'Part',
              quantity: 1,
              unitPrice: service?.basePrice ?? part?.unitPrice ?? 0,
            });
          }
        }

        order.sentToQuote = true;
        order.quoteStatus = order.quoteStatus ?? 'Pending';
        order.updatedAt = fakeNow();
        order.version += 1;
        return ok(work(order));
      }),
    )
    .on(
      'POST',
      SERVICE_ORDER_PATHS.startService,
      withOrder(mechanics, (order, principal) => {
        if (order.status === 'Received') {
          moveOrder(order, 'InDiagnosis', nameOf(principal), null);
          return ok(work(order));
        }

        if (order.status !== 'InProgress') {
          return invalidTransition();
        }

        if (!running(order)) {
          order.timeEntries.push({
            id: fakeId(),
            mechanicName: nameOf(principal),
            startedAt: fakeNow(),
            endedAt: null,
          });
        }

        return ok(work(order));
      }),
    )
    .on(
      'POST',
      SERVICE_ORDER_PATHS.finishService,
      withOrder(mechanics, (order, principal) => {
        if (order.status !== 'InProgress') {
          return invalidTransition();
        }

        const entry = running(order);
        if (entry) {
          entry.endedAt = fakeNow();
        }

        moveOrder(order, 'Completed', nameOf(principal), null);
        return ok(work(order));
      }),
    );

  return new FakeFeatureApi('serviceOrders', router, state.scenarios);
}
