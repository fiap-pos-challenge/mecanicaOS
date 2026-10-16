import { expect, test } from '@playwright/test';
import { messages } from '../src/app/core/i18n/messages.pt-BR';
import { ACCOUNTS } from './accounts';
import { mainNavigation, signIn, trackPolicyErrors } from './support';

const DEMO_URL = 'http://localhost:4301';

test.describe('demo build for manual validation', () => {
  test.describe.configure({ timeout: 120_000 });

  test('shows fictitious data with a fixed banner and keeps the real API where it exists', async ({
    page,
  }) => {
    const policy = trackPolicyErrors(page);
    await page.goto(`${DEMO_URL}/login`);
    const banner = page.getByTestId('demo-banner');
    await expect(banner).toHaveText(messages.demo.banner);
    await signIn(page, ACCOUNTS.admin);
    await expect(
      page.getByRole('heading', { level: 1, name: messages.screens.dashboard }),
    ).toBeVisible();
    await expect(banner).toBeVisible();
    await expect(
      page
        .locator('[data-card="kpis"]')
        .getByText(messages.dashboard.kpis.revenue, { exact: true }),
    ).toBeVisible();
    await expect(page.getByText(messages.pending.dashboard)).toHaveCount(0);

    const nav = await mainNavigation(page);
    await nav.getByRole('link', { name: messages.screens.quotes }).click();
    await expect(page.getByRole('table', { name: messages.quotes.list.caption })).toContainText(
      'Joana',
    );
    await expect(banner).toBeVisible();

    const again = await mainNavigation(page);
    await again.getByRole('link', { name: messages.screens.customers }).click();
    await expect(
      page.getByRole('heading', { level: 1, name: messages.screens.customers }),
    ).toBeVisible();
    await expect(page.getByText('Transportes Rota Sul Ltda')).toHaveCount(0);
    expect(policy).toEqual([]);
  });
});
