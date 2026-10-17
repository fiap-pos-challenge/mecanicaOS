import { expect, Page, test } from '@playwright/test';
import { messages } from '../src/app/core/i18n/messages.pt-BR';
import { ACCOUNTS, adminToken } from './accounts';
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

test.describe('catalog on the real API', () => {
  test.describe.configure({ timeout: 90_000 });

  function unique(prefix: string): string {
    return `${prefix} ${Math.random().toString(36).slice(2, 9)}`;
  }

  async function createByApi(page: Page, path: string, data: object): Promise<void> {
    const token = await adminToken(page.request);
    const response = await page.request.post(path, {
      headers: { Authorization: `Bearer ${token}` },
      data,
    });
    expect(response.status()).toBe(201);
  }

  test('opens every tab without the construction notice', async ({ page }) => {
    await loginAs(page, ACCOUNTS.admin);

    for (const tab of ['services', 'packages', 'parts'] as const) {
      await openCatalog(page, tab);
      await expect(page.getByText(messages.pending.catalog)).toHaveCount(0);
    }

    await expectNoSeriousAxeViolations(page);
  });

  test('creates a service and records the base price history', async ({ page }) => {
    const name = unique('Alinhamento E2E');
    await loginAs(page, ACCOUNTS.admin);
    await openCatalog(page, 'services');
    await page.getByRole('link', { name: catalog.newItem.services }).click();

    await expect(
      page.getByRole('heading', { level: 1, name: form.services.newTitle }),
    ).toBeVisible();
    await expect(page.getByText(messages.pending.catalog)).toHaveCount(0);
    await field(page, form.name).fill(name);
    await field(page, form.services.basePrice).fill('120');
    await field(page, form.services.minutes).fill('40');
    await page.getByRole('button', { name: form.save }).click();

    await expect(
      page.getByRole('heading', { level: 1, name: form.services.editTitle }),
    ).toBeVisible();
    const history = page.getByRole('list', { name: messages.catalog.history.caption });
    await expect(history.getByRole('listitem')).toHaveCount(1);

    await field(page, form.services.basePrice).fill('150');
    await page.getByRole('button', { name: form.save }).click();
    await expect(history.getByRole('listitem')).toHaveCount(2);

    await openCatalog(page, 'services');
    await expect(
      page.getByRole('table', { name: list.caption(catalog.kinds.services) }),
    ).toContainText(name);
  });

  test('creates a part with margin, rejects a repeated code and records cost and price', async ({
    page,
  }) => {
    const code = `PEC-${Math.random().toString(36).slice(2, 9).toUpperCase()}`;
    await loginAs(page, ACCOUNTS.admin);
    await openCatalog(page, 'parts');
    await page.getByRole('link', { name: catalog.newItem.parts }).click();

    await expect(page.getByRole('heading', { level: 1, name: form.parts.newTitle })).toBeVisible();
    await field(page, form.parts.code).fill(code);
    await field(page, form.name).fill('Correia dentada');
    await field(page, form.parts.cost).fill('90');
    await field(page, form.parts.unitPrice).fill('150');
    await expect(page.getByTestId('part-margin')).toHaveText('40%');
    await page.getByRole('button', { name: form.save }).click();

    await expect(page.getByRole('heading', { level: 1, name: form.parts.editTitle })).toBeVisible({
      timeout: 15_000,
    });
    const history = page.getByRole('list', { name: messages.catalog.history.caption });
    await expect(history.getByRole('listitem')).toHaveCount(2);

    await openCatalog(page, 'parts');
    await page.getByRole('link', { name: catalog.newItem.parts }).click();
    await field(page, form.parts.code).fill(code);
    await field(page, form.name).fill('Correia repetida');
    await field(page, form.parts.cost).fill('10');
    await field(page, form.parts.unitPrice).fill('20');
    await page.getByRole('button', { name: form.save }).click();
    await expect(page.getByRole('heading', { level: 1, name: form.parts.newTitle })).toBeVisible();
    await expect(
      page.getByText(/Já existe uma peça ou insumo com este código/).first(),
    ).toBeVisible();
  });

  test('builds a package with savings and blocks deactivating one of its items', async ({
    page,
  }) => {
    const serviceName = unique('Troca de óleo E2E');
    const partName = unique('Filtro E2E');
    const packageName = unique('Revisão E2E');
    await createByApi(page, '/api/admin/services', {
      name: serviceName,
      basePrice: 100,
      estimatedMinutes: 30,
      commissionPercent: 10,
      warrantyDays: 90,
    });
    await createByApi(page, '/api/admin/parts', {
      code: `FLT-${Math.random().toString(36).slice(2, 9).toUpperCase()}`,
      name: partName,
      type: 'Part',
      unitOfMeasure: 'un',
      unitPrice: 25,
      cost: 10,
      minimumStock: 0,
    });
    await loginAs(page, ACCOUNTS.admin);
    await openCatalog(page, 'packages');
    await page.getByRole('link', { name: catalog.newItem.packages }).click();

    await expect(
      page.getByRole('heading', { level: 1, name: form.packages.newTitle }),
    ).toBeVisible();
    await field(page, form.name).fill(packageName);
    for (const item of [serviceName, partName]) {
      await page.getByRole('searchbox', { name: form.packages.search }).fill(item);
      await page.getByRole('button', { name: form.packages.add(item) }).click();
    }
    await field(page, form.packages.price).fill('110');
    await page.getByRole('button', { name: form.save }).click();

    await expect(
      page.getByRole('heading', { level: 1, name: form.packages.editTitle }),
    ).toBeVisible();
    await expect(page.getByText(/R\$\s15,00/).first()).toBeVisible();

    await openCatalog(page, 'parts');
    await page.getByRole('button', { name: list.deactivateOf(partName) }).click();
    await page.getByRole('dialog').getByRole('button', { name: list.deactivate }).click();
    const blocked = page.getByRole('dialog', { name: list.blockedTitle });
    await expect(blocked).toContainText(packageName);
  });

  test('hides costs and changes from the mechanic', async ({ page }) => {
    await loginAs(page, ACCOUNTS.mechanic);
    await openCatalog(page, 'parts');

    const table = page.getByRole('table', { name: list.caption(catalog.kinds.parts) });
    await expect(table.getByRole('columnheader', { name: list.columns.margin })).toHaveCount(0);
    await expect(page.getByRole('link', { name: catalog.newItem.parts })).toHaveCount(0);
  });
});
