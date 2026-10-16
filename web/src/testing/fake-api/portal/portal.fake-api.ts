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
import { authorize, FakeRole, isPrincipal } from '../fake-api/fake-session';
import { FakeRequest, FakeResult } from '../fake-request';
import { moveOrder } from '../service-orders/service-orders.fake-api';
import { FakeBoardOrder, FakeServiceOrders } from '../service-orders/service-orders.seed';

export const PORTAL_TOKEN = 'portal-demo-3324';
export const PORTAL_DONE_TOKEN = 'portal-done-3310';
export const PORTAL_EXPIRED_TOKEN = 'portal-expired-3310';
export const PORTAL_SHOP = 'Oficina Exemplo';
export const SAMPLE_PDF_URL = '/documents/exemplo.pdf';
export const PROVIDER_CHECKOUT = 'https://pagamento.provedor.example/checkout/';
export const MAX_ATTEMPTS = 3;
export const PIX_POLLS_TO_PAY = 2;

export interface FakePortalLink {
  token: string;
  orderId: string;
  expiresAt: string;
  attempts: number;
}

export interface FakeCharge {
  id: string;
  orderId: string;
  method: 'Pix' | 'Card';
  status: 'Pending' | 'Paid' | 'Expired';
  expiresAt: string;
  polls: number;
}

export interface FakePortal {
  links: FakePortalLink[];
  sessions: { id: string; orderId: string; expiresAt: string }[];
  charges: FakeCharge[];
  paidOrders: { orderId: string; paidAt: string }[];
}

const staff: readonly FakeRole[] = ['Admin', 'Manager', 'Attendant', 'Mechanic'];
const text = messages.fake;
const hours = (value: number): string => new Date(Date.now() + value * 3_600_000).toISOString();
const QR = `data:image/svg+xml;base64,${btoa(
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 8 8"><path d="M0 0h3v3H0zM5 0h3v3H5zM0 5h3v3H0zM4 4h1v1H4zM6 6h2v2H6z"/></svg>',
)}`;

export function createPortalSeed(serviceOrders: FakeServiceOrders): FakePortal {
  const id = (number: number): string =>
    serviceOrders.orders.find((order) => order.number === number)?.id ?? '';
  return {
    links: [
      { token: PORTAL_TOKEN, orderId: id(3324), expiresAt: hours(24 * 7), attempts: 0 },
      { token: PORTAL_DONE_TOKEN, orderId: id(3310), expiresAt: hours(24 * 7), attempts: 0 },
      { token: PORTAL_EXPIRED_TOKEN, orderId: id(3310), expiresAt: hours(-24), attempts: 0 },
    ],
    sessions: [],
    charges: [],
    paidOrders: [{ orderId: id(3310), paidAt: hours(-2) }],
  };
}

export function expireCharges(state: FakeBackendState): void {
  for (const charge of state.database.portal.charges) {
    charge.expiresAt = hours(-1);
  }
}

export function createPortalFakeApi(state: FakeBackendState): FakeFeatureApi {
  const portal = (): FakePortal => state.database.portal;
  const orders = () => state.database.serviceOrders.orders;
  const orderById = (id: string): FakeBoardOrder | null =>
    orders().find((order) => order.id === id) ?? null;
  const customerOf = (order: FakeBoardOrder) =>
    state.database.customers.find((customer) => customer.id === order.customerId) ?? null;
  const unavailable = () => problem(404, 'portal.linkUnavailable', text.portalUnavailable);
  const locked = () => problem(423, 'portal.locked', text.portalLocked);
  const notFound = () => problem(404, 'documents.notFound', text.documentNotFound);

  const linkOf = (request: FakeRequest): FakePortalLink | null => {
    const token = readText(readBody(request), 'token');
    const link = portal().links.find((candidate) => candidate.token === token);
    return link && Date.parse(link.expiresAt) > Date.now() ? link : null;
  };

  const sessionOrder = (request: FakeRequest): FakeBoardOrder | null => {
    const session = portal().sessions.find(
      (candidate) =>
        request.portalSession === candidate.id && Date.parse(candidate.expiresAt) > Date.now(),
    );
    return session ? orderById(session.orderId) : null;
  };

  const items = (order: FakeBoardOrder) =>
    order.quoteItems.map((item, index) => ({
      id: `${order.id}-item-${index}`,
      name: item.name,
      kind: item.kind,
      total: Math.round(item.unitPrice * item.quantity * 100) / 100,
    }));

  const subtotal = (order: FakeBoardOrder): number =>
    items(order).reduce((sum, item) => sum + item.total, 0);
  const total = (order: FakeBoardOrder): number =>
    Math.max(0, Math.round((subtotal(order) - order.discount) * 100) / 100);
  const paidAt = (order: FakeBoardOrder): string | null =>
    portal().paidOrders.find((paid) => paid.orderId === order.id)?.paidAt ?? null;

  const view = (order: FakeBoardOrder) => ({
    number: order.number,
    shopName: PORTAL_SHOP,
    customerFirstName: (customerOf(order)?.name ?? '').split(' ')[0] ?? '',
    vehicle: order.vehicle,
    licensePlate: order.plate,
    status: order.status,
    statusHistory: order.history.map((entry) => ({
      status: entry.newStatus,
      changedAt: entry.changedAt,
    })),
    photos: order.photos.map((photo) => ({ id: photo.id, url: photo.url, caption: photo.caption })),
    quote:
      order.quoteStatus === null
        ? null
        : {
            number: order.number + 1000,
            version: 1,
            status: order.quoteStatus,
            items: items(order),
            discountTotal: order.discount,
            total: total(order),
          },
    payment:
      order.quoteStatus === 'Approved'
        ? {
            status: paidAt(order) ? 'Paid' : 'Pending',
            amountDue: paidAt(order) ? 0 : total(order),
            methods: ['Pix', 'Credit'],
          }
        : null,
    documents: order.documents.map((document) => ({ ...document })),
  });

  const withSession = (handler: (order: FakeBoardOrder, request: FakeRequest) => FakeResult) => {
    return (request: FakeRequest): FakeResult => {
      const order = sessionOrder(request);
      return order
        ? handler(order, request)
        : problem(401, 'portal.sessionExpired', text.portalSessionExpired);
    };
  };

  const documentOf = (code: string) => {
    for (const order of orders()) {
      const document = order.documents.find((item) => item.code === code);
      if (document) {
        return { order, document };
      }
    }

    return null;
  };

  const documentView = (order: FakeBoardOrder, document: FakeBoardOrder['documents'][number]) => {
    const customer = customerOf(order);
    const services = order.quoteItems
      .filter((item) => item.kind === 'Service')
      .reduce((sum, item) => sum + item.unitPrice * item.quantity, 0);
    return {
      kind: document.kind,
      code: document.code,
      number: document.kind === 'Quote' ? order.number + 1000 : order.number,
      version: document.kind === 'Quote' ? 1 : null,
      issuedAt: document.issuedAt,
      shopName: PORTAL_SHOP,
      customerName: customer?.name ?? '',
      vehicle: `${order.plate} · ${order.vehicle}`,
      lines: order.quoteItems.map((item) => ({
        name: item.name,
        quantity: item.quantity,
        unitPrice: item.unitPrice,
        total: Math.round(item.unitPrice * item.quantity * 100) / 100,
      })),
      servicesTotal: services,
      partsTotal: Math.round((subtotal(order) - services) * 100) / 100,
      discountTotal: order.discount,
      total: total(order),
      paidAt: document.kind === 'Receipt' ? paidAt(order) : null,
      paymentMethods: ['Pix', 'Credit'],
    };
  };

  const router = new FakeRouter()
    .on('POST', '/api/public/portal/access', (request) => {
      const link = linkOf(request);
      if (!link) {
        return unavailable();
      }

      return link.attempts >= MAX_ATTEMPTS
        ? locked()
        : ok({ attemptsLeft: MAX_ATTEMPTS - link.attempts });
    })
    .on('POST', '/api/public/portal/confirm', (request) => {
      const link = linkOf(request);
      if (!link) {
        return unavailable();
      }

      if (link.attempts >= MAX_ATTEMPTS) {
        return locked();
      }

      const order = orderById(link.orderId);
      const phone = (order ? customerOf(order)?.phone : null) ?? '';
      if (readText(readBody(request), 'lastDigits') !== phone.slice(-3)) {
        link.attempts += 1;
        return link.attempts >= MAX_ATTEMPTS
          ? locked()
          : ok({ status: 'WrongDigits', attemptsLeft: MAX_ATTEMPTS - link.attempts });
      }

      link.attempts = 0;
      const session = { id: fakeId(), orderId: link.orderId, expiresAt: hours(1) };
      portal().sessions.push(session);
      return ok({ status: 'Confirmed', session: session.id, expiresAt: session.expiresAt });
    })
    .on(
      'GET',
      '/api/public/portal/order',
      withSession((order) => ok(view(order))),
    )
    .on(
      'POST',
      '/api/public/portal/quote/approve',
      withSession((order, request) => {
        if (order.status !== 'WaitingApproval' || order.quoteStatus !== 'Pending') {
          return problem(409, 'portal.alreadyAnswered', text.portalAlreadyAnswered);
        }

        const raw = readBody(request)['itemIds'];
        const ids = Array.isArray(raw) ? raw : [];
        const keep = items(order).map((item) => ids.includes(item.id));
        if (!keep.some(Boolean)) {
          return validation([fieldError('itemIds', 'portal.noItems', text.portalNoItems)]);
        }

        const before = subtotal(order);
        order.quoteItems = order.quoteItems.filter((_item, index) => keep[index]);
        order.discount =
          before > 0 ? Math.round((order.discount * subtotal(order) * 100) / before) / 100 : 0;
        moveOrder(order, 'InProgress', customerOf(order)?.name ?? '', null);
        return ok(view(order));
      }),
    )
    .on(
      'POST',
      '/api/public/portal/quote/reject',
      withSession((order, request) => {
        if (order.status !== 'WaitingApproval' || order.quoteStatus !== 'Pending') {
          return problem(409, 'portal.alreadyAnswered', text.portalAlreadyAnswered);
        }

        const reason = readText(readBody(request), 'reason');
        if (reason.length < 5) {
          return validation([fieldError('reason', 'portal.rejectReason', text.quoteRejectReason)]);
        }

        moveOrder(order, 'InDiagnosis', customerOf(order)?.name ?? '', reason);
        return ok(view(order));
      }),
    )
    .on(
      'POST',
      '/api/public/portal/payments/pix',
      withSession((order) => {
        if (order.quoteStatus !== 'Approved' || paidAt(order)) {
          return problem(422, 'portal.nothingToPay', text.portalNothingToPay);
        }

        const charge: FakeCharge = {
          id: fakeId(),
          orderId: order.id,
          method: 'Pix',
          status: 'Pending',
          expiresAt: hours(0.5),
          polls: 0,
        };
        portal().charges.push(charge);
        return ok({
          paymentId: charge.id,
          copyPaste: `00020126580014BR.GOV.BCB.PIX0136${charge.id}5204000053039865406${total(order).toFixed(2)}`,
          qrCodeDataUrl: QR,
          expiresAt: charge.expiresAt,
        });
      }),
    )
    .on('GET', '/api/public/portal/payments/:id', (request, params) => {
      const order = sessionOrder(request);
      const charge = portal().charges.find(
        (item) => item.id === params['id'] && item.orderId === order?.id,
      );
      if (!order || !charge) {
        return problem(404, 'portal.paymentNotFound', text.portalNothingToPay);
      }

      if (charge.status === 'Pending' && Date.parse(charge.expiresAt) <= Date.now()) {
        charge.status = 'Expired';
      } else if (charge.status === 'Pending') {
        charge.polls += 1;
        if (charge.polls >= PIX_POLLS_TO_PAY) {
          charge.status = 'Paid';
          portal().paidOrders.push({ orderId: order.id, paidAt: fakeNow() });
        }
      }

      return ok({ status: charge.status });
    })
    .on(
      'POST',
      '/api/public/portal/payments/card',
      withSession((order) => {
        if (order.quoteStatus !== 'Approved' || paidAt(order)) {
          return problem(422, 'portal.nothingToPay', text.portalNothingToPay);
        }

        const id = fakeId();
        portal().charges.push({
          id,
          orderId: order.id,
          method: 'Card',
          status: 'Pending',
          expiresAt: hours(1),
          polls: 0,
        });
        return ok({ redirectUrl: `${PROVIDER_CHECKOUT}${id}` });
      }),
    )
    .on(
      'GET',
      '/api/public/portal/documents/:code',
      withSession((order, request) => {
        const found = documentOf(decodeURIComponent(request.path.split('/').pop() ?? ''));
        return found && found.order.id === order.id
          ? ok(documentView(found.order, found.document))
          : notFound();
      }),
    )
    .on(
      'GET',
      '/api/public/portal/documents/:code/pdf',
      withSession((order, request) => {
        const code = decodeURIComponent(request.path.split('/').at(-2) ?? '');
        const found = documentOf(code);
        return found && found.order.id === order.id
          ? ok({ url: SAMPLE_PDF_URL, fileName: `${code}.pdf` })
          : notFound();
      }),
    )
    .on('GET', '/api/public/documents/:code/verification', (_request, params) => {
      const found = documentOf(params['code'] ?? '');
      return found
        ? ok({
            valid: true,
            kind: found.document.kind,
            number: documentView(found.order, found.document).number,
            issuedAt: found.document.issuedAt,
            total: total(found.order),
            shopName: PORTAL_SHOP,
          })
        : ok({ valid: false });
    })
    .on('GET', '/api/admin/documents/:code', (request, params) => {
      const principal = authorize(request, state.database.users, staff);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const found = documentOf(params['code'] ?? '');
      return found ? ok(documentView(found.order, found.document)) : notFound();
    })
    .on('GET', '/api/admin/documents/:code/pdf', (request, params) => {
      const principal = authorize(request, state.database.users, staff);
      if (!isPrincipal(principal)) {
        return principal;
      }

      return documentOf(params['code'] ?? '')
        ? ok({ url: SAMPLE_PDF_URL, fileName: `${params['code'] ?? ''}.pdf` })
        : notFound();
    })
    .on('POST', '/api/admin/documents/:code/send', (request, params) => {
      const principal = authorize(request, state.database.users, staff);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const channel = readBody(request)['channel'];
      if (channel !== 'WhatsApp' && channel !== 'Email') {
        return validation([fieldError('channel', 'documents.invalidChannel', text.invalidField)]);
      }

      return documentOf(params['code'] ?? '') ? ok({ status: 'Queued' }) : notFound();
    });

  return new FakeFeatureApi('portal', router, state.scenarios);
}
