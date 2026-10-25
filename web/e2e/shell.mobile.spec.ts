import { expect, test } from '@playwright/test';
import { messages } from '../src/app/core/i18n/messages.pt-BR';
import { ACCOUNTS } from './accounts';
import { expectNoSeriousAxeViolations, loginAs } from './support';

const text = messages.shell;

test.describe('mobile menu', () => {
  test('opens the drawer and closes it with the close button and Escape', async ({ page }) => {
    await loginAs(page, ACCOUNTS.admin);
    await expect(page.getByRole('navigation', { name: text.mainNavigation })).toHaveCount(0);

    await page.getByRole('button', { name: text.openMenu }).click();
    const drawer = page.getByRole('dialog', { name: text.mainNavigation });
    await expect(drawer).toBeVisible();
    await expectNoSeriousAxeViolations(page);

    await drawer.getByRole('button', { name: text.closeMenu }).click();
    await expect(drawer).toHaveCount(0);

    await page.getByRole('button', { name: text.openMenu }).click();
    await page.keyboard.press('Escape');
    await expect(drawer).toHaveCount(0);
  });

  test('closes the drawer when tapping outside and after navigating', async ({ page }) => {
    await loginAs(page, ACCOUNTS.admin);
    const bottomNav = page.getByRole('navigation', { name: messages.bottomNav.label });

    await bottomNav.getByRole('button', { name: messages.bottomNav.menu }).click();
    await expect(page.getByRole('dialog')).toBeVisible();
    await expect(page.getByTestId('drawer-backdrop')).toBeVisible();
    await page.mouse.click(370, 400);
    await expect(page.getByRole('dialog')).toHaveCount(0);

    await bottomNav.getByRole('button', { name: messages.bottomNav.menu }).click();
    await expect(page.getByRole('dialog')).toBeVisible();
    await page.getByRole('dialog').getByRole('link', { name: messages.screens.inventory }).click();
    await expect(page).toHaveURL('/inventory');
    await expect(page.getByRole('dialog')).toHaveCount(0, { timeout: 10_000 });
  });

  test('navigates with the bottom bar', async ({ page }) => {
    await loginAs(page, ACCOUNTS.admin);
    const bottomNav = page.getByRole('navigation', { name: messages.bottomNav.label });

    await bottomNav.getByRole('link', { name: messages.bottomNav.serviceOrders }).click();
    await expect(page).toHaveURL('/service-orders');
    await expect(
      bottomNav.getByRole('link', { name: messages.bottomNav.serviceOrders }),
    ).toHaveAttribute('aria-current', 'page');

    await bottomNav.getByRole('link', { name: messages.bottomNav.quote }).click();
    await expect(page).toHaveURL('/quotes');

    await bottomNav.getByRole('link', { name: messages.bottomNav.mechanic }).click();
    await expect(page.getByRole('heading', { level: 1 })).toHaveText(messages.screens.mechanic);
  });

  test('gives the mechanic a reduced bottom bar on the phone', async ({ page }) => {
    await loginAs(page, ACCOUNTS.mechanic);
    const bottomNav = page.getByRole('navigation', { name: messages.bottomNav.label });

    await expect(page).toHaveURL('/mechanic');
    await expect(bottomNav.getByRole('link')).toHaveText([
      messages.bottomNav.serviceOrders,
      messages.bottomNav.mechanic,
    ]);
  });
});
