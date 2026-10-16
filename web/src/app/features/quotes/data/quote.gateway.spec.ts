import { TestBed } from '@angular/core/testing';
import { firstValueFrom, Observable } from 'rxjs';
import { API_MODES, useGatewayHarness } from '../../../../testing/gateway';
import { maskEmail, maskPhone } from '../../../../testing/fake-api/quotes/quotes.fake-api';
import { ContractError } from '../../../core/data/contract-error';
import { ApiError } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { cents } from '../../../shared/util/money';
import { HttpQuoteGateway, QuoteGateway } from './quote.gateway';
import { toDiscountDto, toQuote, toQuoteOptions } from './quote.mappers';
import { Quote, QuoteDraft } from './quote.models';

const ATTENDANT = 'ana@oficina.local';
const MANAGER = 'gestor@oficina.local';
const MECHANIC = 'marcos@oficina.local';

async function failure(source: Observable<unknown>): Promise<ApiError> {
  try {
    await firstValueFrom(source);
  } catch (error) {
    return error as ApiError;
  }

  throw new Error('Expected the request to fail.');
}

function draftOf(quote: Quote, overrides: Partial<QuoteDraft> = {}): QuoteDraft {
  return {
    lines: quote.lines,
    discountMode: quote.discountMode,
    manualDiscount: quote.manualDiscount,
    payment: quote.payment,
    ...overrides,
  };
}

describe('quote mappers', () => {
  it('requires identifiers and converts money and percentages', () => {
    expect(() => toQuote({ id: 'q' })).toThrow(ContractError);
    expect(toDiscountDto({ kind: 'Percent', basisPoints: 1_250 })).toEqual({
      type: 'Percent',
      value: 12.5,
    });
    expect(toDiscountDto({ kind: 'Amount', amount: cents(3_050) })).toEqual({
      type: 'Amount',
      value: 30.5,
    });
    expect(toQuoteOptions({})).toMatchObject({
      packages: [],
      rules: [],
      discountLimitBasisPoints: 0,
      payment: { methods: [], credit: { maxInstallments: 1 } },
      expirationDays: [7],
    });
  });

  it('masks contact data in the fake contract', () => {
    expect(maskPhone('11987654321')).toBe('(11) 9••••-4321');
    expect(maskPhone('123')).toBeNull();
    expect(maskEmail('joana@example.com')).toBe('j••••@example.com');
    expect(maskEmail(null)).toBeNull();
  });
});

describe.each(API_MODES)('QuoteGateway (%s)', (mode) => {
  const harness = useGatewayHarness();

  async function setup(email?: string): Promise<QuoteGateway> {
    harness.configure(mode);
    await harness.login(email);
    return TestBed.inject(QuoteGateway);
  }

  function quoteId(number: number, version = 1): string {
    return (
      harness
        .state(mode)
        .database.quotes.quotes.find(
          (quote) => quote.number === number && quote.version === version,
        )?.id ?? ''
    );
  }

  it('lists the latest version of each quote with filters', async () => {
    const gateway = await setup();

    expect(gateway).toBeInstanceOf(HttpQuoteGateway);
    const page = await firstValueFrom(gateway.list({ status: null, page: 1, pageSize: 20 }));
    expect(page.items.map((item) => [item.number, item.version, item.status])).toEqual([
      [1047, 1, 'Pending'],
      [1042, 2, 'Pending'],
      [1040, 1, 'Approved'],
    ]);
    expect(page.items[0]).toMatchObject({ customerName: 'Joana Prado', plate: 'FJK4E21' });
    const approved = await firstValueFrom(
      gateway.list({ status: 'Approved', page: 1, pageSize: 20 }),
    );
    expect(approved.totalItems).toBe(1);
  });

  it('creates a quote from an open service order only once', async () => {
    const gateway = await setup(ATTENDANT);

    const orders = await firstValueFrom(gateway.openServiceOrders());
    expect(orders.map((order) => order.number)).toEqual([3322, 3323]);
    const order = orders[0];
    if (!order) {
      throw new Error('Missing order');
    }

    const quote = await firstValueFrom(gateway.create(order.id));
    expect(quote).toMatchObject({ number: 1048, version: 1, status: 'Pending', lines: [] });
    expect(quote.customer.facts.fleet).toBe(true);
    expect(quote.customer.consentEmail).toBe(false);
    expect((await failure(gateway.create(order.id))).status).toBe(409);
    expect((await failure(gateway.create('missing'))).status).toBe(404);
  });

  it('loads options with stock, AI suggestions, rules, limits and payment settings', async () => {
    const gateway = await setup(ATTENDANT);
    const id = quoteId(1047);

    const quote = await firstValueFrom(gateway.get(id));
    expect(quote.customer).toMatchObject({ name: 'Joana Prado', maskedPhone: '(11) 9••••-4321' });
    expect(quote.customer.facts.loyal).toBe(true);
    expect(quote.totals.total).toBeGreaterThan(0);
    const options = await firstValueFrom(gateway.options(id));
    expect(options.discountLimitBasisPoints).toBe(1_000);
    expect(options.suggestions.map((item) => item.name)).toEqual([
      'Revisão 40 mil km',
      'Kit freio dianteiro',
      'Vela de ignição',
    ]);
    expect(options.parts.find((part) => part.name === 'Vela de ignição')?.stock).toEqual({
      available: 0,
      leadTimeDays: 2,
      supplier: 'Auto Peças Centro',
    });
    expect(options.parts.find((part) => part.name === 'Aditivo de radiador')?.fractional).toBe(
      true,
    );
    expect(options.rules.find((rule) => !rule.isActive)?.name).toBe('Aniversário da oficina');
    expect(options.payment.credit).toEqual({
      maxInstallments: 10,
      interestFreeInstallments: 3,
      monthlyInterestBasisPoints: 199,
    });
    expect((await failure(gateway.get('missing'))).status).toBe(404);
    expect((await failure(gateway.options('missing'))).status).toBe(404);
  });

  it('saves drafts and validates items and quantities', async () => {
    const gateway = await setup();
    const quote = await firstValueFrom(gateway.get(quoteId(1047)));

    const saved = await firstValueFrom(
      gateway.saveDraft(
        quote.id,
        draftOf(quote, {
          lines: quote.lines.slice(0, 1),
          discountMode: 'Manual',
          manualDiscount: {
            value: { kind: 'Amount', amount: cents(5_000) },
            reason: 'Concorrência',
          },
          payment: { methods: ['Pix'], maxInstallments: 99 },
        }),
      ),
    );
    expect(saved.lines).toHaveLength(1);
    expect(saved.totals).toMatchObject({ discount: 5_000, total: 50_900 });
    expect(saved.payment).toEqual({ methods: ['Pix'], maxInstallments: 10 });
    const first = quote.lines[0];
    if (!first) {
      throw new Error('Missing line');
    }

    const invalid = await failure(
      gateway.saveDraft(quote.id, draftOf(quote, { lines: [{ ...first, quantity: 1.5 }] })),
    );
    expect(invalid.status).toBe(400);
    const unknown = await failure(
      gateway.saveDraft(quote.id, draftOf(quote, { lines: [{ ...first, itemId: 'x' }] })),
    );
    expect(unknown.fieldErrors[0]?.message).toBe(messages.fake.quoteInvalidItem);
    expect((await failure(gateway.saveDraft('missing', draftOf(quote)))).status).toBe(404);
    const approved = await firstValueFrom(gateway.get(quoteId(1040)));
    expect((await failure(gateway.saveDraft(approved.id, draftOf(approved)))).status).toBe(409);
  });

  it('requests a discount approval and lets only managers decide', async () => {
    const gateway = await setup(ATTENDANT);
    const id = quoteId(1047);
    const discount = {
      value: { kind: 'Percent' as const, basisPoints: 1_500 },
      reason: 'Cliente antigo',
    };

    expect(
      (
        await failure(
          gateway.requestDiscountApproval(id, {
            ...discount,
            value: { kind: 'Percent', basisPoints: 500 },
          }),
        )
      ).status,
    ).toBe(422);
    expect(
      (await failure(gateway.requestDiscountApproval(id, { ...discount, reason: 'x' }))).status,
    ).toBe(400);
    const approval = await firstValueFrom(gateway.requestDiscountApproval(id, discount));
    expect(approval).toMatchObject({ status: 'Pending', requestedBy: 'Ana Atendente' });
    expect(harness.state(mode).database.quotes.notifications).toHaveLength(1);
    expect((await failure(gateway.decideDiscountApproval(id, approval.id, true))).status).toBe(403);

    await harness.login(MANAGER);
    const decided = await firstValueFrom(gateway.decideDiscountApproval(id, approval.id, true));
    expect(decided).toMatchObject({ status: 'Approved', decidedBy: 'Gabi Gestora' });
    expect((await failure(gateway.decideDiscountApproval(id, approval.id, false))).status).toBe(
      409,
    );
    expect((await failure(gateway.decideDiscountApproval(id, 'other', false))).status).toBe(404);
  });

  it('sends with an AI message, follows the queue and retries failures', async () => {
    const gateway = await setup(ATTENDANT);
    const orders = await firstValueFrom(gateway.openServiceOrders());
    const carlos = orders.find((order) => order.number === 3323);
    if (!carlos) {
      throw new Error('Missing order');
    }

    const quote = await firstValueFrom(gateway.create(carlos.id));
    const options = await firstValueFrom(gateway.options(quote.id));
    const service = options.services[0];
    if (!service) {
      throw new Error('Missing service');
    }

    const input = { channel: 'WhatsApp' as const, expiresInDays: 7, message: 'Olá' };
    expect((await failure(gateway.send(quote.id, input))).status).toBe(422);
    await firstValueFrom(
      gateway.saveDraft(
        quote.id,
        draftOf(quote, {
          lines: [{ ...service, lineId: 'new', quantity: 1, source: 'Manual' }],
        }),
      ),
    );
    const message = await firstValueFrom(gateway.messageDraft(quote.id, 'WhatsApp'));
    expect(message).toContain('Carlos');
    expect((await failure(gateway.send(quote.id, { ...input, expiresInDays: 2 }))).status).toBe(
      400,
    );

    const result = await firstValueFrom(gateway.send(quote.id, { ...input, message }));
    expect(result.delivery).toMatchObject({ status: 'Queued', channel: 'WhatsApp', attempts: 1 });
    expect(result.whatsAppUrl).toContain('https://wa.me/55');
    expect((await failure(gateway.send(quote.id, input))).status).toBe(409);
    expect((await failure(gateway.retryDelivery(quote.id))).status).toBe(409);
    const failed = await firstValueFrom(gateway.delivery(quote.id));
    expect(failed).toMatchObject({
      status: 'Failed',
      failureReason: messages.fake.quoteDeliveryFailed,
    });

    const retried = await firstValueFrom(gateway.retryDelivery(quote.id));
    expect(retried).toMatchObject({ status: 'Queued', attempts: 2 });
    expect((await firstValueFrom(gateway.delivery(quote.id))).status).toBe('Sent');
    expect((await firstValueFrom(gateway.delivery(quote.id))).status).toBe('Opened');
  });

  it('blocks sending without consent, above the limit or without a delivery', async () => {
    const gateway = await setup(ATTENDANT);
    const id = quoteId(1047);
    const quote = await firstValueFrom(gateway.get(id));

    expect((await failure(gateway.delivery(id))).status).toBe(404);
    await firstValueFrom(
      gateway.saveDraft(
        id,
        draftOf(quote, {
          discountMode: 'Manual',
          manualDiscount: {
            value: { kind: 'Percent', basisPoints: 2_000 },
            reason: 'Concorrência',
          },
        }),
      ),
    );
    const send = { channel: 'Link' as const, expiresInDays: 3, message: 'Segue' };
    expect((await failure(gateway.send(id, send))).detail).toBe(
      messages.fake.quoteApprovalRequired,
    );

    const orders = await firstValueFrom(gateway.openServiceOrders());
    const fleet = await firstValueFrom(gateway.create(orders[0]?.id ?? ''));
    expect((await failure(gateway.send(fleet.id, { ...send, channel: 'Email' }))).detail).toBe(
      messages.fake.quoteNoConsent,
    );
  });

  it('rejects with a reason, creates a new version and lists versions', async () => {
    const gateway = await setup();
    const id = quoteId(1047);

    expect((await failure(gateway.createVersion(id))).status).toBe(409);
    expect((await failure(gateway.reject(id, 'não'))).status).toBe(400);
    const rejected = await firstValueFrom(gateway.reject(id, 'Preço acima do esperado'));
    expect(rejected).toMatchObject({
      status: 'Rejected',
      rejectionReason: 'Preço acima do esperado',
    });
    expect((await failure(gateway.reject(id, 'Outro motivo'))).status).toBe(409);

    const next = await firstValueFrom(gateway.createVersion(id));
    expect(next).toMatchObject({ number: 1047, version: 2, status: 'Pending', delivery: null });
    expect(next.lines).toHaveLength(rejected.lines.length);
    expect((await failure(gateway.createVersion(id))).status).toBe(409);
    const versions = await firstValueFrom(gateway.versions(next.id));
    expect(versions.map((version) => [version.version, version.status])).toEqual([
      [2, 'Pending'],
      [1, 'Rejected'],
    ]);
    expect((await failure(gateway.versions('missing'))).status).toBe(404);
  });

  it('registers the customer approval at the counter', async () => {
    const gateway = await setup();
    const approved = await firstValueFrom(gateway.approve(quoteId(1047)));

    expect(approved.status).toBe('Approved');
    expect((await failure(gateway.approve(quoteId(1047)))).status).toBe(409);
    expect((await failure(gateway.approve('missing'))).status).toBe(404);
  });

  it('regenerates an expired link and keeps mechanics out', async () => {
    const gateway = await setup();
    const id = quoteId(1042, 2);

    const expired = await firstValueFrom(gateway.get(id));
    expect(Date.parse(expired.delivery?.expiresAt ?? '')).toBeLessThan(Date.now());
    expect((await firstValueFrom(gateway.delivery(id))).status).toBe('Sent');
    expect((await firstValueFrom(gateway.retryDelivery(id))).attempts).toBe(2);

    await harness.login(MECHANIC);
    expect((await failure(gateway.list({ status: null, page: 1, pageSize: 20 }))).status).toBe(403);
  });
});
