import { Type } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { render, RenderComponentOptions, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { provideTestApi, TestApiMode } from '../../../../testing/api';
import { signInWithFakeApi } from '../../../../testing/auth';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { FakeBackend } from '../../../../testing/fake-api/fake-backend.service';
import { BackendRole } from '../../../core/auth/permissions';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { cents } from '../../../shared/util/money';
import { provideAuthData } from '../../auth/data/auth.providers';
import { manualText, toManualValue } from '../components/quote-pricing';
import { provideDocumentsData } from '../../documents/data/documents.gateway';
import { provideQuoteData } from '../data/quote.gateway';
import { Quote } from '../data/quote.models';
import { describeChange } from '../dialogs/compare-versions-dialog';
import { channelAllowed } from '../dialogs/send-quote-dialog';
import {
  deliveryStage,
  formatDiscount,
  optionDetail,
  parseDecimal,
  quoteStage,
  stockLabel,
} from '../quote-display';
import { QUOTE_AUTOSAVE_MS, QUOTE_DELIVERY_POLL_MS } from '../state/quote-editor.store';
import { QuoteEditorPage } from './quote-editor-page';
import { QuotesPage } from './quotes-page';

const text = messages.quotes;

function labelled(label: string): RegExp {
  const escaped = label.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  return new RegExp(`^\\s*${escaped}(\\s*\\*)?\\s*$`);
}

async function show<T>(
  component: Type<T>,
  options: {
    role?: BackendRole;
    mode?: TestApiMode;
    inputs?: RenderComponentOptions<T>['inputs'];
  } = {},
) {
  const view = await render(component, {
    providers: [
      ...provideTestApi(options.mode ?? 'mock', [{ path: '**', children: [] }]),
      provideAuthData(),
      provideQuoteData(),
      provideDocumentsData(),
      { provide: QUOTE_AUTOSAVE_MS, useValue: 0 },
      { provide: QUOTE_DELIVERY_POLL_MS, useValue: 0 },
    ],
    ...(options.inputs ? { inputs: options.inputs } : {}),
    configureTestBed: () => {
      signInWithFakeApi(options.role ?? 'Attendant');
    },
  });
  return { view, user: userEvent.setup() };
}

function quotes() {
  return TestBed.inject(FakeBackend).state.database.quotes;
}

function idOf(number: number, version = 1): string {
  return (
    quotes().quotes.find((quote) => quote.number === number && quote.version === version)?.id ?? ''
  );
}

async function openEditor(number: number, role: BackendRole = 'Attendant', version = 1) {
  const result = await show(QuoteEditorPage, {
    role,
    inputs: { id: '' },
  });
  result.view.fixture.componentRef.setInput('id', idOf(number, version));
  await screen.findByRole('heading', { level: 1, name: text.editor.title(number) });
  return result;
}

describe('quote display helpers', () => {
  it('derives stages, formats and parses values', () => {
    expect(quoteStage('Pending', null)).toBe('Draft');
    expect(quoteStage('Pending', 'Sent')).toBe('Pending');
    expect(quoteStage('Approved', null)).toBe('Approved');
    const delivery = {
      id: 'd',
      channel: 'Link' as const,
      status: 'Sent' as const,
      link: 'l',
      expiresAt: '2026-10-01T00:00:00Z',
      sentAt: null,
      openedAt: null,
      failureReason: null,
      attempts: 1,
    };
    expect(deliveryStage(delivery, Date.parse('2026-10-02T00:00:00Z'))).toBe('Expired');
    expect(
      deliveryStage({ ...delivery, status: 'Opened' }, Date.parse('2026-10-02T00:00:00Z')),
    ).toBe('Opened');
    expect(formatDiscount({ kind: 'Percent', basisPoints: 1_250 })).toBe('12,5%');
    expect(formatDiscount({ kind: 'Amount', amount: cents(3_000) })).toMatch(/R\$\s30,00/);
    expect(parseDecimal('1,5')).toBe(1.5);
    expect(parseDecimal('abc')).toBeNull();
    const option = {
      kind: 'Part' as const,
      itemId: 'p',
      name: 'Vela',
      detail: 'PEC-1',
      unitPrice: cents(100),
      fractional: false,
      stock: { available: 0, leadTimeDays: 2, supplier: null },
    };
    expect(optionDetail(option)).toBe('PEC-1');
    expect(optionDetail({ ...option, kind: 'Service', detail: '30' })).toBe('30 min');
    expect(optionDetail({ ...option, kind: 'Package', detail: '1' })).toBe('1 item');
    expect(optionDetail({ ...option, detail: null })).toBeNull();
    expect(stockLabel(option)).toBe(text.builder.outOfStock(2));
    expect(
      stockLabel({ ...option, stock: { available: 3, leadTimeDays: null, supplier: null } }),
    ).toBe(text.builder.inStock('3'));
    expect(stockLabel({ ...option, stock: null })).toBeNull();
  });

  it('converts manual discount text and describes version changes', () => {
    expect(toManualValue('Percent', '12,5')).toEqual({ kind: 'Percent', basisPoints: 1_250 });
    expect(toManualValue('Percent', 'x')).toEqual({ kind: 'Percent', basisPoints: 0 });
    expect(toManualValue('Amount', '30,50')).toEqual({ kind: 'Amount', amount: 3_050 });
    expect(toManualValue('Amount', 'x')).toEqual({ kind: 'Amount', amount: 0 });
    expect(manualText(null)).toBe('');
    expect(manualText({ kind: 'Percent', basisPoints: 1_250 })).toBe('12,5');
    expect(manualText({ kind: 'Amount', amount: cents(3_050) })).toBe('30,50');
    const line = { kind: 'Service' as const, unitPrice: cents(1_000), quantity: 1 };
    expect(describeChange({ key: 'k', name: 'A', before: null, after: line }).kind).toBe('added');
    expect(describeChange({ key: 'k', name: 'A', before: line, after: null })).toMatchObject({
      kind: 'removed',
      after: '—',
    });
    expect(describeChange({ key: 'k', name: 'A', before: line, after: line }).kind).toBe('changed');
    const quote = { customer: { consentWhatsApp: false, consentEmail: true } } as Quote;
    expect(channelAllowed(quote, 'WhatsApp')).toBe(false);
    expect(channelAllowed(quote, 'Email')).toBe(true);
    expect(channelAllowed(quote, 'Link')).toBe(true);
  });
});

describe('QuotesPage', () => {
  it('lists quotes, filters by status and stays accessible', async () => {
    const { view, user } = await show(QuotesPage);
    const table = await screen.findByRole('table', { name: text.list.caption });
    await vi.waitFor(() => expect(within(table).getAllByRole('row')).toHaveLength(4));

    expect(within(table).getByRole('link', { name: text.list.open(1047) })).toBeVisible();
    expect(within(table).getByText(text.statuses.Draft)).toBeVisible();
    expect(within(table).getByText(text.statuses.Approved)).toBeVisible();
    await expectNoAxeViolations(view.container);

    await user.click(screen.getByRole('radio', { name: text.statuses.Approved }));
    await vi.waitFor(() => expect(within(table).getAllByRole('row')).toHaveLength(2));
    await user.click(screen.getByRole('radio', { name: text.statuses.Rejected }));
    expect(await within(table).findByText(text.list.empty)).toBeVisible();
  });

  it('creates a quote from an open service order', async () => {
    const { user } = await show(QuotesPage);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);

    await user.click(await screen.findByRole('button', { name: text.newQuote }));
    const dialog = await screen.findByRole('dialog', { name: text.create.title });
    await user.click(within(dialog).getByRole('button', { name: text.create.submit }));
    expect(within(dialog).getByTestId('quote-dialog-error')).toHaveTextContent(
      text.create.required,
    );
    await user.click(
      await within(dialog).findByRole('radio', { name: new RegExp(text.create.order(3323)) }),
    );
    await user.click(within(dialog).getByRole('button', { name: text.create.submit }));

    await vi.waitFor(() =>
      expect(navigate).toHaveBeenCalledWith(expect.stringContaining('/quotes/')),
    );
  });

  it('shows the construction notice and load errors', async () => {
    await show(QuotesPage, { mode: 'pending' });
    expect(await screen.findByText(messages.pending.quotes)).toBeVisible();
  });

  it('retries after a server error', async () => {
    const { user } = await show(QuotesPage, {});
    TestBed.inject(FakeBackend).setScenario('quotes', 'serverError');
    await user.click(await screen.findByRole('radio', { name: text.statuses.Pending }));
    expect(await screen.findByText(text.loadError)).toBeVisible();
    TestBed.inject(FakeBackend).setScenario('quotes', 'none');
    await user.click(screen.getByRole('button', { name: text.retry }));
    await vi.waitFor(() => expect(screen.queryByText(text.loadError)).toBeNull());
  });
});

describe('QuoteEditorPage', () => {
  it('builds the quote with catalog tabs, warnings and autosave', async () => {
    const { view, user } = await openEditor(1047);

    expect(screen.getByTestId('stock-warnings')).toHaveTextContent('Vela de ignição: faltam 4');
    expect(screen.getByTestId('price-changes')).toHaveTextContent('Pastilha de freio dianteira');
    expect(screen.getByText(/Ruído na frenagem/)).toBeVisible();
    await expectNoAxeViolations(view.container);

    await user.click(screen.getByRole('button', { name: text.items.updatePrices }));
    await vi.waitFor(() => expect(screen.queryByTestId('price-changes')).toBeNull());
    await user.click(screen.getByRole('tab', { name: text.builder.sections.services }));
    await user.type(screen.getByRole('searchbox', { name: text.builder.search }), 'alinha');
    await user.click(screen.getByRole('button', { name: text.builder.addItem('Alinhamento 3D') }));
    await user.click(screen.getByRole('tab', { name: text.builder.sections.suggestions }));
    await user.clear(screen.getByRole('searchbox', { name: text.builder.search }));
    expect(screen.getByText('Falha de ignição no cilindro 2.')).toBeVisible();
    await user.type(screen.getByRole('searchbox', { name: text.builder.search }), 'xyz');
    expect(screen.getByText(text.builder.noSuggestions)).toBeVisible();

    const quantity = screen.getByRole('textbox', { name: text.items.quantityOf('Alinhamento 3D') });
    await user.clear(quantity);
    await user.type(quantity, '1,5');
    await user.tab();
    expect(await screen.findByText(text.items.invalidQuantity)).toBeVisible();
    expect(screen.getByTestId('send-blocks')).toHaveTextContent(text.editor.blocks.InvalidQuantity);
    expect(screen.getByRole('button', { name: text.editor.send })).toBeDisabled();
    await user.click(screen.getByRole('button', { name: text.items.remove('Alinhamento 3D') }));
    await user.click(screen.getByRole('button', { name: text.items.remove('Vela de ignição') }));
    await vi.waitFor(() => expect(screen.queryByTestId('stock-warnings')).toBeNull());
    await user.click(screen.getByRole('button', { name: text.editor.save }));
    await vi.waitFor(() =>
      expect(screen.getByTestId('save-state')).toHaveTextContent(text.editor.saveStates.saved),
    );
    expect(quotes().quotes.find((quote) => quote.number === 1047)?.items).toHaveLength(2);
  });

  it('applies discounts, payment options and blocks a zero total', async () => {
    const { user } = await openEditor(1047);

    expect(screen.getByText(/Cliente fiel · 5%/)).toBeVisible();
    expect(screen.getByTestId('quote-totals')).toHaveTextContent(
      text.totals.discountRule('Cliente fiel'),
    );
    await user.click(screen.getByRole('button', { name: text.paymentMethods.Cash }));
    await user.selectOptions(
      screen.getByRole('combobox', { name: text.payment.installments }),
      '4',
    );
    expect(screen.getByText(/4x de/)).toBeVisible();
    await user.click(screen.getByRole('button', { name: text.paymentMethods.Credit }));
    expect(screen.queryByRole('combobox', { name: text.payment.installments })).toBeNull();

    await user.click(screen.getByRole('radio', { name: text.discount.modes.Manual }));
    await user.click(screen.getByRole('radio', { name: text.discount.kinds.Amount }));
    await user.type(screen.getByRole('textbox', { name: labelled(text.discount.value) }), '50000');
    await user.type(
      screen.getByRole('textbox', { name: labelled(text.discount.reason) }),
      'Cortesia',
    );
    expect(screen.getByTestId('send-blocks')).toHaveTextContent(text.editor.blocks.ZeroTotal);
    expect(screen.getByTestId('discount-limit')).toBeVisible();
    await user.click(screen.getByRole('radio', { name: text.discount.modes.None }));
    await vi.waitFor(() => expect(screen.queryByTestId('send-blocks')).toBeNull());
  });

  it('asks for approval above the limit and lets the manager decide', async () => {
    const { user } = await openEditor(1047);

    await user.click(screen.getByRole('radio', { name: text.discount.modes.Manual }));
    await user.type(screen.getByRole('textbox', { name: labelled(text.discount.value) }), '15');
    expect(screen.getByTestId('send-blocks')).toHaveTextContent(text.editor.blocks.ReasonRequired);
    await user.type(
      screen.getByRole('textbox', { name: labelled(text.discount.reason) }),
      'Cliente antigo',
    );
    expect(screen.getByTestId('discount-limit')).toHaveTextContent(text.discount.exceeds('10%'));
    await user.click(screen.getByRole('button', { name: text.discount.requestApproval }));
    expect(await screen.findByText(text.discount.approval.Pending('Ana Atendente'))).toBeVisible();
    expect(screen.getByTestId('send-blocks')).toHaveTextContent(text.editor.blocks.ApprovalPending);
    expect(quotes().notifications).toHaveLength(1);
  });

  it('lets a manager approve a pending discount', async () => {
    const approval = {
      id: 'approval',
      status: 'Pending' as const,
      discount: { type: 'Percent' as const, value: 25 },
      reason: 'Cliente antigo',
      requestedBy: 'Ana Atendente',
      requestedAt: '2026-10-01T00:00:00Z',
      decidedBy: null,
    };
    const { view, user } = await show(QuoteEditorPage, { role: 'Manager', inputs: { id: '' } });
    const quote = quotes().quotes.find((item) => item.number === 1047);
    if (quote) {
      quote.discountMode = 'Manual';
      quote.manualDiscount = { type: 'Percent', value: 25, reason: 'Cliente antigo' };
      quote.approval = approval;
    }

    view.fixture.componentRef.setInput('id', idOf(1047));
    await user.click(await screen.findByRole('button', { name: text.discount.approve }));
    expect(await screen.findByText(text.discount.approval.Approved('Gabi Gestora'))).toBeVisible();
  });

  it('sends by WhatsApp with the AI message and follows the queue', async () => {
    const open = vi.spyOn(window, 'open').mockReturnValue(null);
    const { user } = await openEditor(1047);

    await user.click(screen.getByRole('button', { name: text.editor.send }));
    const dialog = await screen.findByRole('dialog', { name: text.send.title(1047) });
    const message = within(dialog).getByRole('textbox', { name: new RegExp(text.send.message) });
    await vi.waitFor(() => expect((message as HTMLTextAreaElement).value).toContain('Joana'));
    expect(within(dialog).getByTestId('send-recipient')).toHaveTextContent('(11) 9••••-4321');
    await user.selectOptions(
      within(dialog).getByRole('combobox', { name: text.send.expiration }),
      '3',
    );
    await user.clear(message);
    await user.click(within(dialog).getByRole('button', { name: text.send.submit }));
    expect(within(dialog).getByTestId('quote-dialog-error')).toHaveTextContent(
      text.send.messageRequired,
    );
    await user.type(message, 'Olá, Joana! Segue o orçamento.');
    await user.click(within(dialog).getByRole('button', { name: text.send.submit }));

    await vi.waitFor(() =>
      expect(open).toHaveBeenCalledWith(
        expect.stringContaining('https://wa.me/55'),
        '_blank',
        'noopener',
      ),
    );
    expect(await screen.findByText(text.deliveryStatuses.Opened)).toBeVisible();
    expect(screen.getByText(text.editor.locked)).toBeVisible();
    expect(
      screen.getByRole('button', { name: text.builder.addItem('Revisão 40 mil km') }),
    ).toBeDisabled();
    await user.click(screen.getByRole('button', { name: text.delivery.refresh }));
    open.mockRestore();
  });

  it('copies the link when sending only the link', async () => {
    const { user } = await openEditor(1047);
    const writeText = vi.spyOn(navigator.clipboard, 'writeText').mockResolvedValue(undefined);

    await user.click(screen.getByRole('button', { name: text.editor.send }));
    const dialog = await screen.findByRole('dialog', { name: text.send.title(1047) });
    await user.click(within(dialog).getByRole('radio', { name: text.channels.Link }));
    expect(within(dialog).getByTestId('send-recipient')).toHaveTextContent(text.send.linkOnly);
    const message = within(dialog).getByRole('textbox', { name: new RegExp(text.send.message) });
    await vi.waitFor(() => expect(message).not.toHaveValue(''));
    await user.click(within(dialog).getByRole('button', { name: text.send.submit }));
    await vi.waitFor(() => expect(writeText).toHaveBeenCalledWith(expect.stringContaining('/q/')));
    await user.click(await screen.findByRole('button', { name: text.delivery.copy }));
    expect(writeText).toHaveBeenCalledTimes(2);
  });

  it('disables channels without consent and retries a failed delivery', async () => {
    const { view, user } = await show(QuoteEditorPage, { inputs: { id: '' } });
    const carlos = quotes().serviceOrders.find((order) => order.number === 3323);
    const fleet = quotes().serviceOrders.find((order) => order.number === 3322);
    const source = quotes().quotes[0];
    if (!carlos || !fleet || !source) {
      throw new Error('Missing seed');
    }

    quotes().quotes.push(
      { ...structuredClone(source), id: 'fleet-quote', number: 2001, serviceOrderId: fleet.id },
      { ...structuredClone(source), id: 'carlos-quote', number: 2002, serviceOrderId: carlos.id },
    );
    view.fixture.componentRef.setInput('id', 'fleet-quote');
    await user.click(await screen.findByRole('button', { name: text.editor.send }));
    let dialog = await screen.findByRole('dialog', { name: text.send.title(2001) });
    expect(
      within(dialog).getByRole('radio', { name: new RegExp(text.channels.Email) }),
    ).toBeDisabled();
    expect(within(dialog).getByText(text.send.noConsent)).toBeVisible();
    await user.click(within(dialog).getByRole('button', { name: text.send.cancel }));

    view.fixture.componentRef.setInput('id', 'carlos-quote');
    await screen.findByRole('heading', { level: 1, name: text.editor.title(2002) });
    await user.click(screen.getByRole('button', { name: text.editor.send }));
    dialog = await screen.findByRole('dialog', { name: text.send.title(2002) });
    await user.click(within(dialog).getByRole('radio', { name: text.channels.Email }));
    const message = within(dialog).getByRole('textbox', { name: new RegExp(text.send.message) });
    await vi.waitFor(() => expect(message).not.toHaveValue(''));
    await user.click(within(dialog).getByRole('button', { name: text.send.submit }));

    expect(await screen.findByText(text.deliveryStatuses.Failed)).toBeVisible();
    expect(screen.getByText(messages.fake.quoteDeliveryFailed)).toBeVisible();
    await user.click(screen.getByRole('button', { name: text.delivery.retry }));
    expect(await screen.findByText(text.deliveryStatuses.Opened)).toBeVisible();
    expect(screen.getByText(text.delivery.channel(text.channels.Email, 2))).toBeVisible();
  });

  it('regenerates an expired link', async () => {
    const { user } = await openEditor(1042, 'Attendant', 2);

    expect(screen.getByText(text.deliveryStatuses.Expired)).toBeVisible();
    await user.click(screen.getByRole('button', { name: text.delivery.regenerate }));
    expect(await screen.findByText(text.delivery.channel(text.channels.Link, 2))).toBeVisible();
    expect(screen.queryByText(text.deliveryStatuses.Expired)).toBeNull();
  });

  it('registers a rejection, creates a version and compares versions', async () => {
    const navigate = vi.fn().mockResolvedValue(true);
    const { user, view } = await openEditor(1047);
    vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockImplementation(navigate);

    await user.click(screen.getByRole('button', { name: text.editor.registerRejection }));
    const dialog = await screen.findByRole('dialog', { name: text.reject.title });
    await user.type(
      within(dialog).getByRole('textbox', { name: new RegExp(text.reject.reason) }),
      'caro',
    );
    await user.click(within(dialog).getByRole('button', { name: text.reject.submit }));
    expect(within(dialog).getByText(text.reject.required)).toBeVisible();
    await user.type(
      within(dialog).getByRole('textbox', { name: new RegExp(text.reject.reason) }),
      ' demais',
    );
    await user.click(within(dialog).getByRole('button', { name: text.reject.submit }));
    expect(await screen.findByText(text.editor.rejected('caro demais'))).toBeVisible();

    await user.click(screen.getByRole('button', { name: text.editor.newVersion }));
    await vi.waitFor(() => expect(navigate).toHaveBeenCalledWith(`/quotes/${idOf(1047, 2)}`));
    view.fixture.componentRef.setInput('id', idOf(1047, 2));
    await screen.findByText(text.editor.version(2));
    await user.click(screen.getByRole('button', { name: text.items.remove('Vela de ignição') }));
    await user.click(screen.getByRole('button', { name: text.editor.save }));
    await vi.waitFor(() =>
      expect(screen.getByTestId('save-state')).toHaveTextContent(text.editor.saveStates.saved),
    );
    await user.click(screen.getByRole('button', { name: text.editor.versions }));
    const versions = await screen.findByRole('dialog', { name: text.versions.title });
    expect(await within(versions).findByText(/Removido · Vela de ignição/)).toBeVisible();
    expect(within(versions).getByTestId('version-delta')).toHaveTextContent('−');
    expect(within(versions).getByText(/caro demais/)).toBeVisible();
    await user.selectOptions(
      within(versions).getByRole('combobox', { name: text.versions.from }),
      idOf(1047, 2),
    );
    expect(within(versions).getByText(text.versions.none)).toBeVisible();
    await user.click(within(versions).getByRole('button', { name: messages.ui.dialog.close }));
  });

  it('registers the approval at the counter and previews the document', async () => {
    const print = vi.spyOn(window, 'print').mockReturnValue(undefined);
    const { user } = await openEditor(1047);

    await user.click(screen.getByRole('button', { name: text.editor.preview }));
    const dialog = await screen.findByRole('dialog', { name: text.document.preview });
    expect(
      within(dialog).getByText(messages.documents.title(messages.documents.kinds.Quote, 1047, 1)),
    ).toBeVisible();
    expect(within(dialog).getByText(messages.documents.draft)).toBeVisible();
    expect(within(dialog).queryByRole('button', { name: messages.documents.download })).toBeNull();
    await user.click(within(dialog).getByRole('button', { name: messages.documents.print }));
    expect(print).toHaveBeenCalled();
    await user.click(within(dialog).getByRole('button', { name: messages.ui.dialog.close }));

    await user.click(screen.getByRole('button', { name: text.editor.registerApproval }));
    expect(await screen.findByText(text.statuses.Approved)).toBeVisible();
    expect(screen.queryByRole('button', { name: text.editor.send })).toBeNull();
    print.mockRestore();
  });

  it('shows the construction notice, missing quotes and load errors', async () => {
    await show(QuoteEditorPage, { mode: 'pending', inputs: { id: 'x' } });
    expect(await screen.findByText(messages.pending.quotes)).toBeVisible();
  });

  it('shows a missing quote and retries a failed load', async () => {
    const { view, user } = await show(QuoteEditorPage, { inputs: { id: 'missing' } });
    expect(await screen.findByText(text.editor.notFound)).toBeVisible();

    TestBed.inject(FakeBackend).setScenario('quotes', 'serverError');
    view.fixture.componentRef.setInput('id', idOf(1047));
    expect(await screen.findByText(text.editor.loadError)).toBeVisible();
    TestBed.inject(FakeBackend).setScenario('quotes', 'none');
    await user.click(screen.getByRole('button', { name: text.retry }));
    expect(
      await screen.findByRole('heading', { level: 1, name: text.editor.title(1047) }),
    ).toBeVisible();
  });
});
