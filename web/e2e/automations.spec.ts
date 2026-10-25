import { expect, Page, test } from '@playwright/test';
import { messages } from '../src/app/core/i18n/messages.pt-BR';
import { FakeBackendState } from '../src/testing/fake-api/fake-api/fake-backend';
import { pushFakeNotification } from '../src/testing/fake-api/notifications/notifications.fake-api';
import { ACCOUNTS } from './accounts';
import { interceptFakeApis } from './fake-backend';
import {
  expectNoHorizontalScroll,
  expectNoSeriousAxeViolations,
  loginAs,
  mainNavigation,
  trackPolicyErrors,
} from './support';

const text = messages.automations;
const bell = messages.notifications;
const assistant = messages.assistant;

async function open(page: Page, screen: string): Promise<void> {
  const nav = await mainNavigation(page);
  await nav.getByRole('link', { name: screen }).click();
  await expect(page.getByRole('heading', { level: 1, name: screen })).toBeVisible();
}

test.describe('automations, notifications and AI with the contract API intercepted', () => {
  test.describe.configure({ timeout: 120_000 });

  let state: FakeBackendState;

  test.beforeEach(async ({ page }) => {
    state = await interceptFakeApis(page);
  });

  test('manages rules in the visual editor and reprocesses a failed item', async ({ page }) => {
    const policy = trackPolicyErrors(page);
    await loginAs(page, ACCOUNTS.admin);
    await open(page, messages.screens.automations);

    const reminder = text.templates.names.quoteReminder;
    await page.getByRole('switch', { name: text.rules.toggle(reminder) }).click();
    await expect(page.getByText(text.rules.pausedNow(reminder))).toBeVisible();
    expect(state.database.automations.rules[0]?.enabled).toBe(false);

    await page.getByRole('button', { name: text.templates.names.preventive }).click();
    const dialog = page.getByRole('dialog');
    const name = dialog.getByRole('textbox', { name: new RegExp(text.editor.name) });
    await name.fill('');
    await dialog.getByRole('button', { name: text.editor.save }).click();
    await expect(dialog.getByTestId('rule-problems')).toContainText(text.problems.name);
    await name.fill('Revisão dos clientes da frota');
    await dialog.getByRole('checkbox', { name: text.editor.wait }).check();
    await dialog.getByRole('button', { name: text.editor.save }).click();
    await expect(page.getByText(text.editor.saved('Revisão dos clientes da frota'))).toBeVisible();
    expect(state.database.automations.rules[0]?.definition).toMatchObject({
      trigger: 'VehicleMaintenanceDue',
      wait: { amount: 1, unit: 'days' },
    });

    const failures = page.getByRole('list', { name: text.queues.failuresLabel });
    await failures
      .getByRole('button', { name: text.queues.retryOf('PDF do recibo REC1-3310') })
      .click();
    await expect(page.getByText(text.queues.retried)).toBeVisible();
    await expect(failures.getByText('PDF do recibo REC1-3310')).toBeHidden();
    expect(state.database.automations.retried).toEqual(['failure-pdf']);

    await expectNoHorizontalScroll(page);
    await expectNoSeriousAxeViolations(page);
    expect(policy).toEqual([]);
  });

  test('shows the notification center with preferences and new notifications', async ({ page }) => {
    await loginAs(page, ACCOUNTS.admin);
    await expect(page.getByTestId('unread-badge')).toContainText('3');
    const button = page.getByRole('button', { name: messages.shell.notifications });
    await button.click();
    const list = page.getByRole('list', { name: bell.label });
    await expect(list.getByRole('listitem')).toHaveCount(3);
    await expect(list.locator('[data-ai="true"]')).toHaveCount(1);

    pushFakeNotification(state, {
      type: 'PaymentConfirmed',
      title: 'Pagamento da OS 3310 confirmado',
      body: 'Cartão aprovado.',
      link: '/service-orders',
      aiSuggested: false,
    });
    await button.click();
    await button.click();
    await expect(list.getByText('Pagamento da OS 3310 confirmado')).toBeVisible();
    await expect(page.getByTestId('unread-badge')).toContainText('4');

    await page.getByRole('button', { name: bell.markAll }).click();
    await expect(page.getByTestId('unread-badge')).toBeHidden();

    await page.getByRole('button', { name: bell.preferences }).click();
    const dialog = page.getByRole('dialog');
    await dialog
      .getByRole('checkbox', { name: bell.toggle(bell.types.ExportReady, bell.channels.email) })
      .check();
    await dialog.getByRole('button', { name: bell.save }).click();
    await expect(page.getByText(bell.saved)).toBeVisible();
    const saved = Object.values(state.database.notifications.preferences)[0];
    expect(saved?.find((item) => item.type === 'ExportReady')?.email).toBe(true);
    await expectNoSeriousAxeViolations(page);
  });

  test('asks the data in Portuguese and opens the AI suggestion in the builder', async ({
    page,
  }) => {
    const policy = trackPolicyErrors(page);
    await loginAs(page, ACCOUNTS.admin);
    await open(page, messages.screens.reports);
    await page
      .getByRole('textbox', { name: new RegExp(assistant.question) })
      .fill('qual mecânico mais faturou em setembro?');
    await page.getByRole('button', { name: assistant.ask }).click();
    const suggestion = page.getByRole('region', { name: assistant.suggestion });
    await expect(suggestion.getByText(messages.ui.ai.badge)).toBeVisible();
    await expect(
      page.getByRole('heading', { name: messages.reports.title('Faturamento', 'Mês') }),
    ).toBeVisible();

    await suggestion.getByRole('button', { name: assistant.open }).click();
    await expect(
      page.getByRole('heading', { name: messages.reports.title('Faturamento', 'Mecânico') }),
    ).toBeVisible();
    await expect(page.getByText(messages.reports.fromAssistant)).toBeVisible();
    await expect(page.locator('ui-chart canvas')).toBeVisible();
    expect(state.database.assistant.questions).toEqual(['qual mecânico mais faturou em setembro?']);
    await expectNoHorizontalScroll(page);
    expect(policy).toEqual([]);
  });
});
