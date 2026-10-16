import { expect, test } from '@playwright/test';
import { messages } from '../src/app/core/i18n/messages.pt-BR';
import { ACCOUNTS } from './accounts';
import { loginAs, signIn } from './support';

const text = messages.shell;

test.describe('desktop menu', () => {
  test.beforeEach(async ({ page }) => loginAs(page, ACCOUNTS.admin));

  test('navigates through the side menu', async ({ page }) => {
    const nav = page.getByRole('navigation', { name: text.mainNavigation });

    await nav.getByRole('link', { name: messages.screens.catalog }).click();

    await expect(page).toHaveURL('/catalog/services');
    await expect(page.getByRole('heading', { level: 1 })).toHaveText(messages.screens.catalog);
    await expect(nav.getByRole('link', { name: messages.screens.catalog })).toHaveAttribute(
      'aria-current',
      'page',
    );
    await expect(page.getByRole('navigation', { name: messages.bottomNav.label })).toHaveCount(0);
  });

  test('collapses the menu to icons and remembers it after a new login', async ({ page }) => {
    await page.getByRole('button', { name: text.collapseMenu }).click();
    await expect(page.getByRole('button', { name: text.expandMenu })).toHaveAttribute(
      'aria-expanded',
      'false',
    );

    await page.reload();
    await signIn(page, ACCOUNTS.admin);
    await expect(page.getByRole('button', { name: text.expandMenu })).toBeVisible();
    const link = page
      .getByRole('navigation', { name: text.mainNavigation })
      .getByRole('link', { name: messages.screens.reports });
    await expect(link).toHaveAttribute('title', messages.screens.reports);

    await page.getByRole('button', { name: text.expandMenu }).click();
    await expect(page.getByRole('button', { name: text.collapseMenu })).toBeVisible();
  });

  test('focuses the global search with Ctrl K', async ({ page }) => {
    await page.keyboard.press('Control+k');

    await expect(page.getByRole('searchbox', { name: text.searchLabel })).toBeFocused();
  });

  test('switches the theme and keeps it after reload', async ({ page }) => {
    await page
      .getByRole('button', { name: text.theme.change(text.theme.system, text.theme.light) })
      .click();
    await page
      .getByRole('button', { name: text.theme.change(text.theme.light, text.theme.dark) })
      .click();
    await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');

    await page.reload();
    await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');
  });
});
