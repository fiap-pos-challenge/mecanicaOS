import { expect, Page, test } from '@playwright/test';
import { messages } from '../src/app/core/i18n/messages.pt-BR';
import { FakeBackendState } from '../src/testing/fake-api/fake-api/fake-backend';
import { FAKE_ADMIN_PASSWORD } from '../src/testing/fake-api/fake-api/fake-database';
import { ACCOUNTS } from './accounts';
import { interceptFakeApis } from './fake-backend';
import {
  expectNoHorizontalScroll,
  expectNoSeriousAxeViolations,
  loginAs,
  mainNavigation,
  openInApp,
  trackPolicyErrors,
} from './support';

const text = messages.settings;

function field(page: Page, label: string) {
  return page.getByLabel(new RegExp(`^\\s*${label.replace(/[()]/g, '\\$&')}`));
}

async function confirm(page: Page, password: string): Promise<void> {
  const dialog = page.getByRole('dialog', { name: text.reauth.title });
  await dialog.getByLabel(new RegExp(text.reauth.password)).fill(password);
  await dialog.getByRole('button', { name: text.reauth.confirm }).click();
}

test.describe('settings with the contract API intercepted', () => {
  test.describe.configure({ timeout: 120_000 });

  let state: FakeBackendState;

  test.beforeEach(async ({ page }) => {
    state = await interceptFakeApis(page);
  });

  test('changes discounts, payments, messages and security with reauthentication', async ({
    page,
  }) => {
    const policy = trackPolicyErrors(page);
    await loginAs(page, ACCOUNTS.admin);
    const nav = await mainNavigation(page);
    await nav.getByRole('link', { name: messages.screens.settings }).click();
    await expect(page.getByTestId('plan')).toContainText('Essencial');

    await page.getByRole('tab', { name: text.tabs.discounts }).click();
    const attendant = field(page, text.discounts.limitOf(messages.roles.Attendant));
    await attendant.fill('15');
    await page.getByRole('button', { name: text.save }).click();
    await confirm(page, 'senha-errada');
    await expect(page.getByRole('dialog').getByText(text.reauth.failed)).toBeVisible();
    await confirm(page, FAKE_ADMIN_PASSWORD);
    await expect(page.getByText(text.saved)).toBeVisible();
    expect(state.database.quotes.discountLimits.Attendant).toBe(15);

    await page.getByRole('tab', { name: text.tabs.payments }).click();
    await field(page, text.payments.maxInstallments).fill('12');
    await page.getByRole('button', { name: text.save }).click();
    await confirm(page, FAKE_ADMIN_PASSWORD);
    await expect(page.getByText(text.saved).first()).toBeVisible();
    await expect.poll(() => state.database.quotes.payment.maxInstallments).toBe(12);

    await page.getByRole('tab', { name: text.tabs.messages }).click();
    const body = page.getByLabel(
      new RegExp(
        text.messages.body(text.messages.kinds.QuoteSent, text.messages.channels.WhatsApp),
      ),
    );
    await body.fill('Olá {cliente}, <img src=x onerror=alert(1)> {senha}');
    await expect(page.getByText(text.messages.unknown('{senha}'))).toBeVisible();
    await expect(page.getByTestId('message-preview').first()).toContainText(
      'Olá Joana, <img src=x onerror=alert(1)>',
    );
    await expect(page.locator('[data-testid="message-preview"] img')).toHaveCount(0);
    await body.fill('Olá {cliente}, veja {link}');
    await page.getByRole('button', { name: text.save }).click();
    await confirm(page, FAKE_ADMIN_PASSWORD);
    await expect
      .poll(() => state.database.settings.messages.templates[0]?.body)
      .toBe('Olá {cliente}, veja {link}');

    await page.getByRole('tab', { name: text.tabs.security }).click();
    await field(page, text.security.idle).fill('20');
    await page.getByRole('button', { name: text.save }).click();
    await confirm(page, FAKE_ADMIN_PASSWORD);
    await expect.poll(() => state.database.settings.security.idleMinutes).toBe(20);
    const table = page.getByRole('table', { name: text.security.auditCaption });
    await expect(table).toContainText('Política de segurança alterada');

    await expectNoHorizontalScroll(page);
    await expectNoSeriousAxeViolations(page);
    expect(policy).toEqual([]);
  });

  test('keeps the attendant out of the settings', async ({ page }) => {
    await loginAs(page, ACCOUNTS.attendant);
    const nav = await mainNavigation(page);
    await expect(nav.getByRole('link', { name: messages.screens.settings })).toHaveCount(0);
    await openInApp(page, '/settings');
    await expect(page.getByRole('heading', { level: 1 })).toHaveText(
      messages.pages.forbidden.title,
    );
  });
});
