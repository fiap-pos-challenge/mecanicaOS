import { expect, test } from '@playwright/test';
import { messages, pageTitle } from '../src/app/core/i18n/messages.pt-BR';
import { Account, ACCOUNTS, adminToken, createUser, E2E_PASSWORD } from './accounts';
import {
  expectNoSeriousAxeViolations,
  loginAs,
  mainNavigation,
  openInApp,
  signIn,
} from './support';

const text = messages.auth;

const problem = (status: number, code: string, extra: Record<string, unknown> = {}) => ({
  type: `https://httpstatuses.io/${status}`,
  title: 'Error',
  status,
  detail: 'Erro simulado no teste.',
  code,
  traceId: '00-e2e0000000000000000000000000000-0000000000000001-01',
  ...extra,
});

test.describe('authentication against the real API', () => {
  test('signs in with the development administrator and lands on the dashboard', async ({
    page,
  }) => {
    await loginAs(page, ACCOUNTS.admin);

    await expect(page).toHaveURL('/dashboard');
    await expect(page).toHaveTitle(pageTitle(messages.screens.dashboard));
    await page.getByRole('button', { name: text.user.menu(ACCOUNTS.admin.name) }).click();
    await expect(
      page.getByRole('region', { name: text.user.menu(ACCOUNTS.admin.name) }),
    ).toContainText(messages.roles.Admin);
  });

  test('shows a generic message for a wrong password', async ({ page }) => {
    await page.goto('/login');

    await signIn(page, { ...ACCOUNTS.attendant, password: 'Errada@2026' });

    await expect(page.getByTestId('login-error')).toHaveText(text.errors.invalidCredentials);
    await expect(page.locator('input[autocomplete="current-password"]')).toHaveValue('');
    await expectNoSeriousAxeViolations(page);
  });

  test('tells a disabled account apart after the right password', async ({ page }) => {
    await page.goto('/login');

    await signIn(page, ACCOUNTS.disabled);

    await expect(page.getByTestId('login-error')).toHaveText(text.errors.accountDisabled);
  });

  test('locks the account after five wrong passwords and shows the time left', async ({
    page,
    request,
  }, testInfo) => {
    const account: Account = {
      email: `bloqueio.${testInfo.project.name}.${Date.now()}@oficina.local`,
      password: E2E_PASSWORD,
      name: 'Bloqueio E2E',
      role: 'Attendant',
    };
    await createUser(request, await adminToken(request), account);
    await page.goto('/login');

    for (let attempt = 1; attempt < 5; attempt++) {
      await signIn(page, { ...account, password: 'Errada@2026' });
      await expect(page.getByTestId('login-error')).toHaveText(text.errors.invalidCredentials);
    }
    await signIn(page, { ...account, password: 'Errada@2026' });

    await expect(page.getByTestId('login-wait')).toHaveText(/Conta bloqueada .* 1[45]:\d\d\./);
    await expect(page.getByRole('button', { name: text.login.submit })).toBeDisabled();
  });

  test('counts down the rate limit of the API and frees the form', async ({ page }) => {
    await page.route('**/api/auth/login', (route) =>
      route.fulfill({
        status: 429,
        contentType: 'application/problem+json',
        headers: { 'Retry-After': '2' },
        body: JSON.stringify(problem(429, 'common.tooManyRequests', { retryAfterSeconds: 2 })),
      }),
    );
    await page.goto('/login');

    await signIn(page, ACCOUNTS.attendant);

    await expect(page.getByTestId('login-wait')).toHaveText(text.errors.tooManyRequests('0:02'));
    await expect(page.getByRole('button', { name: text.login.submit })).toBeDisabled();
    await expect(page.getByTestId('login-wait')).toHaveText(text.errors.waitOver, {
      timeout: 5000,
    });
    await expect(page.getByRole('button', { name: text.login.submit })).toBeEnabled();
  });

  test('explains when there is no network and shows the support code on 500', async ({ page }) => {
    await page.goto('/login');
    await page.route('**/api/auth/login', (route) => route.abort('internetdisconnected'));
    await signIn(page, ACCOUNTS.attendant);
    await expect(page.getByTestId('login-error')).toHaveText(text.errors.network);

    await page.unroute('**/api/auth/login');
    await page.route('**/api/auth/login', (route) =>
      route.fulfill({
        status: 500,
        contentType: 'application/problem+json',
        body: JSON.stringify(problem(500, 'common.unexpected')),
      }),
    );
    await signIn(page, ACCOUNTS.attendant);
    await expect(page.getByTestId('login-error')).toContainText(text.errors.server);
    await expect(page.getByTestId('login-error')).toContainText(
      '00-e2e0000000000000000000000000000-0000000000000001-01',
    );
  });

  test('shows only what the profile can see and blocks forbidden addresses', async ({ page }) => {
    await loginAs(page, ACCOUNTS.attendant);
    const nav = await mainNavigation(page);
    await expect(nav.getByRole('link', { name: messages.screens.customers })).toBeVisible();
    await expect(nav.getByRole('link', { name: messages.screens.finance })).toHaveCount(0);
    await expect(nav.getByRole('link', { name: messages.screens.team })).toHaveCount(0);
    await page.keyboard.press('Escape');

    await openInApp(page, '/finance');

    await expect(page).toHaveURL('/forbidden');
    await expect(page.getByRole('heading', { level: 1 })).toHaveText(
      messages.pages.forbidden.title,
    );
  });

  test('sends the mechanic straight to the mechanic mode', async ({ page }) => {
    await loginAs(page, ACCOUNTS.mechanic);

    await expect(page).toHaveURL('/mechanic');
    const nav = await mainNavigation(page);
    await expect(nav.getByRole('link', { name: messages.screens.customers })).toHaveCount(0);
    await expect(nav.getByRole('link', { name: messages.screens.inventory })).toBeVisible();
  });

  test('returns to the requested address after signing in', async ({ page }) => {
    await page.goto('/customers');
    await expect(page).toHaveURL('/login');

    await signIn(page, ACCOUNTS.attendant);

    await expect(page).toHaveURL('/customers');
    await expect(page.getByRole('heading', { level: 1 })).toHaveText(messages.screens.customers);
  });

  test('logs out and keeps the screens closed afterwards', async ({ page }) => {
    await loginAs(page, ACCOUNTS.attendant);

    await page.getByRole('button', { name: text.user.menu(ACCOUNTS.attendant.name) }).click();
    await page.getByRole('button', { name: text.user.logout }).click();

    await expect(page).toHaveURL('/login');
    await expect(page.getByRole('status')).toHaveText(text.notices.logout);
    await page.goBack();
    await expect(page).toHaveURL('/login');
  });

  test('keeps the session after reloading the page with the refresh cookie', async ({ page }) => {
    await loginAs(page, ACCOUNTS.attendant);
    const url = page.url();

    await page.reload();

    await expect(page).toHaveURL(url);
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
    await page.context().clearCookies();
    await page.reload();
    await expect(page).toHaveURL('/login');
  });

  test('warns at 25 minutes of inactivity and logs out at 30', async ({ page }) => {
    await page.clock.install();
    await loginAs(page, ACCOUNTS.attendant);

    await page.clock.fastForward('25:00');
    const dialog = page.getByRole('dialog', { name: text.inactivity.title });
    await expect(dialog).toBeVisible();
    await dialog.getByRole('button', { name: text.inactivity.stay }).click();
    await expect(dialog).toHaveCount(0);

    await page.clock.fastForward('25:00');
    await expect(dialog).toBeVisible();
    await page.clock.fastForward('05:00');

    await expect(page).toHaveURL('/login');
    await expect(page.getByRole('status')).toHaveText(text.notices.inactivity);
  });
});
