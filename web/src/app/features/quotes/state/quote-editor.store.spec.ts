import { TestBed } from '@angular/core/testing';
import { BackendRole } from '../../../core/auth/permissions';
import { provideTestApi, TestApiMode } from '../../../../testing/api';
import { signInWithFakeApi } from '../../../../testing/auth';
import { FakeBackend } from '../../../../testing/fake-api/fake-backend.service';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { cents } from '../../../shared/util/money';
import { provideAuthData } from '../../auth/data/auth.providers';
import { provideQuoteData } from '../data/quote.gateway';
import { Quote, QuoteDelivery } from '../data/quote.models';
import {
  isDeliveryExpired,
  isQuoteEditable,
  QUOTE_AUTOSAVE_MS,
  QUOTE_DELIVERY_POLL_MS,
  QuoteEditorStore,
} from './quote-editor.store';

function setup(role: BackendRole = 'Admin', mode: TestApiMode = 'mock') {
  TestBed.configureTestingModule({
    providers: [
      ...provideTestApi(mode, [{ path: '**', children: [] }]),
      provideAuthData(),
      provideQuoteData(),
      QuoteEditorStore,
      { provide: QUOTE_AUTOSAVE_MS, useValue: 0 },
      { provide: QUOTE_DELIVERY_POLL_MS, useValue: 0 },
    ],
  });
  signInWithFakeApi(role);
  return TestBed.inject(QuoteEditorStore);
}

function backend() {
  return TestBed.inject(FakeBackend).state.database.quotes;
}

function idOf(number: number, version = 1): string {
  return (
    backend().quotes.find((quote) => quote.number === number && quote.version === version)?.id ?? ''
  );
}

const settle = () => new Promise((resolve) => setTimeout(resolve, 5));

async function until(check: () => boolean): Promise<void> {
  for (let attempt = 0; attempt < 100 && !check(); attempt += 1) {
    await settle();
  }

  expect(check()).toBe(true);
}

describe('editability helpers', () => {
  const delivery = (status: QuoteDelivery['status'], expiresAt: string): QuoteDelivery => ({
    id: 'd',
    channel: 'Link',
    status,
    link: 'l',
    expiresAt,
    sentAt: null,
    openedAt: null,
    failureReason: null,
    attempts: 1,
  });

  it('locks quotes that were answered or sent with a valid link', () => {
    const now = Date.parse('2026-10-10T00:00:00Z');
    const quote = { status: 'Pending', delivery: null } as unknown as Quote;
    expect(isQuoteEditable(quote, now)).toBe(true);
    expect(isQuoteEditable({ ...quote, status: 'Approved' }, now)).toBe(false);
    expect(
      isQuoteEditable({ ...quote, delivery: delivery('Sent', '2026-10-11T00:00:00Z') }, now),
    ).toBe(false);
    expect(
      isQuoteEditable({ ...quote, delivery: delivery('Failed', '2026-10-11T00:00:00Z') }, now),
    ).toBe(true);
    expect(
      isQuoteEditable({ ...quote, delivery: delivery('Opened', '2026-10-09T00:00:00Z') }, now),
    ).toBe(true);
    expect(isDeliveryExpired(delivery('Sent', '2026-10-10T00:00:00Z'), now)).toBe(true);
  });
});

describe('QuoteEditorStore', () => {
  it('loads the quote, options and derived warnings', async () => {
    const store = setup('Attendant');
    await store.load(idOf(1047));

    expect(store.loadState()).toBe('ready');
    expect(store.editable()).toBe(true);
    expect(store.pricing().appliedRule?.name).toBe('Cliente fiel');
    expect(store.stockWarnings()).toEqual([
      expect.objectContaining({ name: 'Vela de ignição', missing: 4, leadTimeDays: 2 }),
    ]);
    expect(store.priceChanges()).toEqual([
      expect.objectContaining({
        name: 'Pastilha de freio dianteira',
        before: 17_900,
        after: 18_900,
      }),
    ]);
    expect(store.installments()).toHaveLength(6);
    expect(store.blocks()).toEqual([]);
  });

  it('reports pending, missing and failing quotes', async () => {
    const pending = setup('Admin', 'pending');
    await pending.load('x');
    expect(pending.loadState()).toBe('pending');

    TestBed.resetTestingModule();
    const store = setup();
    await store.load('missing');
    expect(store.loadState()).toBe('notFound');

    TestBed.inject(FakeBackend).setScenario('quotes', 'serverError');
    await store.load(idOf(1047));
    expect(store.loadState()).toBe('error');
  });

  it('edits lines and saves the draft automatically', async () => {
    const store = setup();
    await store.load(idOf(1047));
    const options = store.options();
    const alignment = options?.services.find((item) => item.name === 'Alinhamento 3D');
    const coolant = options?.parts.find((item) => item.name === 'Aditivo de radiador');
    if (!alignment || !coolant) {
      throw new Error('Missing options');
    }

    store.addOption(alignment);
    store.addOption(alignment);
    store.addOption(coolant, 'Ai');
    expect(store.saveState()).toBe('dirty');
    const coolantLine = store.draft()?.lines.find((line) => line.itemId === coolant.itemId);
    store.setQuantity(coolantLine?.lineId ?? '', 1.5);
    store.applyCatalogPrices();
    const spark = store.draft()?.lines.find((line) => line.name === 'Vela de ignição');
    store.removeLine(spark?.lineId ?? '');
    expect(store.priceChanges()).toEqual([]);

    await until(() => store.saveState() === 'saved');
    const saved = backend().quotes.find((quote) => quote.id === idOf(1047));
    expect(saved?.items.map((item) => [item.name, item.quantity, item.source])).toEqual([
      ['Revisão 40 mil km', 1, 'Manual'],
      ['Pastilha de freio dianteira', 1, 'Manual'],
      ['Alinhamento 3D', 2, 'Manual'],
      ['Aditivo de radiador', 1.5, 'Ai'],
    ]);
    expect(saved?.items[1]?.unitPrice).toBe(189);
    expect(store.draft()?.lines.every((line) => !line.lineId.startsWith('new-'))).toBe(true);
    expect(store.hasInvalidQuantity()).toBe(false);
  });

  it('keeps the draft and shows the error when saving fails', async () => {
    const store = setup();
    await store.load(idOf(1047));
    TestBed.inject(FakeBackend).setScenario('quotes', 'serverError');

    store.setDiscountMode('None');
    await until(() => store.saveState() === 'error');
    expect(store.actionError()).toBe(messages.fake.serverError);
    expect(store.draft()?.discountMode).toBe('None');
    expect(await store.send({ channel: 'Link', expiresInDays: 7, message: 'x' })).toBeNull();
  });

  it('asks for approval above the limit and lets the manager decide', async () => {
    const store = setup('Attendant');
    await store.load(idOf(1047));

    store.setDiscountMode('Manual');
    store.setManualDiscount({
      value: { kind: 'Percent', basisPoints: 1_500 },
      reason: 'Cliente antigo',
    });
    expect(store.pricing().exceedsLimit).toBe(true);
    expect(store.blocks()).toEqual(['ApprovalRequired']);
    expect(await store.requestApproval()).toBe(true);
    expect(store.blocks()).toEqual(['ApprovalPending']);
    expect(backend().notifications).toHaveLength(1);
    expect(store.canApproveDiscount()).toBe(false);
    expect(await store.decideApproval(true)).toBe(false);
    expect(store.actionError()).toBe(messages.fake.forbidden);

    const approval = backend().quotes.find((quote) => quote.id === idOf(1047))?.approval;
    if (approval) {
      approval.status = 'Approved';
    }

    await store.load(idOf(1047));
    expect(store.blocks()).toEqual([]);
  });

  it('lets a manager approve the pending discount', async () => {
    const store = setup('Manager');
    await store.load(idOf(1047));
    store.setDiscountMode('Manual');
    store.setManualDiscount({
      value: { kind: 'Amount', amount: cents(30_000) },
      reason: 'Parceria',
    });

    expect(await store.requestApproval()).toBe(true);
    expect(store.canApproveDiscount()).toBe(true);
    expect(await store.decideApproval(false)).toBe(true);
    expect(store.blocks()).toEqual(['ApprovalRejected']);
  });

  it('sends, follows the queue until opened and locks editing', async () => {
    const store = setup('Attendant');
    await store.load(idOf(1047));

    const message = await store.messageDraft('WhatsApp');
    expect(message).toContain('Joana');
    const result = await store.send({
      channel: 'WhatsApp',
      expiresInDays: 7,
      message: message ?? '',
    });
    expect(result?.whatsAppUrl).toContain('wa.me');
    await until(() => store.quote()?.delivery?.status === 'Opened');
    expect(store.editable()).toBe(false);
    expect(store.blocks()).toContain('Locked');
    store.setDiscountMode('None');
    expect(store.saveState()).not.toBe('dirty');
  });

  it('shows failed deliveries and retries them', async () => {
    const store = setup('Attendant');
    const orders = backend().serviceOrders;
    const carlos = orders.find((order) => order.number === 3323);
    const source = backend().quotes[0];
    if (!source) {
      throw new Error('Missing quote');
    }

    backend().quotes.push({
      ...structuredClone(source),
      id: 'carlos-quote',
      number: 2000,
      serviceOrderId: carlos?.id ?? '',
    });
    await store.load('carlos-quote');

    await store.send({ channel: 'Email', expiresInDays: 3, message: 'Segue o orçamento' });
    await until(() => store.quote()?.delivery?.status === 'Failed');
    expect(store.editable()).toBe(true);
    expect(await store.retryDelivery()).toBe(true);
    await until(() => store.quote()?.delivery?.status === 'Opened');
    expect(store.quote()?.delivery?.attempts).toBe(2);
  });

  it('registers the rejection, creates a version and lists the history', async () => {
    const store = setup();
    await store.load(idOf(1047));

    expect(await store.reject('curto')).toBe(true);
    expect(store.quote()?.status).toBe('Rejected');
    expect(store.editable()).toBe(false);
    const next = await store.createVersion();
    expect(next).toBe(idOf(1047, 2));
    await store.load(next ?? '');
    expect(store.quote()?.version).toBe(2);
    const versions = await store.loadVersions();
    expect(versions.map((version) => version.version)).toEqual([2, 1]);
  });

  it('saves pending changes before comparing versions', async () => {
    const store = setup();
    await store.load(idOf(1047));
    await store.reject('Achou caro');
    await store.load((await store.createVersion()) ?? '');
    const removed = store.draft()?.lines[0];

    store.removeLine(removed?.lineId ?? '');
    expect(store.saveState()).toBe('dirty');
    const versions = await store.loadVersions();

    expect(store.saveState()).toBe('saved');
    expect(versions[0]?.lines.some((line) => line.itemId === removed?.itemId)).toBe(false);
    expect(versions[1]?.lines.some((line) => line.itemId === removed?.itemId)).toBe(true);
  });

  it('reports a short rejection reason and registers counter approvals', async () => {
    const store = setup();
    await store.load(idOf(1047));

    expect(await store.reject('não')).toBe(false);
    expect(store.actionError()).toBe(messages.errors.validation);
    store.dismissError();
    expect(store.actionError()).toBeNull();
    expect(await store.approve()).toBe(true);
    expect(store.quote()?.status).toBe('Approved');
    expect(await store.createVersion()).toBeNull();
  });

  it('refreshes an expired link delivery', async () => {
    const store = setup();
    await store.load(idOf(1042, 2));

    expect(store.editable()).toBe(true);
    await store.refreshDelivery();
    expect(store.quote()?.delivery?.status).toBe('Sent');
    expect(await store.retryDelivery()).toBe(true);
    expect(store.quote()?.delivery?.attempts).toBe(2);
  });

  it('keeps read-only users from editing', async () => {
    const store = setup('Mechanic');
    await store.load(idOf(1047));
    expect(store.loadState()).toBe('error');
    expect(store.editable()).toBe(false);
  });
});
