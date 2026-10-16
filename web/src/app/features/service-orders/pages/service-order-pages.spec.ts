import { signal, Type } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { render, RenderComponentOptions, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { Subject } from 'rxjs';
import { provideTestApi, TestApiMode } from '../../../../testing/api';
import { signInWithFakeApi } from '../../../../testing/auth';
import { expectNoAxeViolations } from '../../../../testing/axe';
import { FakeBackend } from '../../../../testing/fake-api/fake-backend.service';
import { moveOrder } from '../../../../testing/fake-api/service-orders/service-orders.fake-api';
import { BackendRole } from '../../../core/auth/permissions';
import { messages } from '../../../core/i18n/messages.pt-BR';
import {
  RealtimeEvent,
  RealtimeGateway,
  RealtimeStatus,
} from '../../../core/realtime/realtime.gateway';
import { provideAuthData } from '../../auth/data/auth.providers';
import { BoardView } from '../components/board-view';
import { provideDocumentsData } from '../../documents/data/documents.gateway';
import { provideServiceOrderData } from '../data/service-order.gateway';
import { fieldValue, formatDay, formatWhen } from '../service-order-display';
import { BoardStore, SERVICE_ORDER_POLL_MS } from '../state/board.store';
import { ServiceOrderDetailPage } from './service-order-detail-page';
import { ServiceOrdersPage } from './service-orders-page';

const text = messages.serviceOrders;

class FakeRealtime extends RealtimeGateway {
  readonly events = new Subject<RealtimeEvent>();
  readonly status = signal<RealtimeStatus>('connected');
  readonly events$ = this.events.asObservable();
  async connect(): Promise<void> {
    return Promise.resolve();
  }

  async disconnect(): Promise<void> {
    return Promise.resolve();
  }
}

async function show<T>(
  component: Type<T>,
  options: {
    role?: BackendRole;
    mode?: TestApiMode;
    failing?: boolean;
    inputs?: RenderComponentOptions<T>['inputs'];
  } = {},
) {
  const realtime = new FakeRealtime();
  const view = await render(component, {
    providers: [
      ...provideTestApi(options.mode ?? 'mock', [{ path: '**', children: [] }]),
      provideAuthData(),
      provideServiceOrderData(),
      provideDocumentsData(),
      { provide: RealtimeGateway, useValue: realtime },
      { provide: SERVICE_ORDER_POLL_MS, useValue: 60_000 },
    ],
    ...(options.inputs ? { inputs: options.inputs } : {}),
    configureTestBed: () => {
      signInWithFakeApi(options.role ?? 'Attendant');
      if (options.failing) {
        TestBed.inject(FakeBackend).setScenario('serviceOrders', 'serverError');
      }
    },
  });
  return { view, realtime, user: userEvent.setup() };
}

function order(number: number) {
  const found = TestBed.inject(FakeBackend).state.database.serviceOrders.orders.find(
    (item) => item.number === number,
  );
  if (!found) {
    throw new Error('Missing order');
  }

  return found;
}

function column(status: keyof typeof text.statuses): HTMLElement {
  const found = document.querySelector<HTMLElement>(`section[data-status="${status}"]`);
  if (!found) {
    throw new Error('Missing column');
  }

  return found;
}

describe('service order display helpers', () => {
  it('formats dates, days and changed values', () => {
    expect(formatWhen(null)).toBe('—');
    expect(formatDay('2026-10-15')).toMatch(/15\/10/);
    const card = {
      status: 'Received',
      mechanicName: null,
      promisedAt: null,
      total: 0,
    } as unknown as Parameters<typeof fieldValue>[0];
    expect(fieldValue(card, 'status')).toBe(text.statuses.Received);
    expect(fieldValue(card, 'mechanicName')).toBe(text.unassigned);
    expect(fieldValue(card, 'promisedAt')).toBe('—');
    expect(fieldValue(card, 'total')).toMatch(/R\$/);
  });
});

describe('BoardView', () => {
  it('emits a move only when the card changes column', async () => {
    const view = await render(BoardView, {
      inputs: { cards: [] },
      providers: [provideTestApi('mock')],
    });
    const board = view.fixture.componentInstance as unknown as {
      drop(event: unknown): void;
      moved: { subscribe(fn: (value: unknown) => void): void };
    };
    const emitted: unknown[] = [];
    board.moved.subscribe((value) => emitted.push(value));
    const container = { data: 'Received' };
    board.drop({ previousContainer: container, container, item: { data: { id: 'a' } } });
    board.drop({
      previousContainer: container,
      container: { data: 'InDiagnosis' },
      item: { data: { id: 'a' } },
    });
    expect(emitted).toEqual([{ card: { id: 'a' }, to: 'InDiagnosis' }]);
  });
});

describe('ServiceOrdersPage', () => {
  it('shows the six columns with tags and moves a card with the keyboard menu', async () => {
    const { view, user } = await show(ServiceOrdersPage);
    await screen.findByRole('group', { name: text.board.label });

    for (const status of Object.keys(text.statuses) as (keyof typeof text.statuses)[]) {
      expect(column(status)).toBeVisible();
    }

    expect(within(column('InProgress')).getByText(text.tags.Late)).toBeVisible();
    expect(within(column('InProgress')).getByText(text.tags.MissingPart)).toBeVisible();
    expect(within(column('WaitingApproval')).getByText(text.tags.ReminderSent)).toBeVisible();
    await expectNoAxeViolations(view.container);

    await user.click(screen.getByRole('button', { name: text.board.moveOf(3326) }));
    const blocked = screen.getByRole('button', {
      name: text.board.moveTo(text.statuses.Completed),
    });
    expect(blocked).toBeDisabled();
    expect(blocked).toHaveAccessibleDescription(text.board.blocked.NotAllowed);
    await user.keyboard('{Escape}');
    expect(
      screen.queryByRole('button', { name: text.board.moveTo(text.statuses.Completed) }),
    ).toBeNull();
    await user.click(screen.getByRole('button', { name: text.board.moveOf(3326) }));
    await user.click(
      screen.getByRole('button', { name: text.board.moveTo(text.statuses.InDiagnosis) }),
    );
    await vi.waitFor(() =>
      expect(within(column('InDiagnosis')).getByText(text.number(3326))).toBeVisible(),
    );
  });

  it('explains blocked drops and asks a reason to send the order back to diagnosis', async () => {
    const { view, user } = await show(ServiceOrdersPage);
    await screen.findByRole('group', { name: text.board.label });
    const board = view.debugElement.query((node) => node.componentInstance instanceof BoardView)
      .componentInstance as BoardView;
    const store = view.debugElement.injector.get(BoardStore);
    const card = (number: number) => {
      const found = store.cards().find((item) => item.number === number);
      if (!found) {
        throw new Error('Missing card');
      }

      return found;
    };

    board.moved.emit({ card: card(3327), to: 'WaitingApproval' });
    expect(await screen.findByTestId('move-blocked')).toHaveTextContent(
      text.board.blocked.NeedsQuote,
    );

    board.moved.emit({ card: card(3324), to: 'InDiagnosis' });
    const dialog = await screen.findByRole('dialog', { name: text.reason.title });
    await user.click(within(dialog).getByRole('button', { name: text.reason.submit }));
    expect(within(dialog).getByText(text.reason.required)).toBeVisible();
    await user.type(within(dialog).getByRole('textbox'), 'Cliente achou caro');
    await user.click(within(dialog).getByRole('button', { name: text.reason.submit }));
    await vi.waitFor(() => expect(order(3324).status).toBe('InDiagnosis'));

    board.moved.emit({ card: card(3325), to: 'InDiagnosis' });
    await user.click(await screen.findByRole('button', { name: text.reason.cancel }));
    expect(order(3325).status).toBe('WaitingApproval');
  });

  it('shows a conflict and a realtime change with what changed', async () => {
    const { realtime, user } = await show(ServiceOrdersPage);
    await screen.findByRole('group', { name: text.board.label });

    moveOrder(order(3326), 'InDiagnosis', 'Gabi Gestora', null);
    await user.click(screen.getByRole('button', { name: text.board.moveOf(3326) }));
    await user.click(
      screen.getByRole('button', { name: text.board.moveTo(text.statuses.InDiagnosis) }),
    );
    const conflict = await screen.findByText(text.changes.conflict(3326));
    expect(conflict.closest('[data-testid="order-change"]')).toHaveTextContent(
      `${text.changes.fields.status}: ${text.statuses.Received} → ${text.statuses.InDiagnosis}`,
    );
    await user.click(screen.getByRole('button', { name: text.changes.dismiss }));

    moveOrder(order(3319), 'Completed', 'Gabi Gestora', null);
    realtime.events.next({
      type: 'serviceOrderStatusChanged',
      serviceOrderId: order(3319).id,
      number: 3319,
      status: 'Completed',
      occurredAt: new Date().toISOString(),
    });
    expect(await screen.findByText(text.changes.remote(3319))).toBeVisible();
  });

  it('filters the list and shows the agenda', async () => {
    const { user, view } = await show(ServiceOrdersPage);
    await screen.findByRole('group', { name: text.board.label });

    await user.click(screen.getByRole('radio', { name: text.views.list }));
    const table = screen.getByRole('table', { name: text.list.caption });
    expect(within(table).getAllByRole('row')).toHaveLength(11);
    await user.selectOptions(
      screen.getByRole('combobox', { name: text.filters.status }),
      'InProgress',
    );
    expect(within(table).getAllByRole('row')).toHaveLength(4);
    await user.selectOptions(
      screen.getByRole('combobox', { name: text.filters.mechanic }),
      'Marcos Mecânico',
    );
    expect(within(table).getAllByRole('row')).toHaveLength(3);
    await user.click(screen.getByRole('switch', { name: text.filters.lateOnly }));
    expect(within(table).getAllByRole('row')).toHaveLength(2);
    await user.click(screen.getByRole('button', { name: text.filters.clear }));
    await user.type(screen.getByRole('searchbox', { name: text.filters.search }), 'zzz');
    expect(within(table).getByText(text.list.empty)).toBeVisible();
    await user.clear(screen.getByRole('searchbox', { name: text.filters.search }));
    await user.selectOptions(screen.getByRole('combobox', { name: text.filters.status }), '');
    await expectNoAxeViolations(view.container);

    await user.click(screen.getByRole('radio', { name: text.views.agenda }));
    expect(screen.getByRole('heading', { name: text.agenda.overdue })).toBeVisible();
    expect(
      within(screen.getByRole('region', { name: text.agenda.caption })).getAllByRole('article')
        .length,
    ).toBeGreaterThan(3);
    await user.type(screen.getByRole('searchbox', { name: text.filters.search }), 'zzz');
    expect(screen.getByText(text.agenda.empty)).toBeVisible();
  });

  it('hides moves from mechanics', async () => {
    await show(ServiceOrdersPage, { role: 'Mechanic' });
    await screen.findByRole('group', { name: text.board.label });
    expect(screen.queryByRole('button', { name: text.board.moveOf(3326) })).toBeNull();
  });

  it('shows pending and error states', async () => {
    await show(ServiceOrdersPage, { mode: 'pending' });
    expect(await screen.findByText(messages.pending.serviceOrders)).toBeVisible();
  });

  it('retries after a load error', async () => {
    const { user } = await show(ServiceOrdersPage, { failing: true });
    expect(await screen.findByText(text.loadError)).toBeVisible();
    TestBed.inject(FakeBackend).setScenario('serviceOrders', 'none');
    await user.click(screen.getByRole('button', { name: text.retry }));
    expect(await screen.findByRole('group', { name: text.board.label })).toBeVisible();
  });
});

describe('ServiceOrderDetailPage', () => {
  async function openDetail(number: number, role: BackendRole = 'Attendant') {
    const result = await show(ServiceOrderDetailPage, { role, inputs: { id: '' } });
    result.view.fixture.componentRef.setInput('id', order(number).id);
    await screen.findByRole('heading', { level: 1, name: text.detail.title(number) });
    return result;
  }

  it('shows timeline, quote, parts, photos, time and documents', async () => {
    const { view } = await openDetail(3315);

    expect(screen.getByText(text.tags.MissingPart)).toBeVisible();
    expect(screen.getByText(text.detail.missing(3))).toBeVisible();
    expect(screen.getByText(text.detail.reserved)).toBeVisible();
    expect(screen.getAllByText(text.statuses.InProgress).length).toBeGreaterThan(0);
    expect(screen.getByText(text.detail.noDocuments)).toBeVisible();
    await expectNoAxeViolations(view.container);
  });

  it('moves the order, asks the reason and records time', async () => {
    const { user, view } = await openDetail(3324, 'Manager');

    expect(screen.getByAltText('Pastilha gasta')).toBeVisible();
    expect(screen.getByTestId('order-documents')).toHaveTextContent('ORC1-3324-A7KD');
    await user.click(
      screen.getByRole('button', {
        name: text.detail.openDocument(text.detail.documentKinds.Quote, 'ORC1-3324-A7KD'),
      }),
    );
    const document = await screen.findByRole('dialog', { name: messages.documents.preview });
    expect(await within(document).findByTestId('verification-code')).toHaveTextContent(
      'ORC1-3324-A7KD',
    );
    expect(
      within(document).getByRole('button', { name: messages.documents.sendWhatsApp }),
    ).toBeVisible();
    await user.click(within(document).getByRole('button', { name: messages.ui.dialog.close }));
    await user.click(
      screen.getByRole('button', { name: text.board.moveTo(text.statuses.InDiagnosis) }),
    );
    const dialog = await screen.findByRole('dialog', { name: text.reason.title });
    await user.type(within(dialog).getByRole('textbox'), 'Cliente pediu para rever');
    await user.click(within(dialog).getByRole('button', { name: text.reason.submit }));
    expect(await screen.findByText('Cliente pediu para rever')).toBeVisible();

    view.fixture.componentRef.setInput('id', order(3319).id);
    await screen.findByRole('heading', { level: 1, name: text.detail.title(3319) });
    await user.click(screen.getByRole('button', { name: text.detail.startTime }));
    expect(await screen.findByRole('button', { name: text.detail.stopTime })).toBeVisible();
    await user.click(screen.getByRole('button', { name: text.detail.stopTime }));
    expect(await screen.findByRole('button', { name: text.detail.startTime })).toBeVisible();
    await user.click(
      screen.getByRole('button', { name: text.board.moveTo(text.statuses.Completed) }),
    );
    await vi.waitFor(() => expect(order(3319).status).toBe('Completed'));
  });

  it('reloads after a conflict or a realtime change and shows server errors', async () => {
    const { user, realtime } = await openDetail(3326);

    moveOrder(order(3326), 'InDiagnosis', 'Gabi Gestora', null);
    await user.click(
      screen.getByRole('button', { name: text.board.moveTo(text.statuses.InDiagnosis) }),
    );
    expect(await screen.findByText(text.changes.conflict(3326))).toBeVisible();
    await user.click(screen.getByRole('button', { name: text.changes.dismiss }));

    moveOrder(order(3326), 'WaitingApproval', 'Gabi Gestora', null);
    realtime.events.next({
      type: 'serviceOrderStatusChanged',
      serviceOrderId: order(3326).id,
      number: 3326,
      status: 'WaitingApproval',
      occurredAt: new Date().toISOString(),
    });
    expect(await screen.findByText(text.changes.remote(3326))).toBeVisible();

    TestBed.inject(FakeBackend).setScenario('serviceOrders', 'unprocessable');
    await user.click(
      screen.getByRole('button', { name: text.board.moveTo(text.statuses.InProgress) }),
    );
    expect(await screen.findByTestId('detail-error')).toBeVisible();
  });

  it('shows not found, errors and the construction notice', async () => {
    const { view, user } = await show(ServiceOrderDetailPage, { inputs: { id: 'missing' } });
    expect(await screen.findByText(text.detail.notFound)).toBeVisible();

    TestBed.inject(FakeBackend).setScenario('serviceOrders', 'serverError');
    view.fixture.componentRef.setInput('id', order(3326).id);
    expect(await screen.findByText(text.detail.loadError)).toBeVisible();
    TestBed.inject(FakeBackend).setScenario('serviceOrders', 'none');
    await user.click(screen.getByRole('button', { name: text.retry }));
    expect(
      await screen.findByRole('heading', { level: 1, name: text.detail.title(3326) }),
    ).toBeVisible();
  });

  it('shows the construction notice', async () => {
    await show(ServiceOrderDetailPage, { mode: 'pending', inputs: { id: 'x' } });
    expect(await screen.findByText(messages.pending.serviceOrders)).toBeVisible();
  });
});
