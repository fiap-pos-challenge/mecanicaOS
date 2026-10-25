import { expect, test } from '@playwright/test';
import { messages, pageTitle } from '../src/app/core/i18n/messages.pt-BR';
import { SCREENS } from '../src/app/layout/screens';
import { ACCOUNTS } from './accounts';
import {
  expectNoHorizontalScroll,
  expectNoSeriousAxeViolations,
  loginAs,
  openInApp,
  trackPolicyErrors,
} from './support';

test.describe('shell', () => {
  test('loads under the strict security headers without policy violations', async ({ page }) => {
    const policyErrors = trackPolicyErrors(page);

    const response = await page.goto('/');
    const headers = response?.headers() ?? {};

    expect(headers['content-security-policy']).toContain("require-trusted-types-for 'script'");
    expect(headers['content-security-policy']).toContain("frame-ancestors 'none'");
    expect(headers['content-security-policy']).not.toContain('unsafe-inline');
    expect(headers['x-content-type-options']).toBe('nosniff');
    expect(headers['referrer-policy']).toBe('no-referrer');
    await expect(page).toHaveURL('/login');
    await expect(page).toHaveTitle(pageTitle(messages.auth.login.screen));
    await expectNoSeriousAxeViolations(page);
    expect(policyErrors).toEqual([]);
  });

  test('serves every screen of the administrator inside the session', async ({ page }) => {
    const policyErrors = trackPolicyErrors(page);
    await loginAs(page, ACCOUNTS.admin);
    await expect(page).toHaveTitle(pageTitle(messages.screens.dashboard));

    for (const screen of SCREENS.filter((item) => item.insideShell)) {
      await openInApp(page, `/${screen.path}`);
      await expect(page).toHaveURL(new RegExp(`/${screen.path}(/[a-z]+)?$`));
      await expect(page.getByRole('heading', { level: 1 })).toHaveText(screen.label);
    }

    await openInApp(page, '/qualquer/rota');
    await expect(page.getByRole('heading', { level: 1 })).toHaveText(messages.pages.notFound.title);
    expect(policyErrors).toEqual([]);
  });

  test('never scrolls horizontally from 360 px up', async ({ page }) => {
    await loginAs(page, ACCOUNTS.admin);

    for (const width of [360, 390, 768, 1024, 1440]) {
      await page.setViewportSize({ width, height: 800 });
      await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
      await expectNoHorizontalScroll(page);
    }

    await page.goto('/login');
    for (const width of [360, 1440]) {
      await page.setViewportSize({ width, height: 800 });
      await expectNoHorizontalScroll(page);
    }
  });

  for (const colorScheme of ['light', 'dark'] as const) {
    test(`has no serious accessibility violations in the ${colorScheme} theme`, async ({
      page,
    }) => {
      await page.emulateMedia({ reducedMotion: 'reduce', colorScheme });
      await page.goto('/login');
      await expectNoSeriousAxeViolations(page);
      await loginAs(page, ACCOUNTS.admin);

      await expectNoSeriousAxeViolations(page);
    });
  }
});
