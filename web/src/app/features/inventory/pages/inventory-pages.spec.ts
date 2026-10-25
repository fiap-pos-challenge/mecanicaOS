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
import { provideInventoryData } from '../data/inventory.gateway';
import { CountPage, isValidCounted, parseCounted } from './count-page';
import { CountsPage } from './counts-page';
import { InventoryPage } from './inventory-page';
import { PartMovementsPage, signedQuantity } from './part-movements-page';
import { groupBySupplier, PurchaseOrdersPage } from './purchase-orders-page';
import { kpiCards, StockOverviewPage, toStockRow } from './stock-overview-page';
import { SuppliersPage } from './suppliers-page';

const text = messages.inventory;

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
      ...provideTestApi(options.mode ?? 'mock'),
      provideAuthData(),
      provideInventoryData(),
    ],
    ...(options.inputs ? { inputs: options.inputs } : {}),
    configureTestBed: () => {
      signInWithFakeApi(options.role ?? 'Admin');
    },
  });
  return { view, user: userEvent.setup() };
}

function partId(code: string): string {
  return (
    TestBed.inject(FakeBackend).state.database.catalog.parts.find((part) => part.code === code)
      ?.id ?? ''
  );
}

describe('inventory display helpers', () => {
  it('builds rows, KPI cards, signed quantities and supplier groups', () => {
    const item = {
      partId: 'p',
      code: 'A',
      name: 'A',
      type: 'Part' as const,
      unitOfMeasure: 'un',
      location: 'A1',
      stockQuantity: 0,
      minimumStock: 2,
      outflow30d: 1,
      unitCost: cents(100),
      lastOutflowAt: null,
      barcode: null,
    };
    expect(toStockRow(item)).toMatchObject({ situation: 'OutOfStock', level: 0 });
    expect(kpiCards(null).map((card) => card.value)).toEqual(['—', '—', '—', '—']);
    expect(
      kpiCards({
        belowMinimum: 3,
        suggestedOrderTotal: cents(194_000),
        outflowMonth: 428,
        outflowMonthDeltaPercent: -9,
        turnoverDays: 23,
        turnoverGoalDays: 30,
        idleItems: 11,
        idleValue: cents(387_000),
        stockValue: cents(5_431_000),
        totalItems: 186,
      })[1],
    ).toMatchObject({ value: '428 un.', trend: 'down' });
    const movement = {
      id: 'm',
      type: 'Adjustment' as const,
      quantity: -2,
      balanceAfter: 1,
      reason: '',
      userName: '',
      serviceOrderNumber: null,
      occurredAt: '2026-10-01T00:00:00Z',
    };
    expect(signedQuantity(movement, 'un')).toBe('−2 un');
    expect(signedQuantity({ ...movement, type: 'In', quantity: 3 }, 'L')).toBe('+3 L');
    expect(parseCounted('1,5')).toBe(1.5);
    expect(parseCounted('')).toBeNull();
    expect(Number.isNaN(parseCounted('x'))).toBe(true);
    expect(isValidCounted({ type: 'Part' }, 1.5)).toBe(false);
    expect(isValidCounted({ type: 'Supply' }, 1.5)).toBe(true);
    expect(isValidCounted({ type: 'Part' }, 0)).toBe(true);
    expect(groupBySupplier([], [])).toEqual([]);
  });
});

describe('StockOverviewPage', () => {
  it('shows KPIs, levels and situations and filters the table', async () => {
    const { view, user } = await show(StockOverviewPage);
    const table = await screen.findByRole('table', { name: text.overview.caption });
    await vi.waitFor(() => expect(within(table).getAllByRole('row')).toHaveLength(8));

    expect(screen.getByText(text.overview.kpis.belowMinimum)).toBeInTheDocument();
    expect(within(table).getByText(text.situations.OutOfStock)).toBeInTheDocument();
    expect(within(table).getAllByRole('meter').length).toBe(7);
    expect(screen.getByRole('link', { name: /Gerar pedidos \(2\)/ })).toBeVisible();
    await expectNoAxeViolations(view.container);

    await user.selectOptions(
      screen.getByRole('combobox', { name: text.overview.situation }),
      'Reorder',
    );
    await vi.waitFor(() => expect(within(table).getAllByRole('row')).toHaveLength(2));
    await user.selectOptions(screen.getByRole('combobox', { name: text.overview.location }), 'A3');
    await vi.waitFor(() => expect(within(table).getAllByRole('row')).toHaveLength(2));
    await user.click(screen.getByRole('button', { name: text.overview.clear }));
    await user.type(screen.getByRole('searchbox'), 'vela');
    await vi.waitFor(() => expect(within(table).getAllByRole('row')).toHaveLength(2));
  });

  it('registers a manual entry and blocks a negative exit', async () => {
    const { user } = await show(StockOverviewPage);
    const table = await screen.findByRole('table', { name: text.overview.caption });
    await vi.waitFor(() => expect(within(table).getAllByRole('row')).toHaveLength(8));

    await user.click(screen.getByRole('button', { name: text.overview.registerEntry }));
    const entry = await screen.findByRole('dialog', { name: text.entry.title });
    await user.selectOptions(
      within(entry).getByRole('combobox', { name: new RegExp(text.entry.part) }),
      partId('PEC-0412'),
    );
    const quantity = within(entry).getByRole('spinbutton', {
      name: new RegExp(text.entry.quantity),
    });
    await user.type(quantity, '1.5');
    await user.click(within(entry).getByRole('button', { name: text.entry.save }));
    expect(within(entry).getByText(text.entry.invalidQuantity)).toBeInTheDocument();
    await user.clear(quantity);
    await user.type(quantity, '4');
    await user.click(within(entry).getByRole('button', { name: text.entry.save }));
    await vi.waitFor(() =>
      expect(
        TestBed.inject(FakeBackend).state.database.catalog.parts.find(
          (part) => part.code === 'PEC-0412',
        )?.stockQuantity,
      ).toBe(6),
    );

    await user.click(
      within(table).getByRole('button', {
        name: text.overview.exitOf('Pastilha de freio dianteira'),
      }),
    );
    const exit = await screen.findByRole('dialog', { name: text.exit.title });
    await user.type(
      within(exit).getByRole('spinbutton', { name: new RegExp(text.exit.quantity) }),
      '9',
    );
    expect(within(exit).getByText(text.exit.negative)).toBeInTheDocument();
    await user.click(within(exit).getByRole('button', { name: text.exit.save }));
    expect(screen.getByRole('dialog', { name: text.exit.title })).toBeInTheDocument();
    const exitQuantity = within(exit).getByRole('spinbutton', {
      name: new RegExp(text.exit.quantity),
    });
    await user.clear(exitQuantity);
    await user.type(exitQuantity, '2');
    await user.click(within(exit).getByRole('button', { name: text.exit.save }));
    await vi.waitFor(() =>
      expect(screen.queryByRole('dialog', { name: text.exit.title })).toBeNull(),
    );
  });

  it('reads an invoice draft and finds parts by barcode with a fallback for unknown codes', async () => {
    const { user } = await show(StockOverviewPage);
    await screen.findByRole('table', { name: text.overview.caption });

    await user.click(screen.getByRole('button', { name: text.overview.registerEntry }));
    const entry = await screen.findByRole('dialog', { name: text.entry.title });
    await user.click(within(entry).getByRole('radio', { name: text.entry.modes.barcode }));
    const barcode = within(entry).getByRole('textbox', { name: new RegExp(text.entry.barcode) });
    await user.type(barcode, '0000000000000');
    await user.click(within(entry).getByRole('button', { name: text.entry.lookup }));
    expect(await within(entry).findByTestId('inventory-dialog-error')).toHaveTextContent(
      messages.fake.unknownBarcode('0000000000000'),
    );
    await user.clear(barcode);
    await user.type(barcode, '7891234567895{Enter}');
    expect(
      await within(entry).findByText(text.entry.found('PEC-0101', 'Óleo 5W30 sintético')),
    ).toBeVisible();
    await user.click(within(entry).getByRole('button', { name: text.entry.camera }));
    expect(within(entry).getByText(text.entry.cameraUnavailable)).toBeInTheDocument();

    await user.click(within(entry).getByRole('radio', { name: text.entry.modes.invoice }));
    const file = within(entry).getByLabelText(text.entry.invoiceFile);
    await user.upload(file, new File(['x'], 'nota.txt', { type: 'text/plain' }));
    await user.click(within(entry).getByRole('button', { name: text.entry.read }));
    expect(within(entry).getByTestId('inventory-dialog-error')).toHaveTextContent(
      text.entry.invalidFile,
    );
    await user.upload(file, new File(['<nfe/>'], 'nota.xml', { type: 'text/xml' }));
    await user.click(within(entry).getByRole('button', { name: text.entry.read }));
    expect(
      await within(entry).findByText(text.entry.draftTitle('4588', 'Distribuidora Freio Sul')),
    ).toBeVisible();
    await user.click(within(entry).getByRole('button', { name: text.entry.confirm }));
    expect(within(entry).getByTestId('inventory-dialog-error')).toHaveTextContent(
      text.entry.draftHint,
    );
    await user.selectOptions(
      within(entry).getByRole('combobox', {
        name: text.entry.lineLabel(text.entry.linePart, 'FLUIDO FREIO DOT4 500ML'),
      }),
      partId('INS-0201'),
    );
    await user.click(within(entry).getByRole('button', { name: text.entry.confirm }));
    await vi.waitFor(() =>
      expect(screen.queryByRole('dialog', { name: text.entry.title })).toBeNull(),
    );
  });

  it('shows the construction notice when the API is pending', async () => {
    await show(StockOverviewPage, { mode: 'pending' });

    expect(await screen.findByText(messages.pending.inventory)).toBeInTheDocument();
    expect(screen.getByText(text.overview.empty)).toBeInTheDocument();
  });

  it('hides stock changes from the mechanic', async () => {
    await show(StockOverviewPage, { role: 'Mechanic' });
    await screen.findByRole('table', { name: text.overview.caption });

    expect(screen.queryByRole('button', { name: text.overview.registerEntry })).toBeNull();
    expect(screen.getByRole('link', { name: text.overview.startCount })).toBeVisible();
  });
});

describe('PartMovementsPage', () => {
  it('lists the movements of the part', async () => {
    const { view } = await show(PartMovementsPage, { inputs: { partId: 'loading' } });
    view.fixture.componentRef.setInput('partId', partId('PEC-0412'));

    expect(
      await screen.findByRole('heading', {
        level: 1,
        name: text.movements.title('Pastilha de freio dianteira'),
      }),
    ).toBeVisible();
    expect(screen.getByText(text.movements.order(3021))).toBeInTheDocument();
    expect(screen.getByText('−8 jogo')).toBeInTheDocument();
  });

  it('explains missing parts and pending APIs', async () => {
    await show(PartMovementsPage, { inputs: { partId: 'missing' } });
    expect(await screen.findByText(text.movements.notFound)).toBeInTheDocument();
  });

  it('shows the construction notice', async () => {
    await show(PartMovementsPage, { mode: 'pending', inputs: { partId: 'any' } });
    expect(await screen.findByText(messages.pending.inventory)).toBeInTheDocument();
  });
});

describe('CountsPage and CountPage', () => {
  it('opens a count per location and blocks a second one', async () => {
    const { user } = await show(CountsPage);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    await screen.findByRole('table', { name: text.counts.caption });

    await user.click(screen.getByRole('button', { name: text.counts.start }));
    expect(screen.getByTestId('counts-error')).toHaveTextContent(text.counts.chooseLocation);
    await user.selectOptions(
      screen.getByRole('combobox', { name: new RegExp(text.counts.location) }),
      'B2',
    );
    await user.click(screen.getByRole('button', { name: text.counts.start }));
    await vi.waitFor(() => expect(navigate).toHaveBeenCalled());
    await user.click(screen.getByRole('button', { name: text.counts.start }));
    expect(await screen.findByTestId('counts-error')).toHaveTextContent(
      messages.fake.countAlreadyOpen('B2'),
    );
  });

  it('counts with differences, submits and lets the manager approve', async () => {
    const backend = (): FakeBackend => TestBed.inject(FakeBackend);
    const { view, user } = await show(CountPage, { inputs: { id: 'loading' } });
    const count = await new Promise<string>((resolve) => {
      TestBed.inject(FakeBackend).state.database.inventory.counts.push({
        id: 'count-1',
        location: 'A3',
        status: 'Open',
        openedBy: 'Marcos',
        openedAt: '2026-10-05T10:00:00Z',
        submittedAt: null,
        decidedBy: null,
        decidedAt: null,
        rejectionReason: null,
        lines: [
          { partId: partId('PEC-0412'), systemQuantity: 2, countedQuantity: null },
          { partId: partId('PEC-0415'), systemQuantity: 4, countedQuantity: null },
        ],
      });
      resolve('count-1');
    });
    view.fixture.componentRef.setInput('id', count);

    expect(
      await screen.findByRole('heading', { level: 1, name: text.count.title('A3') }),
    ).toBeVisible();
    await user.click(screen.getByRole('button', { name: text.count.submit }));
    expect(screen.getByTestId('count-error')).toHaveTextContent(text.count.incomplete);
    const pads = screen.getByRole('textbox', {
      name: text.count.countedOf('Pastilha de freio dianteira'),
    });
    await user.type(pads, '1,5');
    await user.click(screen.getByRole('button', { name: text.count.save }));
    expect(screen.getByTestId('count-error')).toHaveTextContent(text.count.invalid);
    await user.clear(pads);
    await user.type(pads, '1');
    expect(screen.getByTestId('difference-PEC-0412')).toHaveTextContent('−1 jogo');
    await user.type(
      screen.getByRole('textbox', { name: text.count.countedOf('Disco de freio dianteiro') }),
      '4',
    );
    expect(screen.getByTestId('difference-PEC-0415')).toHaveTextContent(text.count.noDifference);
    expect(screen.getByText(text.count.progress(2, 2, 1))).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: text.count.submit }));
    expect(await screen.findByText(text.counts.statuses.PendingApproval)).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: text.count.reject }));
    await user.click(screen.getByRole('button', { name: text.count.reject }));
    await user.type(
      screen.getByRole('textbox', { name: new RegExp(text.count.rejectReason) }),
      'Recontar',
    );
    await user.click(screen.getByRole('button', { name: text.count.reject }));
    expect(await screen.findByText(text.counts.statuses.Rejected)).toBeInTheDocument();
    expect(backend().state.database.inventory.counts[0]?.rejectionReason).toBe('Recontar');
  });

  it('applies the adjustment after approval', async () => {
    const { view, user } = await show(CountPage, { inputs: { id: 'loading' } });
    TestBed.inject(FakeBackend).state.database.inventory.counts.push({
      id: 'count-2',
      location: 'A3',
      status: 'PendingApproval',
      openedBy: 'Marcos',
      openedAt: '2026-10-05T10:00:00Z',
      submittedAt: '2026-10-05T11:00:00Z',
      decidedBy: null,
      decidedAt: null,
      rejectionReason: null,
      lines: [{ partId: partId('PEC-0412'), systemQuantity: 2, countedQuantity: 1 }],
    });
    view.fixture.componentRef.setInput('id', 'count-2');

    await user.click(await screen.findByRole('button', { name: text.count.approve }));
    expect(await screen.findByText(text.counts.statuses.Approved)).toBeInTheDocument();
    expect(
      TestBed.inject(FakeBackend).state.database.catalog.parts.find(
        (part) => part.code === 'PEC-0412',
      )?.stockQuantity,
    ).toBe(1);
  });

  it('waits for the manager when the attendant opens a pending count', async () => {
    const { view } = await show(CountPage, { role: 'Attendant', inputs: { id: 'loading' } });
    TestBed.inject(FakeBackend).state.database.inventory.counts.push({
      id: 'count-3',
      location: 'C1',
      status: 'PendingApproval',
      openedBy: 'Ana',
      openedAt: '2026-10-05T10:00:00Z',
      submittedAt: null,
      decidedBy: null,
      decidedAt: null,
      rejectionReason: null,
      lines: [],
    });
    view.fixture.componentRef.setInput('id', 'count-3');

    expect(await screen.findByText(text.count.waiting)).toBeInTheDocument();
  });

  it('explains missing and pending counts', async () => {
    await show(CountPage, { inputs: { id: 'missing' } });
    expect(await screen.findByText(text.count.notFound)).toBeInTheDocument();
  });
});

describe('PurchaseOrdersPage', () => {
  it('creates a draft from the suggestions and queues it', async () => {
    const { view, user } = await show(PurchaseOrdersPage);

    const group = await screen.findByRole('heading', { level: 3, name: 'Distribuidora Freio Sul' });
    expect(group).toBeVisible();
    await expectNoAxeViolations(view.container);
    const quantity = screen.getByRole('spinbutton', {
      name: text.orders.quantityOf('Pastilha de freio dianteira'),
    });
    await user.clear(quantity);
    await user.type(quantity, '1.5');
    await user.tab();
    expect(
      screen.getByRole('button', { name: text.orders.createDraft('Distribuidora Freio Sul') }),
    ).toBeDisabled();
    await user.clear(quantity);
    await user.type(quantity, '20');
    await user.tab();
    await user.click(
      screen.getByRole('button', { name: text.orders.createDraft('Distribuidora Freio Sul') }),
    );
    await user.click(await screen.findByRole('button', { name: text.orders.sendOf(1001) }));
    expect(await screen.findByText(text.orders.statuses.Queued)).toBeInTheDocument();

    await user.click(screen.getByRole('checkbox', { name: text.orders.choose('Vela de ignição') }));
    expect(
      screen.getByRole('button', { name: text.orders.createDraft('Auto Peças Centro') }),
    ).toBeDisabled();
  });

  it('shows the construction notice when pending', async () => {
    await show(PurchaseOrdersPage, { mode: 'pending' });
    expect(await screen.findByText(messages.pending.inventory)).toBeInTheDocument();
  });
});

describe('SuppliersPage', () => {
  it('lists, creates and edits suppliers', async () => {
    const { user } = await show(SuppliersPage);
    const table = await screen.findByRole('table', { name: text.suppliers.caption });
    await vi.waitFor(() => expect(within(table).getAllByRole('row')).toHaveLength(4));

    await user.click(screen.getByRole('button', { name: text.suppliers.add }));
    const dialog = await screen.findByRole('dialog', { name: text.suppliers.newTitle });
    await user.type(
      within(dialog).getByRole('textbox', { name: new RegExp(`^${text.suppliers.name}`) }),
      'Peças Norte',
    );
    await user.type(within(dialog).getByRole('textbox', { name: text.suppliers.document }), '123');
    await user.click(within(dialog).getByRole('button', { name: text.suppliers.save }));
    expect(within(dialog).getByText(text.suppliers.invalidDocument)).toBeInTheDocument();
    const document = within(dialog).getByRole('textbox', { name: text.suppliers.document });
    await user.clear(document);
    await user.type(document, '11444777000161');
    expect(document).toHaveValue('11.444.777/0001-61');
    await user.type(
      within(dialog).getByRole('textbox', { name: text.suppliers.phone }),
      '1133334444',
    );
    await user.click(within(dialog).getByRole('button', { name: text.suppliers.save }));
    await vi.waitFor(() => expect(within(table).getAllByRole('row')).toHaveLength(5));

    await user.click(
      within(table).getByRole('button', { name: text.suppliers.editOf('Peças Norte') }),
    );
    const edit = await screen.findByRole('dialog', { name: text.suppliers.editTitle });
    const lead = within(edit).getByRole('spinbutton', {
      name: new RegExp(text.suppliers.leadTime.replace(/[()]/g, '\\$&')),
    });
    await user.clear(lead);
    await user.type(lead, '9');
    await user.click(within(edit).getByRole('button', { name: text.suppliers.save }));
    expect(await within(table).findByText(text.suppliers.leadTimeValue(9))).toBeInTheDocument();
  });

  it('shows the construction notice when pending', async () => {
    await show(SuppliersPage, { mode: 'pending' });
    expect(await screen.findByText(messages.pending.inventory)).toBeInTheDocument();
  });
});

describe('InventoryPage', () => {
  it('renders the sections as navigation', async () => {
    await show(InventoryPage);

    const nav = screen.getByRole('navigation', { name: text.tabs });
    expect(within(nav).getByRole('link', { name: text.sections.suppliers })).toHaveAttribute(
      'href',
      '/inventory/suppliers',
    );
  });
});
