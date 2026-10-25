import { expect, Page, test } from '@playwright/test';
import { messages } from '../src/app/core/i18n/messages.pt-BR';
import { FakeBackendState } from '../src/testing/fake-api/fake-api/fake-backend';
import { ACCOUNTS } from './accounts';
import { interceptFakeApis } from './fake-backend';
import {
  expectNoHorizontalScroll,
  expectNoSeriousAxeViolations,
  loginAs,
  mainNavigation,
  trackPolicyErrors,
} from './support';

const board = messages.dashboard;
const text = messages.reports;

function field(page: Page, label: string) {
  return page.getByRole('combobox', { name: new RegExp(`^\\s*${label}`) });
}

async function open(page: Page, screen: string): Promise<void> {
  const nav = await mainNavigation(page);
  await nav.getByRole('link', { name: screen }).click();
  await expect(page.getByRole('heading', { level: 1, name: screen })).toBeVisible();
}

test.describe('dashboard and reports with the contract API intercepted', () => {
  test.describe.configure({ timeout: 120_000 });

  let state: FakeBackendState;

  test.beforeEach(async ({ page }) => {
    state = await interceptFakeApis(page);
  });

  test('customizes the dashboard and acts on what needs attention', async ({ page }) => {
    const policy = trackPolicyErrors(page);
    await loginAs(page, ACCOUNTS.admin);
    await expect(
      page.getByRole('heading', { level: 1, name: messages.screens.dashboard }),
    ).toBeVisible();
    await expect(
      page.locator('[data-card="kpis"]').getByText(board.kpis.revenue, { exact: true }),
    ).toBeVisible();
    await expect(page.locator('[data-card="dailyRevenue"] canvas')).toBeVisible();
    await expect(page.locator('[data-card="yard"] canvas')).toBeVisible();
    await expect(page.locator('[data-card="attention"] [data-ai="true"]')).toHaveCount(2);

    const attention = page.getByRole('list', { name: board.attentionLabel });
    await attention.getByRole('button', { name: board.actions.SendReminder }).click();
    await expect(page.getByText(board.actionDone.SendReminder)).toBeVisible();
    expect(state.database.dashboard.actions).toEqual([
      { itemId: 'attention-quote', action: 'SendReminder' },
    ]);

    await page.getByRole('radio', { name: board.periods.month }).click();
    await expect(page.getByText(board.versus(board.percent(12.4)))).toBeVisible();

    await page.getByTestId('customize').click();
    await page.getByRole('checkbox', { name: board.show(board.cards.kpis) }).uncheck();
    await page.getByRole('button', { name: board.moveUp(board.cards.yard) }).click();
    await page.getByRole('button', { name: board.save }).click();
    await expect(page.getByText(board.saved)).toBeVisible();
    await expect(
      page.locator('[data-card="kpis"]').getByText(board.kpis.revenue, { exact: true }),
    ).toBeHidden();

    await open(page, messages.screens.serviceOrders);
    await open(page, messages.screens.dashboard);
    await expect(page.locator('[data-card="yard"]')).toBeVisible();
    await expect(
      page.locator('[data-card="kpis"]').getByText(board.kpis.revenue, { exact: true }),
    ).toBeHidden();
    const cards = await page
      .locator('[data-card]')
      .evaluateAll((items) => items.map((item) => item.getAttribute('data-card')));
    expect(cards).toEqual(['dailyRevenue', 'attention', 'yard', 'topServices']);

    await expectNoHorizontalScroll(page);
    await expectNoSeriousAxeViolations(page);
    expect(policy).toEqual([]);
  });

  test('builds, saves, schedules and exports reports', async ({ page }) => {
    const policy = trackPolicyErrors(page);
    await loginAs(page, ACCOUNTS.admin);
    await open(page, messages.screens.reports);
    await expect(
      page.getByRole('heading', { name: text.title('Faturamento', 'Mês') }),
    ).toBeVisible();
    await expect(page.locator('ui-chart canvas')).toBeVisible();
    await page.getByText(messages.ui.chart.showTable).click();
    await expect(page.getByRole('table')).toContainText('jan/26');

    await field(page, text.fields.measure).selectOption('commission');
    await expect(
      field(page, text.fields.groupBy).getByRole('option', {
        name: text.incompatible(text.groupings.customer),
      }),
    ).toBeDisabled();
    await field(page, text.fields.groupBy).selectOption('mechanic');
    await field(page, text.fields.chart).selectOption('line');
    await expect(page.getByTestId('report-problems')).toContainText(text.problems.lineNeedsTime);
    await expect(page.getByRole('button', { name: text.save.open })).toBeDisabled();

    await field(page, text.fields.chart).selectOption('table');
    await field(page, text.fields.period).selectOption('custom');
    await page.getByLabel(new RegExp(`^\\s*${text.fields.from}`)).fill('2025-01-01');
    await page.getByLabel(new RegExp(`^\\s*${text.fields.to}`)).fill('2025-03-31');
    await expect(page.getByText(text.empty)).toBeVisible();

    await field(page, text.fields.period).selectOption('yearToDate');
    await field(page, text.fields.chart).selectOption('donut');
    await expect(page.locator('ui-chart canvas')).toBeVisible();
    await page.getByRole('button', { name: text.save.open }).click();
    const dialog = page.getByRole('dialog');
    await dialog.getByRole('textbox').fill('Comissão por mecânico');
    await dialog.getByRole('button', { name: text.save.submit }).click();
    await expect(page.getByText(text.save.saved('Comissão por mecânico'))).toBeVisible();
    expect(state.database.reports.saved[0]?.definition).toEqual({
      measure: 'commission',
      groupBy: 'mechanic',
      chart: 'donut',
      period: { preset: 'yearToDate', from: null, to: null },
    });

    await page.getByRole('button', { name: text.saved.schedule('Comissão por mecânico') }).click();
    const scheduling = page.getByRole('dialog');
    await scheduling
      .getByRole('textbox', { name: new RegExp(text.schedule.recipients) })
      .fill('gestor@oficina.local');
    await scheduling.getByRole('button', { name: text.schedule.submit }).click();
    await expect(
      page.getByText(text.saved.scheduled(text.weekdays.Monday, '08:00')).first(),
    ).toBeVisible();

    await page.getByRole('button', { name: text.export.xlsx }).click();
    await expect(
      page.getByRole('link', { name: text.export.download(text.formats.xlsx) }),
    ).toBeVisible({ timeout: 15_000 });

    await field(page, text.fields.chart).selectOption('table');
    await field(page, text.fields.measure).selectOption('revenue');
    await field(page, text.fields.groupBy).selectOption('customer');
    await page.getByRole('button', { name: text.export.pdf }).click();
    await expect(page.getByText(text.export.large)).toBeVisible();

    await page.getByRole('button', { name: text.saved.remove('Comissão por mecânico') }).click();
    await page.getByRole('dialog').getByRole('button', { name: text.saved.removeConfirm }).click();
    await expect(page.getByText(text.saved.removed('Comissão por mecânico'))).toBeVisible();

    await expectNoHorizontalScroll(page);
    await expectNoSeriousAxeViolations(page);
    expect(policy).toEqual([]);
  });
});
