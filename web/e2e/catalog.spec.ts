import { expect, Page, test } from '@playwright/test';
import { messages } from '../src/app/core/i18n/messages.pt-BR';
import { ACCOUNTS } from './accounts';
import { expectNoSeriousAxeViolations, loginAs, mainNavigation } from './support';

const catalog = messages.catalog;
const list = catalog.list;
const form = catalog.form;

async function openCatalog(page: Page, tab: keyof typeof catalog.kinds): Promise<void> {
  const nav = await mainNavigation(page);
  await nav.getByRole('link', { name: messages.screens.catalog }).click();
  await expect(
    page.getByRole('heading', { level: 1, name: messages.screens.catalog }),
  ).toBeVisible();
  await page
    .getByRole('navigation', { name: catalog.tabs })
    .getByRole('link', { name: catalog.kinds[tab] })
    .click();
  await expect(page.getByRole('table', { name: list.caption(catalog.kinds[tab]) })).toBeVisible();
}

function field(page: Page, label: string) {
  const escaped = label.replace(/[()$]/g, '\\$&');
  return page.getByLabel(new RegExp(`^\\s*${escaped}(\\s*\\*)?\\s*$`));
}

test.describe('catalog while its API is under construction', () => {
  test.describe.configure({ timeout: 90_000 });

  test('shows every tab empty with the construction notice', async ({ page }) => {
    await loginAs(page, ACCOUNTS.admin);

    for (const tab of ['services', 'packages', 'parts'] as const) {
      await openCatalog(page, tab);
      await expect(page.getByText(messages.pending.catalog)).toBeVisible();
      await expect(page.getByText(list.empty)).toBeVisible();
    }

    await expectNoSeriousAxeViolations(page);
  });

  test('keeps the part form and answers the save with the construction message', async ({
    page,
  }) => {
    await loginAs(page, ACCOUNTS.admin);
    await openCatalog(page, 'parts');
    await page.getByRole('link', { name: catalog.newItem.parts }).click();

    await expect(page.getByRole('heading', { level: 1, name: form.parts.newTitle })).toBeVisible();
    await field(page, form.parts.code).fill('PEC-9200');
    await field(page, form.name).fill('Correia dentada');
    await field(page, form.parts.cost).fill('90');
    await field(page, form.parts.unitPrice).fill('150');
    await expect(page.getByTestId('part-margin')).toHaveText('40%');
    await page.getByRole('button', { name: form.save }).click();
    await expect(page.getByTestId('catalog-form-error')).toHaveText(messages.errors.featurePending);
  });

  test('opens the package form with the construction notice', async ({ page }) => {
    await loginAs(page, ACCOUNTS.admin);
    await openCatalog(page, 'packages');
    await page.getByRole('link', { name: catalog.newItem.packages }).click();

    await expect(
      page.getByRole('heading', { level: 1, name: form.packages.newTitle }),
    ).toBeVisible();
    await expect(page.getByText(messages.pending.catalog)).toBeVisible();
  });

  test('hides costs and changes from the mechanic', async ({ page }) => {
    await loginAs(page, ACCOUNTS.mechanic);
    await openCatalog(page, 'parts');

    const table = page.getByRole('table', { name: list.caption(catalog.kinds.parts) });
    await expect(table.getByRole('columnheader', { name: list.columns.margin })).toHaveCount(0);
    await expect(page.getByRole('link', { name: catalog.newItem.parts })).toHaveCount(0);
  });
});
