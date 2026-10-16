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

test.describe('inventory while its API is under construction', () => {
  test.describe.configure({ timeout: 90_000 });

  test('shows the overview with KPIs, filters and the construction notice', async ({ page }) => {
    await loginAs(page, ACCOUNTS.admin);
    await openInventory(page);

    await expect(page.getByText(messages.pending.inventory)).toBeVisible();
    await expect(page.getByText(text.overview.kpis.belowMinimum)).toBeVisible();
    await expect(page.getByRole('table', { name: text.overview.caption })).toBeVisible();
    await expect(page.getByText(text.overview.empty)).toBeVisible();
    await expectNoHorizontalScroll(page);
    await expectNoSeriousAxeViolations(page);
  });

  test('keeps the entry dialog with its three modes and answers with the construction message', async ({
    page,
  }) => {
    await loginAs(page, ACCOUNTS.admin);
    await openInventory(page);

    await page.getByRole('button', { name: text.overview.registerEntry }).click();
    const dialog = page.getByRole('dialog', { name: text.entry.title });
    await expect(dialog.getByText(messages.pending.inventory)).toBeVisible();
    await dialog.getByRole('radio', { name: text.entry.modes.barcode }).click();
    await dialog
      .getByRole('textbox', { name: new RegExp(text.entry.barcode) })
      .fill('7891234567895');
    await dialog.getByRole('button', { name: text.entry.lookup }).click();
    await expect(dialog.getByTestId('inventory-dialog-error')).toHaveText(
      messages.errors.featurePending,
    );
    await dialog.getByRole('radio', { name: text.entry.modes.invoice }).click();
    await dialog.getByLabel(text.entry.invoiceFile).setInputFiles({
      name: 'nota.xml',
      mimeType: 'text/xml',
      buffer: Buffer.from('<nfe/>'),
    });
    await dialog.getByRole('button', { name: text.entry.read }).click();
    await expect(dialog.getByTestId('inventory-dialog-error')).toHaveText(
      messages.errors.featurePending,
    );
  });

  test('opens the count, orders and suppliers sections and keeps their actions', async ({
    page,
  }) => {
    await loginAs(page, ACCOUNTS.admin);
    await openInventory(page);

    await openSection(page, 'counts');
    await expect(page.getByText(messages.pending.inventory)).toBeVisible();
    await expect(page.getByRole('button', { name: text.counts.start })).toBeVisible();

    await openSection(page, 'orders');
    await expect(
      page.getByRole('heading', { level: 2, name: text.orders.suggestionsTitle }),
    ).toBeVisible();
    await expect(page.getByText(messages.pending.inventory)).toBeVisible();

    await openSection(page, 'suppliers');
    await page.getByRole('button', { name: text.suppliers.add }).click();
    const dialog = page.getByRole('dialog', { name: text.suppliers.newTitle });
    await dialog
      .getByRole('textbox', { name: new RegExp(`^${text.suppliers.name}`) })
      .fill('Peças Norte');
    await dialog.getByRole('button', { name: text.suppliers.save }).click();
    await expect(dialog.getByTestId('inventory-dialog-error')).toHaveText(
      messages.errors.featurePending,
    );
  });

  test('lets the mechanic open the count on the phone without stock changes', async ({ page }) => {
    await loginAs(page, ACCOUNTS.mechanic);
    await openInventory(page);

    await expect(page.getByRole('button', { name: text.overview.registerEntry })).toHaveCount(0);
    await page.getByRole('link', { name: text.overview.startCount }).click();
    await expect(page.getByRole('heading', { level: 2, name: text.counts.title })).toBeVisible();
    await expect(page.getByText(messages.pending.inventory)).toBeVisible();
    await expectNoHorizontalScroll(page);
  });
});
