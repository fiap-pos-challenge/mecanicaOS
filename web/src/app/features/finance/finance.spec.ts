import { Type } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { firstValueFrom } from 'rxjs';
import { provideTestApi, TestApiMode } from '../../../testing/api';
import { signInWithFakeApi } from '../../../testing/auth';
import { expectNoAxeViolations } from '../../../testing/axe';
import { FakeBackend } from '../../../testing/fake-api/fake-backend.service';
import { BackendRole } from '../../core/auth/permissions';
import { messages } from '../../core/i18n/messages.pt-BR';
import { provideAuthData } from '../auth/data/auth.providers';
import {
  FinanceGateway,
  provideFinanceData,
  toFinanceSummary,
  toPlatform,
} from './data/finance.gateway';
import { FinancePage } from './pages/finance-page';
import { PlatformPage } from './pages/platform-page';

const text = messages.finance;
const platform = messages.platform;

async function show<T>(
  component: Type<T>,
  options: { mode?: TestApiMode; role?: BackendRole; failing?: boolean } = {},
) {
  const view = await render(component, {
    providers: [
      ...provideTestApi(options.mode ?? 'mock', [{ path: '**', children: [] }]),
      provideAuthData(),
    ],
    configureTestBed: () => {
      signInWithFakeApi(options.role ?? 'Admin');
      if (options.failing) {
        TestBed.inject(FakeBackend).setScenario('finance', 'serverError');
      }
    },
  });
  return { view, user: userEvent.setup() };
}

describe('FinancePage', () => {
  it('shows received, receivable, commissions and the tables', async () => {
    const { view, user } = await show(FinancePage);

    expect(
      await screen.findByRole('heading', { level: 1, name: messages.screens.finance }),
    ).toBeInTheDocument();
    expect(await screen.findByText(text.kpis.received)).toBeInTheDocument();
    expect(screen.getByText(text.receivableCount(1))).toBeInTheDocument();
    const receivables = screen.getByRole('table', { name: text.receivables });
    expect(
      within(receivables).getByRole('link', { name: text.openOrder(3310) }),
    ).toBeInTheDocument();
    expect(screen.getByRole('table', { name: text.receipts })).toBeInTheDocument();
    expect(screen.getByRole('table', { name: text.commissions })).toBeInTheDocument();
    await expectNoAxeViolations(view.container);

    await user.click(screen.getByRole('radio', { name: text.periods.today }));
    expect(await screen.findByText(text.kpis.averageTicket)).toBeInTheDocument();
  });

  it('reports load errors and retries', async () => {
    const { user } = await show(FinancePage, { failing: true });

    expect(await screen.findByRole('alert')).toHaveTextContent(text.loadError);
    TestBed.inject(FakeBackend).setScenario('finance', 'none');
    await user.click(screen.getByRole('button', { name: text.retry }));
    expect(await screen.findByText(text.kpis.received)).toBeInTheDocument();
  });

  it('shows the pending notice when the API is not published', async () => {
    await show(FinancePage, { mode: 'pending' });

    expect(await screen.findByText(messages.errors.featurePending)).toBeInTheDocument();
  });

  it('maps empty summaries with defaults', async () => {
    expect(
      toFinanceSummary({ period: 'week', from: '2026-10-01', to: '2026-10-07' }).kpis.received,
    ).toBe(0);
    expect(
      toPlatform({ plan: { name: 'Essencial', renewsAt: '2026-12-01T00:00:00Z' } }).usage.vehicles,
    ).toBe(0);
    TestBed.configureTestingModule({
      providers: [...provideTestApi('mock'), provideAuthData(), provideFinanceData()],
    });
    signInWithFakeApi('Manager');
    const summary = await firstValueFrom(TestBed.inject(FinanceGateway).summary('quarter'));
    expect(summary.period).toBe('quarter');
  });
});

describe('PlatformPage', () => {
  it('shows the plan, the usage and the modules', async () => {
    const { view } = await show(PlatformPage);

    expect(
      await screen.findByRole('heading', { level: 1, name: messages.screens.platform }),
    ).toBeInTheDocument();
    expect(await screen.findByText(platform.planName('Essencial'))).toBeInTheDocument();
    expect(screen.getByText(platform.usageItems.customers)).toBeInTheDocument();
    expect(screen.getByText('Notas fiscais')).toBeInTheDocument();
    expect(screen.getByText(platform.disabled)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: platform.settings })).toHaveAttribute(
      'href',
      '/settings',
    );
    await expectNoAxeViolations(view.container);
  });

  it('warns near the user limit and reports errors', async () => {
    const { user } = await show(PlatformPage, { failing: true });

    expect(await screen.findByRole('alert')).toHaveTextContent(platform.loadError);
    const backend = TestBed.inject(FakeBackend);
    backend.setScenario('finance', 'none');
    backend.state.database.settings.workshop.plan.maxUsers = 7;
    await user.click(screen.getByRole('button', { name: platform.retry }));
    expect(await screen.findByRole('status')).toHaveTextContent(platform.nearLimit);
  });

  it('shows the pending notice when the API is not published', async () => {
    await show(PlatformPage, { mode: 'pending' });

    expect(await screen.findByText(messages.errors.featurePending)).toBeInTheDocument();
  });
});
