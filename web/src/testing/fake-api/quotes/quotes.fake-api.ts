import { messages } from '../../../app/core/i18n/messages.pt-BR';
import {
  isValidQuantity,
  priceQuote,
  sendBlocks,
} from '../../../app/features/quotes/data/quote.math';
import {
  DiscountValue,
  ManualDiscount,
  QuoteLine,
} from '../../../app/features/quotes/data/quote.models';
import { cents, centsFromDecimal, formatBrl } from '../../../app/shared/util/money';
import { FakeCustomerRecord } from '../fake-api/fake-database';
import { FakeRequest, FakeResult } from '../fake-request';
import { FakeBackendState, FakeFeatureApi } from '../fake-api/fake-backend';
import {
  accepted,
  created,
  fieldError,
  FakeRouter,
  ok,
  paginate,
  problem,
  readBody,
  readText,
  validation,
} from '../fake-api/fake-http';
import { fakeId, fakeNow } from '../fake-api/fake-ids';
import { authorize, FakePrincipal, FakeRole, isPrincipal } from '../fake-api/fake-session';
import {
  FAKE_PORTAL_ORIGIN,
  FakeApproval,
  FakeChannel,
  FakeDelivery,
  FakeDiscount,
  FakeItemKind,
  FakePaymentMethod,
  FakeQuoteLine,
  FakeQuoteRecord,
  FakeQuotes,
  FakeServiceOrder,
} from './quotes.seed';

export const QUOTE_PATHS = {
  collection: '/api/admin/quotes',
  serviceOrders: '/api/admin/quotes/service-orders',
  item: '/api/admin/quotes/:id',
  options: '/api/admin/quotes/:id/options',
  draft: '/api/admin/quotes/:id/draft',
  approvals: '/api/admin/quotes/:id/discount-approvals',
  decision: '/api/admin/quotes/:id/discount-approvals/:approvalId/:action',
  messageDraft: '/api/admin/quotes/:id/message-draft',
  send: '/api/admin/quotes/:id/send',
  delivery: '/api/admin/quotes/:id/delivery',
  retry: '/api/admin/quotes/:id/delivery/retry',
  approve: '/api/admin/quotes/:id/approve',
  reject: '/api/admin/quotes/:id/reject',
  versions: '/api/admin/quotes/:id/versions',
} as const;

const viewers: readonly FakeRole[] = ['Admin', 'Manager', 'Attendant'];
const approvers: readonly FakeRole[] = ['Admin', 'Manager'];
const KINDS: readonly FakeItemKind[] = ['Package', 'Service', 'Part'];
const CHANNELS: readonly FakeChannel[] = ['WhatsApp', 'Email', 'Link'];
const METHODS: readonly FakePaymentMethod[] = ['Pix', 'Credit', 'Debit', 'Cash', 'Boleto'];
const MAX_MESSAGE = 1000;
const MIN_REASON = 5;
const text = messages.fake;

function toDiscountValue(discount: FakeDiscount): DiscountValue {
  return discount.type === 'Percent'
    ? { kind: 'Percent', basisPoints: Math.round(discount.value * 100) }
    : { kind: 'Amount', amount: centsFromDecimal(discount.value) };
}

export function maskPhone(phone: string | null): string | null {
  if (!phone || phone.length < 10) {
    return null;
  }

  return `(${phone.slice(0, 2)}) ${phone.slice(2, 3)}••••-${phone.slice(-4)}`;
}

export function maskEmail(email: string | null): string | null {
  const [name = '', domain = ''] = email?.split('@') ?? [];
  return name && domain ? `${name.slice(0, 1)}••••@${domain}` : null;
}

function isExpired(delivery: FakeDelivery, now = Date.now()): boolean {
  return Date.parse(delivery.expiresAt) <= now;
}

export function isEditable(quote: FakeQuoteRecord): boolean {
  const delivery = quote.delivery;
  return (
    quote.status === 'Pending' &&
    (delivery === null || delivery.status === 'Failed' || isExpired(delivery))
  );
}

function readDiscount(
  body: Readonly<Record<string, unknown>>,
): (FakeDiscount & { reason: string }) | null {
  const type = body['type'];
  const value = body['value'];
  if ((type !== 'Percent' && type !== 'Amount') || typeof value !== 'number') {
    return null;
  }

  return { type, value, reason: readText(body, 'reason') };
}

export function createQuotesFakeApi(state: FakeBackendState): FakeFeatureApi {
  const db = (): FakeQuotes => state.database.quotes;
  const catalog = () => state.database.catalog;
  const customerOf = (order: FakeServiceOrder): FakeCustomerRecord | null =>
    state.database.customers.find((customer) => customer.id === order.customerId) ?? null;
  const orderOf = (quote: FakeQuoteRecord): FakeServiceOrder | null =>
    db().serviceOrders.find((order) => order.id === quote.serviceOrderId) ?? null;
  const nameOf = (principal: FakePrincipal): string =>
    state.database.users.find((user) => user.id === principal.id)?.name ??
    messages.roles[principal.role];
  const limitOf = (principal: FakePrincipal): number =>
    state.database.teamProfiles.find((profile) => profile.userId === principal.id)
      ?.discountLimitPercent ?? db().discountLimits[principal.role];
  const latest = (number: number): FakeQuoteRecord | null =>
    db()
      .quotes.filter((quote) => quote.number === number)
      .reduce<FakeQuoteRecord | null>(
        (best, quote) => (best === null || quote.version > best.version ? quote : best),
        null,
      );

  const facts = (customerId: string) => ({
    loyal: db().loyalCustomerIds.includes(customerId),
    firstService: db().firstServiceCustomerIds.includes(customerId),
    fleet: db().fleetCustomerIds.includes(customerId),
  });

  const toLines = (items: readonly FakeQuoteLine[]): QuoteLine[] =>
    items.map((item) => ({
      lineId: item.id,
      kind: item.kind,
      itemId: item.itemId,
      name: item.name,
      unitPrice: centsFromDecimal(item.unitPrice),
      quantity: item.quantity,
      source: item.source,
      fractional: item.fractional,
    }));

  const manualOf = (quote: FakeQuoteRecord): ManualDiscount | null =>
    quote.manualDiscount
      ? { value: toDiscountValue(quote.manualDiscount), reason: quote.manualDiscount.reason }
      : null;

  const pricing = (quote: FakeQuoteRecord, limitPercent: number) => {
    const order = orderOf(quote);
    return priceQuote({
      lines: toLines(quote.items),
      rules: db().rules.map((rule) => ({
        id: rule.id,
        name: rule.name,
        value: toDiscountValue(rule),
        condition: rule.condition,
        isActive: rule.isActive,
      })),
      facts: facts(order?.customerId ?? ''),
      mode: quote.discountMode,
      manual: manualOf(quote),
      limitBasisPoints: Math.round(limitPercent * 100),
    });
  };

  const totalsOf = (quote: FakeQuoteRecord, limitPercent = 100) => {
    const price = pricing(quote, limitPercent);
    return {
      servicesTotal: price.services / 100,
      partsTotal: price.parts / 100,
      discountTotal: price.discount / 100,
      total: price.total / 100,
    };
  };

  const deliveryDto = (delivery: FakeDelivery | null) => (delivery ? { ...delivery } : null);

  const toDto = (quote: FakeQuoteRecord) => {
    const order = orderOf(quote);
    const customer = order ? customerOf(order) : null;
    const customerFacts = facts(order?.customerId ?? '');
    return {
      id: quote.id,
      number: quote.number,
      serviceOrderId: quote.serviceOrderId,
      serviceOrderNumber: order?.number ?? 0,
      version: quote.version,
      status: quote.status,
      customer: {
        id: customer?.id ?? '',
        name: customer?.name ?? '',
        maskedPhone: maskPhone(customer?.phone ?? null),
        maskedEmail: maskEmail(customer?.email ?? null),
        consentWhatsApp: Boolean(customer?.consentWhatsApp && customer.phone),
        consentEmail: Boolean(customer?.consentEmail && customer.email),
        ...customerFacts,
      },
      vehicle: {
        licensePlate: order?.plate ?? '',
        description: order?.vehicle ?? '',
        mileage: order?.mileage ?? 0,
      },
      diagnosis: order?.diagnosis ?? null,
      items: quote.items.map((item) => ({ ...item })),
      discountMode: quote.discountMode,
      manualDiscount: quote.manualDiscount ? { ...quote.manualDiscount } : null,
      payment: {
        methods: [...quote.payment.methods],
        maxInstallments: quote.payment.maxInstallments,
      },
      approval: quote.approval
        ? { ...quote.approval, discount: { ...quote.approval.discount } }
        : null,
      delivery: deliveryDto(quote.delivery),
      rejectionReason: quote.rejectionReason,
      ...totalsOf(quote),
      updatedAt: quote.updatedAt,
    };
  };

  const summaryOf = (quote: FakeQuoteRecord) => {
    const order = orderOf(quote);
    return {
      id: quote.id,
      number: quote.number,
      version: quote.version,
      status: quote.status,
      customerName: (order ? customerOf(order)?.name : null) ?? '',
      licensePlate: order?.plate ?? '',
      total: totalsOf(quote).total,
      deliveryStatus: quote.delivery?.status ?? null,
      approvalPending: quote.approval?.status === 'Pending',
      updatedAt: quote.updatedAt,
    };
  };

  const optionOf = (kind: FakeItemKind, itemId: string) => {
    if (kind === 'Package') {
      const item = catalog().packages.find(
        (candidate) => candidate.id === itemId && candidate.isActive,
      );
      return item
        ? {
            kind,
            itemId: item.id,
            name: item.name,
            detail: `${item.items.length}`,
            unitPrice: item.price,
            fractional: false,
            stock: null,
          }
        : null;
    }

    if (kind === 'Service') {
      const item = catalog().services.find(
        (candidate) => candidate.id === itemId && candidate.isActive,
      );
      return item
        ? {
            kind,
            itemId: item.id,
            name: item.name,
            detail: `${item.estimatedMinutes}`,
            unitPrice: item.basePrice,
            fractional: false,
            stock: null,
          }
        : null;
    }

    const item = catalog().parts.find((candidate) => candidate.id === itemId && candidate.isActive);
    const supplier = state.database.inventory.suppliers.find(
      (candidate) => candidate.name === item?.supplier,
    );
    return item
      ? {
          kind,
          itemId: item.id,
          name: item.name,
          detail: item.code,
          unitPrice: item.unitPrice,
          fractional: item.type === 'Supply',
          stock: {
            available: item.stockQuantity,
            leadTimeDays: supplier?.leadTimeDays ?? null,
            supplier: item.supplier,
          },
        }
      : null;
  };

  const find = (id: string): FakeQuoteRecord | null =>
    db().quotes.find((quote) => quote.id === id) ?? null;

  const guard = (request: FakeRequest, roles: readonly FakeRole[]): FakePrincipal | FakeResult =>
    authorize(request, state.database.users, roles);

  const notFound = () => problem(404, 'quotes.notFound', text.quoteNotFound);
  const locked = () => problem(409, 'quotes.locked', text.quoteLocked);
  const touch = (quote: FakeQuoteRecord): void => {
    quote.updatedAt = fakeNow();
  };

  const newDelivery = (channel: FakeChannel, days: number, attempts: number): FakeDelivery => ({
    id: fakeId(),
    channel,
    status: 'Queued',
    link: `${FAKE_PORTAL_ORIGIN}/q/${fakeId().replace(/-/g, '')}`,
    expiresAt: new Date(Date.now() + days * 24 * 60 * 60 * 1000).toISOString(),
    sentAt: null,
    openedAt: null,
    failureReason: null,
    attempts,
  });

  const router = new FakeRouter()
    .on('GET', QUOTE_PATHS.collection, (request) => {
      const principal = guard(request, viewers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const status = request.query.get('status');
      const numbers = [...new Set(db().quotes.map((quote) => quote.number))].sort((a, b) => b - a);
      const rows = numbers
        .map(latest)
        .filter((quote): quote is FakeQuoteRecord => quote !== null)
        .filter((quote) => !status || quote.status === status)
        .map(summaryOf);
      return paginate(rows, request.query);
    })
    .on('GET', QUOTE_PATHS.serviceOrders, (request) => {
      const principal = guard(request, viewers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      return ok({
        items: db()
          .serviceOrders.filter(
            (order) => !db().quotes.some((quote) => quote.serviceOrderId === order.id),
          )
          .map((order) => ({
            id: order.id,
            number: order.number,
            customerName: customerOf(order)?.name ?? '',
            licensePlate: order.plate,
            vehicle: order.vehicle,
          })),
      });
    })
    .on('POST', QUOTE_PATHS.collection, (request) => {
      const principal = guard(request, viewers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const order = db().serviceOrders.find(
        (candidate) => candidate.id === readText(readBody(request), 'serviceOrderId'),
      );
      if (!order) {
        return problem(404, 'serviceOrders.notFound', text.serviceOrderNotFound);
      }

      if (db().quotes.some((quote) => quote.serviceOrderId === order.id)) {
        return problem(409, 'quotes.alreadyExists', text.quoteExists);
      }

      const now = fakeNow();
      const quote: FakeQuoteRecord = {
        id: fakeId(),
        number: Math.max(0, ...db().quotes.map((item) => item.number)) + 1,
        serviceOrderId: order.id,
        version: 1,
        status: 'Pending',
        items: [],
        discountMode: 'Automatic',
        manualDiscount: null,
        payment: {
          methods: [...db().payment.methods],
          maxInstallments: db().payment.maxInstallments,
        },
        approval: null,
        delivery: null,
        rejectionReason: null,
        createdAt: now,
        updatedAt: now,
      };
      db().quotes.push(quote);
      return created(toDto(quote));
    })
    .on('GET', QUOTE_PATHS.item, (request, params) => {
      const principal = guard(request, viewers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const quote = find(params['id'] ?? '');
      return quote ? ok(toDto(quote)) : notFound();
    })
    .on('GET', QUOTE_PATHS.options, (request, params) => {
      const principal = guard(request, viewers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const quote = find(params['id'] ?? '');
      const order = quote ? orderOf(quote) : null;
      if (!quote || !order) {
        return notFound();
      }

      const options = (kind: FakeItemKind, ids: readonly string[]) =>
        ids.map((id) => optionOf(kind, id)).filter((option) => option !== null);
      return ok({
        packages: options(
          'Package',
          catalog().packages.map((item) => item.id),
        ),
        services: options(
          'Service',
          catalog().services.map((item) => item.id),
        ),
        parts: options(
          'Part',
          catalog().parts.map((item) => item.id),
        ),
        suggestions: order.suggestions.flatMap((suggestion) => {
          const option = optionOf(suggestion.kind, suggestion.itemId);
          return option ? [{ ...option, reason: suggestion.reason }] : [];
        }),
        discountRules: db().rules.map((rule) => ({ ...rule })),
        discountLimitPercent: limitOf(principal),
        payment: { ...db().payment, methods: [...db().payment.methods] },
        expirationDays: [...db().expirationDays],
      });
    })
    .on('PUT', QUOTE_PATHS.draft, (request, params) => {
      const principal = guard(request, viewers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const quote = find(params['id'] ?? '');
      if (!quote) {
        return notFound();
      }

      if (!isEditable(quote)) {
        return locked();
      }

      const body = readBody(request);
      const errors: ReturnType<typeof fieldError>[] = [];
      const rawItems = Array.isArray(body['items']) ? (body['items'] as unknown[]) : [];
      const items = rawItems.flatMap((raw, index): FakeQuoteLine[] => {
        const entry =
          typeof raw === 'object' && raw !== null ? (raw as Record<string, unknown>) : {};
        const kind = KINDS.find((candidate) => candidate === entry['kind']);
        const option = kind ? optionOf(kind, readText(entry, 'itemId')) : null;
        if (!kind || !option) {
          errors.push(
            fieldError(`items[${index}].itemId`, 'quotes.invalidItem', text.quoteInvalidItem),
          );
          return [];
        }

        const quantity = entry['quantity'];
        if (typeof quantity !== 'number' || !isValidQuantity(quantity, option.fractional)) {
          errors.push(
            fieldError(
              `items[${index}].quantity`,
              'quotes.invalidQuantity',
              text.quoteInvalidQuantity,
            ),
          );
          return [];
        }

        const unitPrice =
          typeof entry['unitPrice'] === 'number' ? entry['unitPrice'] : option.unitPrice;
        const previous = quote.items.find(
          (item) => item.kind === kind && item.itemId === option.itemId,
        );
        return [
          {
            id: previous?.id ?? fakeId(),
            kind,
            itemId: option.itemId,
            name: option.name,
            unitPrice: Math.max(0, unitPrice),
            quantity,
            source: entry['source'] === 'Ai' ? 'Ai' : 'Manual',
            fractional: option.fractional,
          },
        ];
      });

      const mode = body['discountMode'];
      const discountMode = mode === 'Manual' || mode === 'None' ? mode : 'Automatic';
      const rawManual = body['manualDiscount'];
      const manual =
        typeof rawManual === 'object' && rawManual !== null
          ? readDiscount(rawManual as Record<string, unknown>)
          : null;
      if (discountMode === 'Manual' && manual && manual.value <= 0) {
        errors.push(
          fieldError('manualDiscount.value', 'quotes.invalidDiscount', text.quoteInvalidDiscount),
        );
      }

      const payment =
        typeof body['payment'] === 'object' && body['payment'] !== null
          ? (body['payment'] as Record<string, unknown>)
          : {};
      const methods = Array.isArray(payment['methods'])
        ? METHODS.filter((method) => (payment['methods'] as unknown[]).includes(method))
        : quote.payment.methods;
      const maxInstallments =
        typeof payment['maxInstallments'] === 'number'
          ? Math.min(
              Math.max(1, Math.trunc(payment['maxInstallments'])),
              db().payment.maxInstallments,
            )
          : quote.payment.maxInstallments;

      if (errors.length > 0) {
        return validation(errors);
      }

      quote.items = items;
      quote.discountMode = discountMode;
      quote.manualDiscount = discountMode === 'Manual' ? manual : null;
      quote.payment = { methods, maxInstallments };
      if (quote.delivery && (quote.delivery.status === 'Failed' || isExpired(quote.delivery))) {
        quote.delivery = null;
      }

      touch(quote);
      return ok(toDto(quote));
    })
    .on('POST', QUOTE_PATHS.approvals, (request, params) => {
      const principal = guard(request, viewers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const quote = find(params['id'] ?? '');
      if (!quote) {
        return notFound();
      }

      if (!isEditable(quote)) {
        return locked();
      }

      const discount = readDiscount(readBody(request));
      if (!discount || discount.value <= 0) {
        return validation([
          fieldError('value', 'quotes.invalidDiscount', text.quoteInvalidDiscount),
        ]);
      }

      if (discount.reason.trim().length < MIN_REASON) {
        return validation([
          fieldError('reason', 'quotes.discountReason', text.quoteDiscountReason),
        ]);
      }

      const draft = { ...quote, discountMode: 'Manual' as const, manualDiscount: discount };
      if (!pricing(draft, limitOf(principal)).exceedsLimit) {
        return problem(422, 'quotes.approvalNotNeeded', text.quoteApprovalNotNeeded);
      }

      const approval: FakeApproval = {
        id: fakeId(),
        status: 'Pending',
        discount: { type: discount.type, value: discount.value },
        reason: discount.reason.trim(),
        requestedBy: nameOf(principal),
        requestedAt: fakeNow(),
        decidedBy: null,
      };
      quote.approval = approval;
      db().notifications.push({
        role: 'Manager',
        quoteId: quote.id,
        text: text.quoteApprovalNotification(approval.requestedBy, quote.number),
      });
      touch(quote);
      return created({ ...approval });
    })
    .on('POST', QUOTE_PATHS.decision, (request, params) => {
      const principal = guard(request, approvers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const quote = find(params['id'] ?? '');
      const approval = quote?.approval;
      if (!quote || !approval || approval.id !== params['approvalId']) {
        return notFound();
      }

      if (approval.status !== 'Pending') {
        return problem(409, 'quotes.approvalDecided', text.quoteApprovalDecided);
      }

      approval.status = params['action'] === 'approve' ? 'Approved' : 'Rejected';
      approval.decidedBy = nameOf(principal);
      touch(quote);
      return ok({ ...approval });
    })
    .on('POST', QUOTE_PATHS.messageDraft, (request, params) => {
      const principal = guard(request, viewers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const quote = find(params['id'] ?? '');
      const order = quote ? orderOf(quote) : null;
      if (!quote || !order) {
        return notFound();
      }

      const firstName = (customerOf(order)?.name ?? '').split(' ')[0] ?? '';
      const total = formatBrl(cents(Math.round(totalsOf(quote).total * 100)));
      return ok({
        message: text.quoteMessage(firstName, order.vehicle, order.plate, total, 7),
      });
    })
    .on('POST', QUOTE_PATHS.send, (request, params) => {
      const principal = guard(request, viewers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const quote = find(params['id'] ?? '');
      const order = quote ? orderOf(quote) : null;
      const customer = order ? customerOf(order) : null;
      if (!quote || !order || !customer) {
        return notFound();
      }

      if (!isEditable(quote)) {
        return locked();
      }

      const body = readBody(request);
      const channel = CHANNELS.find((candidate) => candidate === body['channel']);
      const days = body['expiresInDays'];
      const message = readText(body, 'message');
      const errors = [
        ...(typeof days === 'number' && db().expirationDays.includes(days)
          ? []
          : [fieldError('expiresInDays', 'quotes.invalidExpiration', text.quoteInvalidExpiration)]),
        ...(message.length > 0 && message.length <= MAX_MESSAGE
          ? []
          : [fieldError('message', 'quotes.invalidMessage', text.quoteInvalidMessage)]),
      ];
      if (!channel || errors.length > 0) {
        return validation(
          channel
            ? errors
            : [...errors, fieldError('channel', 'quotes.invalidChannel', text.invalidField)],
        );
      }

      if (
        (channel === 'WhatsApp' && !(customer.consentWhatsApp && customer.phone)) ||
        (channel === 'Email' && !(customer.consentEmail && customer.email))
      ) {
        return problem(422, 'quotes.noConsent', text.quoteNoConsent);
      }

      const price = pricing(quote, limitOf(principal));
      const blocks = sendBlocks({
        editable: true,
        lines: toLines(quote.items),
        mode: quote.discountMode,
        manual: manualOf(quote),
        approval: quote.approval
          ? {
              ...quote.approval,
              discount: toDiscountValue(quote.approval.discount),
            }
          : null,
        pricing: price,
      });
      if (blocks.includes('Empty')) {
        return problem(422, 'quotes.empty', text.quoteEmpty);
      }

      if (blocks.includes('ZeroTotal')) {
        return problem(422, 'quotes.zeroTotal', text.quoteZeroTotal);
      }

      if (blocks.includes('InvalidDiscount') || blocks.includes('ReasonRequired')) {
        return problem(422, 'quotes.discountReason', text.quoteDiscountReason);
      }

      if (blocks.some((block) => block.startsWith('Approval'))) {
        return problem(422, 'quotes.approvalRequired', text.quoteApprovalRequired);
      }

      quote.delivery = newDelivery(channel, days as number, (quote.delivery?.attempts ?? 0) + 1);
      touch(quote);
      return accepted({
        delivery: deliveryDto(quote.delivery),
        whatsAppUrl:
          channel === 'WhatsApp'
            ? `https://wa.me/55${customer.phone ?? ''}?text=${encodeURIComponent(message)}`
            : null,
      });
    })
    .on('GET', QUOTE_PATHS.delivery, (request, params) => {
      const principal = guard(request, viewers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const quote = find(params['id'] ?? '');
      if (!quote) {
        return notFound();
      }

      const delivery = quote.delivery;
      if (!delivery) {
        return problem(404, 'quotes.noDelivery', text.quoteNoDelivery);
      }

      const order = orderOf(quote);
      const flaky = db().flakyCustomerIds.includes(order?.customerId ?? '');
      if (delivery.status === 'Queued' && flaky && delivery.attempts === 1) {
        delivery.status = 'Failed';
        delivery.failureReason = text.quoteDeliveryFailed;
      } else if (delivery.status === 'Queued') {
        delivery.status = 'Sent';
        delivery.sentAt = fakeNow();
      } else if (delivery.status === 'Sent' && !isExpired(delivery)) {
        delivery.status = 'Opened';
        delivery.openedAt = fakeNow();
      }

      return ok(deliveryDto(delivery));
    })
    .on('POST', QUOTE_PATHS.retry, (request, params) => {
      const principal = guard(request, viewers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const quote = find(params['id'] ?? '');
      const delivery = quote?.delivery;
      if (!quote || !delivery) {
        return notFound();
      }

      if (quote.status !== 'Pending' || (delivery.status !== 'Failed' && !isExpired(delivery))) {
        return problem(409, 'quotes.retryNotAllowed', text.quoteRetryNotAllowed);
      }

      quote.delivery = newDelivery(delivery.channel, 7, delivery.attempts + 1);
      touch(quote);
      return accepted(deliveryDto(quote.delivery));
    })
    .on('POST', QUOTE_PATHS.approve, (request, params) => {
      const principal = guard(request, viewers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const quote = find(params['id'] ?? '');
      if (!quote) {
        return notFound();
      }

      if (quote.status !== 'Pending') {
        return problem(409, 'quotes.notPending', text.quoteNotPending);
      }

      if (quote.items.length === 0 || totalsOf(quote).total <= 0) {
        return problem(422, 'quotes.zeroTotal', text.quoteZeroTotal);
      }

      quote.status = 'Approved';
      touch(quote);
      return ok(toDto(quote));
    })
    .on('POST', QUOTE_PATHS.reject, (request, params) => {
      const principal = guard(request, viewers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const quote = find(params['id'] ?? '');
      if (!quote) {
        return notFound();
      }

      if (quote.status !== 'Pending') {
        return problem(409, 'quotes.notPending', text.quoteNotPending);
      }

      const reason = readText(readBody(request), 'reason');
      if (reason.length < MIN_REASON) {
        return validation([fieldError('reason', 'quotes.rejectReason', text.quoteRejectReason)]);
      }

      quote.status = 'Rejected';
      quote.rejectionReason = reason;
      touch(quote);
      return ok(toDto(quote));
    })
    .on('POST', QUOTE_PATHS.versions, (request, params) => {
      const principal = guard(request, viewers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const quote = find(params['id'] ?? '');
      if (!quote) {
        return notFound();
      }

      if (quote.status !== 'Rejected' || latest(quote.number)?.id !== quote.id) {
        return problem(409, 'quotes.versionNotAllowed', text.quoteVersionNotAllowed);
      }

      const now = fakeNow();
      const next: FakeQuoteRecord = {
        ...quote,
        id: fakeId(),
        version: quote.version + 1,
        status: 'Pending',
        items: quote.items.map((item) => ({ ...item, id: fakeId() })),
        approval: null,
        delivery: null,
        rejectionReason: null,
        createdAt: now,
        updatedAt: now,
      };
      db().quotes.push(next);
      return created(toDto(next));
    })
    .on('GET', QUOTE_PATHS.versions, (request, params) => {
      const principal = guard(request, viewers);
      if (!isPrincipal(principal)) {
        return principal;
      }

      const quote = find(params['id'] ?? '');
      if (!quote) {
        return notFound();
      }

      return ok({
        items: db()
          .quotes.filter((item) => item.number === quote.number)
          .sort((a, b) => b.version - a.version)
          .map((item) => ({
            id: item.id,
            version: item.version,
            status: item.status,
            items: item.items.map((line) => ({ ...line })),
            ...totalsOf(item),
            rejectionReason: item.rejectionReason,
            createdAt: item.createdAt,
          })),
      });
    });

  return new FakeFeatureApi('quotes', router, state.scenarios);
}
