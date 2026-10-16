import { TestBed } from '@angular/core/testing';
import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { provideTestApi, TestApiMode } from '../../../testing/api';
import { signInWithFakeApi } from '../../../testing/auth';
import { expectNoAxeViolations } from '../../../testing/axe';
import { FakeBackend } from '../../../testing/fake-api/fake-backend.service';
import { messages } from '../../core/i18n/messages.pt-BR';
import { ToastService } from '../../shared/ui/toast/toast';
import { cents, formatBrl } from '../../shared/util/money';
import { provideAuthData } from '../auth/data/auth.providers';
import { provideDashboardData, toSnapshot } from './data/dashboard.gateway';
import {
  DashboardKpi,
  defaultLayout,
  moveCard,
  normalizeLayout,
  toggleCard,
  variation,
} from './data/dashboard.models';
import { DashboardPage, greetingFor, kpiView, targetLink } from './pages/dashboard-page';

const text = messages.dashboard;

function kpi(partial: Partial<DashboardKpi> & Pick<DashboardKpi, 'id'>): DashboardKpi {
  return { value: 100, previous: null, detail: null, ...partial };
}

describe('dashboard models', () => {
  it('normalizes the saved layout, keeping the order and adding missing cards', () => {
    expect(
      normalizeLayout([
        { id: 'yard', visible: false },
        { id: 'yard', visible: true },
        { visible: true },
        { id: 'kpis' },
      ]).cards,
    ).toEqual([
      { id: 'yard', visible: false },
      { id: 'kpis', visible: true },
      { id: 'dailyRevenue', visible: true },
      { id: 'attention', visible: true },
      { id: 'topServices', visible: true },
    ]);
  });

  it('moves and hides cards without leaving the list', () => {
    const layout = defaultLayout();
    expect(moveCard(layout, 'dailyRevenue', -1).cards[0]?.id).toBe('dailyRevenue');
    expect(moveCard(layout, 'kpis', -1)).toBe(layout);
    expect(moveCard(layout, 'yard', 1)).toBe(layout);
    expect(toggleCard(layout, 'yard').cards.at(-1)).toEqual({ id: 'yard', visible: false });
  });

  it('computes the variation against the previous period', () => {
    expect(variation(kpi({ id: 'revenue', value: 110, previous: 100 }))).toBe(10);
    expect(variation(kpi({ id: 'revenue', previous: 0 }))).toBeNull();
    expect(variation(kpi({ id: 'revenue' }))).toBeNull();
  });

  it('formats each indicator with its own unit and trend', () => {
    expect(kpiView(kpi({ id: 'revenue', value: 11_000, previous: 10_000 }))).toEqual({
      label: text.kpis.revenue,
      value: formatBrl(cents(11_000)),
      delta: text.versus(text.percent(10)),
      trend: 'up',
    });
    expect(kpiView(kpi({ id: 'revenue', value: 9_000, previous: 10_000 })).trend).toBe('down');
    expect(kpiView(kpi({ id: 'revenue', value: 9_000 })).trend).toBe('neutral');
    expect(kpiView(kpi({ id: 'approvalRate', value: 70, previous: 72 }))).toMatchObject({
      value: '70%',
      delta: text.versus(text.points(-2)),
      trend: 'down',
    });
    expect(kpiView(kpi({ id: 'approvalRate', value: 70, previous: 70 })).trend).toBe('neutral');
    expect(kpiView(kpi({ id: 'approvalRate', value: 75, previous: 70 })).trend).toBe('up');
    expect(kpiView(kpi({ id: 'approvalRate', value: 70 })).delta).toBeNull();
    expect(kpiView(kpi({ id: 'openOrders', value: 9, detail: 2 })).delta).toBe(
      text.kpiDetail.openOrders(2),
    );
    expect(kpiView(kpi({ id: 'openOrders', value: 9 })).delta).toBeNull();
    expect(kpiView(kpi({ id: 'receivable', value: 500, detail: 1 }))).toMatchObject({
      delta: text.kpiDetail.receivable(1),
      trend: 'down',
    });
    expect(kpiView(kpi({ id: 'receivable', value: 500, detail: 0 })).trend).toBe('neutral');
  });

  it('greets by the time of day and links each kind of target', () => {
    expect(greetingFor(8)).toBe(text.greeting.morning);
    expect(greetingFor(14)).toBe(text.greeting.afternoon);
    expect(greetingFor(20)).toBe(text.greeting.evening);
    expect(targetLink({ kind: 'serviceOrder', id: 'o1' })).toEqual(['/service-orders', 'o1']);
    expect(targetLink({ kind: 'quote', id: 'q1' })).toEqual(['/quotes', 'q1']);
    expect(targetLink({ kind: 'inventory' })).toEqual(['/inventory']);
    expect(targetLink({ kind: 'customers' })).toEqual(['/customers']);
  });

  it('maps a sparse snapshot with safe defaults', () => {
    expect(
      toSnapshot({
        kpis: [{ id: 'revenue' }],
        dailyRevenue: [{ date: '2026-10-01' }],
        topServices: [{ name: 'Troca' }],
        yard: [{ status: 'Received' }],
        attention: [{ id: 'a', kind: 'LowStock', title: 'Peça' }],
      }),
    ).toEqual({
      kpis: [{ id: 'revenue', value: 0, previous: null, detail: null }],
      dailyRevenue: [{ date: '2026-10-01', services: 0, parts: 0 }],
      topServices: [{ name: 'Troca', quantity: 0 }],
      yard: [{ status: 'Received', count: 0 }],
      attention: [
        {
          id: 'a',
          kind: 'LowStock',
          severity: 'info',
          title: 'Peça',
          detail: '',
          action: null,
          target: null,
          aiSuggested: false,
        },
      ],
    });
    expect(toSnapshot({})).toEqual({
      kpis: [],
      dailyRevenue: [],
      topServices: [],
      yard: [],
      attention: [],
    });
  });
});

async function show(mode: TestApiMode = 'mock') {
  const view = await render(DashboardPage, {
    providers: [
      ...provideTestApi(mode, [{ path: '**', children: [] }]),
      provideAuthData(),
      provideDashboardData(),
    ],
    configureTestBed: () => {
      signInWithFakeApi('Manager');
    },
  });
  return { view, user: userEvent.setup() };
}

async function toast(message: string): Promise<void> {
  await vi.waitFor(() =>
    expect(
      TestBed.inject(ToastService)
        .toasts()
        .map((item) => item.message),
    ).toContain(message),
  );
}

function dashboard() {
  return TestBed.inject(FakeBackend).state.database.dashboard;
}

describe('DashboardPage', () => {
  it('shows the indicators, the charts and acts on what needs attention', async () => {
    const { view, user } = await show();
    expect(await screen.findByText(text.kpis.revenue)).toBeTruthy();
    expect(screen.getByTestId('greeting').textContent).toContain('Gabi');
    const open = TestBed.inject(FakeBackend).state.database.serviceOrders.orders.filter(
      (order) => order.status !== 'Delivered',
    ).length;
    expect(screen.getByTestId('greeting').textContent).toContain(text.subtitle(open));
    expect(screen.getAllByText(messages.ui.ai.badge)).toHaveLength(2);
    const attention = screen.getByRole('list', { name: text.attentionLabel });
    expect(within(attention).getByRole('link', { name: text.targets.serviceOrder })).toBeTruthy();

    await user.click(within(attention).getByRole('button', { name: text.actions.SendReminder }));
    await toast(text.actionDone.SendReminder);
    expect(dashboard().actions).toEqual([{ itemId: 'attention-quote', action: 'SendReminder' }]);
    expect(within(attention).queryByText('Orçamento nº 1042 sem resposta')).toBeNull();

    await user.click(screen.getByRole('radio', { name: text.periods.month }));
    expect(await screen.findByText(text.versus(text.percent(12.4)))).toBeTruthy();
    await expectNoAxeViolations(view.container);
  });

  it('lets the user choose and order the cards and saves the preference', async () => {
    const { user } = await show();
    await screen.findByText(text.kpis.revenue);
    await user.click(screen.getByTestId('customize'));
    await user.click(screen.getByRole('checkbox', { name: text.show(text.cards.kpis) }));
    await user.click(screen.getByRole('button', { name: text.moveUp(text.cards.yard) }));
    await user.click(screen.getByRole('button', { name: text.moveDown(text.cards.dailyRevenue) }));
    await user.click(screen.getByRole('button', { name: text.save }));
    await toast(text.saved);
    const saved = Object.values(dashboard().layouts)[0];
    expect(saved?.map((card) => card.id)).toEqual([
      'kpis',
      'attention',
      'dailyRevenue',
      'yard',
      'topServices',
    ]);
    expect(saved?.[0]?.visible).toBe(false);
    expect(screen.queryByText(text.kpis.revenue)).toBeNull();
    expect(document.querySelector('[data-card="attention"]')).toBeTruthy();

    await user.click(screen.getByTestId('customize'));
    await user.click(screen.getByRole('button', { name: text.cancel }));
    expect(screen.queryByRole('button', { name: text.save })).toBeNull();
  });

  it('warns when every card is hidden and when saving fails', async () => {
    const { user } = await show();
    await screen.findByText(text.kpis.revenue);
    await user.click(screen.getByTestId('customize'));
    for (const card of Object.values(text.cards)) {
      await user.click(screen.getByRole('checkbox', { name: text.show(card) }));
    }

    TestBed.inject(FakeBackend).setScenario('dashboard', 'serverError');
    await user.click(screen.getByRole('button', { name: text.save }));
    expect(await screen.findByRole('button', { name: text.save })).toBeTruthy();
    TestBed.inject(FakeBackend).setScenario('dashboard', 'none');
    await user.click(screen.getByRole('button', { name: text.save }));
    expect(await screen.findByText(text.allHidden)).toBeTruthy();
  });

  it('reports a failing action, a failing load and the pending API', async () => {
    const { user } = await show();
    await screen.findByText(text.kpis.revenue);
    const [stock] = dashboard().attention;
    if (!stock) {
      throw new Error('Missing attention item');
    }

    stock.action = 'SendNewDeadline';
    await user.click(screen.getByRole('button', { name: text.actions.CreatePurchaseOrder }));
    await toast(messages.fake.actionNotAllowed);

    TestBed.inject(FakeBackend).setScenario('dashboard', 'serverError');
    await user.click(screen.getByRole('radio', { name: text.periods.week }));
    expect(await screen.findByText(text.loadError)).toBeTruthy();
    TestBed.inject(FakeBackend).setScenario('dashboard', 'none');
    await user.click(screen.getByRole('button', { name: text.retry }));
    expect(await screen.findByText(text.kpis.revenue)).toBeTruthy();

    TestBed.resetTestingModule();
    await show('pending');
    expect(await screen.findByText(messages.pending.dashboard)).toBeTruthy();
  });
});
