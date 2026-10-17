import { expect, Page, test } from '@playwright/test';
import { messages } from '../src/app/core/i18n/messages.pt-BR';
import { ACCOUNTS } from './accounts';
import {
  expectNoHorizontalScroll,
  expectNoSeriousAxeViolations,
  loginAs,
  mainNavigation,
} from './support';

const text = messages.inventory;

async function openInventory(page: Page): Promise<void> {
  const nav = await mainNavigation(page);
  await nav.getByRole('link', { name: messages.screens.inventory }).click();
  await expect(
    page.getByRole('heading', { level: 1, name: messages.screens.inventory }),
  ).toBeVisible();
}

async function openSection(page: Page, section: keyof typeof text.sections): Promise<void> {
  await page
    .getByRole('navigation', { name: text.tabs })
    .getByRole('link', { name: text.sections[section] })
    .click();
}

test.describe('inventory on the real API', () => {
  test.describe.configure({ timeout: 90_000 });

  test('shows the overview with KPIs and filters without the construction notice', async ({
    page,
  }) => {
    await loginAs(page, ACCOUNTS.admin);
    await openInventory(page);

    await expect(page.getByText(text.overview.kpis.belowMinimum)).toBeVisible();
    await expect(page.getByRole('table', { name: text.overview.caption })).toBeVisible();
    await expect(page.getByText(messages.pending.inventory)).toHaveCount(0);
    await expectNoHorizontalScroll(page);
    await expectNoSeriousAxeViolations(page);
  });

  test('answers the entry dialog with the server validation', async ({ page }) => {
    await loginAs(page, ACCOUNTS.admin);
    await openInventory(page);

    await page.getByRole('button', { name: text.overview.registerEntry }).click();
    const dialog = page.getByRole('dialog', { name: text.entry.title });
    await expect(dialog.getByText(messages.pending.inventory)).toHaveCount(0);
    await dialog.getByRole('radio', { name: text.entry.modes.barcode }).click();
    await dialog
      .getByRole('textbox', { name: new RegExp(text.entry.barcode) })
      .fill('7891234567895');
    await dialog.getByRole('button', { name: text.entry.lookup }).click();
    await expect(dialog.getByTestId('inventory-dialog-error')).toBeVisible();
    await expect(dialog.getByTestId('inventory-dialog-error')).not.toHaveText(
      messages.errors.featurePending,
    );
    await dialog.getByRole('radio', { name: text.entry.modes.invoice }).click();
    await dialog.getByLabel(text.entry.invoiceFile).setInputFiles({
      name: 'nota.xml',
      mimeType: 'text/xml',
      buffer: Buffer.from('<nfe/>'),
    });
    await dialog.getByRole('button', { name: text.entry.read }).click();
    await expect(dialog.getByTestId('inventory-dialog-error')).toBeVisible();
    await expect(dialog.getByTestId('inventory-dialog-error')).not.toHaveText(
      messages.errors.featurePending,
    );
  });

  test('opens the count and orders sections and registers a supplier', async ({ page }) => {
    await loginAs(page, ACCOUNTS.admin);
    await openInventory(page);

    await openSection(page, 'counts');
    await expect(page.getByRole('button', { name: text.counts.start })).toBeVisible();
    await expect(page.getByText(messages.pending.inventory)).toHaveCount(0);

    await openSection(page, 'orders');
    await expect(
      page.getByRole('heading', { level: 2, name: text.orders.suggestionsTitle }),
    ).toBeVisible();

    await openSection(page, 'suppliers');
    await page.getByRole('button', { name: text.suppliers.add }).click();
    const dialog = page.getByRole('dialog', { name: text.suppliers.newTitle });
    const name = `Peças Norte ${Date.now() % 100_000}`;
    await dialog.getByRole('textbox', { name: new RegExp(`^${text.suppliers.name}`) }).fill(name);
    await dialog.getByRole('button', { name: text.suppliers.save }).click();
    await expect(dialog).toHaveCount(0);
    await expect(page.getByText(name)).toBeVisible();
  });

  test('lets the mechanic open the count on the phone without stock changes', async ({ page }) => {
    await loginAs(page, ACCOUNTS.mechanic);
    await openInventory(page);

    await expect(page.getByRole('button', { name: text.overview.registerEntry })).toHaveCount(0);
    await page.getByRole('link', { name: text.overview.startCount }).click();
    await expect(page.getByRole('heading', { level: 2, name: text.counts.title })).toBeVisible();
    await expect(page.getByText(messages.pending.inventory)).toHaveCount(0);
    await expectNoHorizontalScroll(page);
  });
});
