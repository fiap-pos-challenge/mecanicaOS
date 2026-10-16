import { expect, Page, test } from '@playwright/test';
import { messages } from '../src/app/core/i18n/messages.pt-BR';
import { ACCOUNTS } from './accounts';
import { interceptFakeApis } from './fake-backend';
import {
  expectNoHorizontalScroll,
  expectNoSeriousAxeViolations,
  loginAs,
  mainNavigation,
} from './support';

const text = messages.quotes;

function field(page: Page, label: string) {
  const escaped = label.replace(/[()$.*+?]/g, '\\$&');
  return page.getByLabel(new RegExp(`^\\s*${escaped}(\\s*\\*)?\\s*$`));
}

async function openQuotes(page: Page): Promise<void> {
  const nav = await mainNavigation(page);
  await nav.getByRole('link', { name: messages.screens.quotes }).click();
  await expect(
    page.getByRole('heading', { level: 1, name: messages.screens.quotes }),
  ).toBeVisible();
  await expect(page.getByRole('link', { name: text.list.open(1047) })).toBeVisible();
}

async function addFrom(page: Page, section: keyof typeof text.builder.sections, name: string) {
  await page.getByRole('tab', { name: text.builder.sections[section] }).click();
  await page.getByRole('button', { name: text.builder.addItem(name) }).click();
}

test.describe('quotes with the contract API intercepted', () => {
  test.describe.configure({ timeout: 120_000 });

  test.beforeEach(async ({ page }) => {
    await interceptFakeApis(page);
  });

  test('builds, prices, sends and registers the approval of a new quote', async ({ page }) => {
    await loginAs(page, ACCOUNTS.attendant);
    await openQuotes(page);

    await page.getByRole('button', { name: text.newQuote }).click();
    const create = page.getByRole('dialog', { name: text.create.title });
    await create.getByRole('radio', { name: new RegExp(text.create.order(3323)) }).check();
    await create.getByRole('button', { name: text.create.submit }).click();
    await expect(
      page.getByRole('heading', { level: 1, name: text.editor.title(1048) }),
    ).toBeVisible();
    await expect(page.getByTestId('send-blocks')).toContainText(text.editor.blocks.Empty);

    await addFrom(page, 'packages', 'Revisão 40 mil km');
    await addFrom(page, 'parts', 'Vela de ignição');
    await expect(page.getByTestId('stock-warnings')).toContainText('Vela de ignição: faltam 1');
    await page.getByRole('tab', { name: text.builder.sections.suggestions }).click();
    await expect(page.getByText(text.builder.noSuggestions)).toBeVisible();
    await expect(page.getByTestId('quote-totals')).toContainText(
      text.totals.discountRule('Primeiro serviço'),
    );

    await page.getByRole('radio', { name: text.discount.modes.Manual }).click();
    await field(page, text.discount.value).fill('15');
    await field(page, text.discount.reason).fill('Cliente indicado');
    await expect(page.getByTestId('discount-limit')).toContainText(text.discount.exceeds('10%'));
    await page.getByRole('button', { name: text.discount.requestApproval }).click();
    await expect(page.getByTestId('send-blocks')).toContainText(text.editor.blocks.ApprovalPending);
    await field(page, text.discount.value).fill('5');
    await expect(page.getByTestId('send-blocks')).toBeHidden();

    await page.getByRole('button', { name: text.paymentMethods.Boleto }).click();
    await field(page, text.payment.installments).selectOption({ label: text.payment.upTo(6) });
    await expect(page.getByText(/6x de/)).toBeVisible();
    await page.getByRole('button', { name: text.editor.save, exact: true }).click();
    await expect(page.getByTestId('save-state')).toHaveText(text.editor.saveStates.saved);
    await expectNoHorizontalScroll(page);
    await expectNoSeriousAxeViolations(page);

    await page.getByRole('button', { name: text.editor.preview }).click();
    const preview = page.getByRole('dialog', { name: text.document.preview });
    await expect(preview.getByText(messages.documents.draft)).toBeVisible();
    await preview.getByRole('button', { name: messages.ui.dialog.close }).click();

    await page.getByRole('button', { name: text.editor.send }).click();
    const send = page.getByRole('dialog', { name: text.send.title(1048) });
    await send.getByRole('radio', { name: text.channels.Email }).check();
    await expect(field(page, text.send.message)).toHaveValue(/Carlos/);
    await send.getByRole('button', { name: text.send.submit }).click();
    await expect(page.getByTestId('delivery')).toContainText(text.deliveryStatuses.Failed, {
      timeout: 15_000,
    });
    await page.getByRole('button', { name: text.delivery.retry }).click();
    await expect(page.getByTestId('delivery')).toContainText(text.deliveryStatuses.Opened, {
      timeout: 20_000,
    });
    await expect(page.getByText(text.editor.locked)).toBeVisible();

    await page.getByRole('button', { name: text.editor.registerApproval }).click();
    await expect(page.getByText(text.statuses.Approved).first()).toBeVisible();
    await expect(page.getByRole('button', { name: text.editor.send })).toHaveCount(0);
    await expectNoHorizontalScroll(page);
  });

  test('registers a rejection, creates a new version and compares versions', async ({ page }) => {
    await loginAs(page, ACCOUNTS.admin);
    await openQuotes(page);
    await page.getByRole('link', { name: text.list.open(1047) }).click();
    await expect(
      page.getByRole('heading', { level: 1, name: text.editor.title(1047) }),
    ).toBeVisible();
    await expect(page.getByTestId('price-changes')).toContainText('Pastilha de freio dianteira');
    await page.getByRole('button', { name: text.items.updatePrices }).click();
    await expect(page.getByTestId('price-changes')).toBeHidden();

    await page.getByRole('button', { name: text.editor.registerRejection }).click();
    const reject = page.getByRole('dialog', { name: text.reject.title });
    await reject.getByRole('button', { name: text.reject.submit }).click();
    await expect(reject.getByText(text.reject.required)).toBeVisible();
    await field(page, text.reject.reason).fill('Achou caro');
    await reject.getByRole('button', { name: text.reject.submit }).click();
    await expect(page.getByText(text.editor.rejected('Achou caro'))).toBeVisible();

    await page.getByRole('button', { name: text.editor.newVersion }).click();
    await expect(page.getByText(text.editor.version(2), { exact: true })).toBeVisible();
    await page.getByRole('button', { name: text.items.remove('Vela de ignição') }).click();
    await expect(page.getByTestId('save-state')).toHaveText(text.editor.saveStates.saved);
    await page.getByRole('button', { name: text.editor.versions }).click();
    const versions = page.getByRole('dialog', { name: text.versions.title });
    await expect(versions.getByText(/Removido · Vela de ignição/)).toBeVisible();
    await expect(versions.getByTestId('version-delta')).toContainText('−');
    await expectNoHorizontalScroll(page);
    await expectNoSeriousAxeViolations(page);
  });

  test('regenerates an expired link', async ({ page }) => {
    await loginAs(page, ACCOUNTS.admin);
    await openQuotes(page);
    await page.getByRole('link', { name: text.list.open(1042) }).click();

    await expect(page.getByTestId('delivery')).toContainText(text.deliveryStatuses.Expired);
    await page.getByRole('button', { name: text.delivery.regenerate }).click();
    await expect(page.getByTestId('delivery')).toContainText(
      text.delivery.channel(text.channels.Link, 2),
    );
  });
});
