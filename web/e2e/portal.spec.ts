import { expect, Page, test } from '@playwright/test';
import { messages } from '../src/app/core/i18n/messages.pt-BR';
import { FakeBackendState } from '../src/testing/fake-api/fake-api/fake-backend';
import {
  PORTAL_DONE_TOKEN,
  PORTAL_EXPIRED_TOKEN,
  PORTAL_TOKEN,
  PROVIDER_CHECKOUT,
} from '../src/testing/fake-api/portal/portal.fake-api';
import { interceptFakeApis } from './fake-backend';
import { expectNoHorizontalScroll, expectNoSeriousAxeViolations } from './support';

const text = messages.portal;
const documents = messages.documents;

async function confirm(page: Page, digits = '321'): Promise<void> {
  await expect(page.getByRole('heading', { name: text.confirmTitle })).toBeVisible();
  await page.getByRole('textbox', { name: new RegExp(text.digits) }).fill(digits);
  await page.getByRole('button', { name: text.confirm }).click();
}

function order(state: FakeBackendState, number: number) {
  const found = state.database.serviceOrders.orders.find((item) => item.number === number);
  if (!found) {
    throw new Error('Missing order');
  }

  return found;
}

test.describe('customer portal with the contract API intercepted', () => {
  test.describe.configure({ timeout: 120_000 });

  let state: FakeBackendState;

  test.beforeEach(async ({ page }) => {
    state = await interceptFakeApis(page);
  });

  test('shows the same neutral screen for invalid and expired links and keeps the token out of the title', async ({
    page,
  }) => {
    const response = await page.goto('/o/invalid-token');
    expect(response?.headers()['referrer-policy']).toBe('no-referrer');
    const invalid = await page.getByTestId('portal-unavailable').textContent();
    await page.goto(`/o/${PORTAL_EXPIRED_TOKEN}`);
    await expect(page.getByTestId('portal-unavailable')).toHaveText(invalid ?? '');
    await expect(page).toHaveTitle(new RegExp(text.screen));
    expect(await page.title()).not.toContain(PORTAL_EXPIRED_TOKEN);
    await expectNoSeriousAxeViolations(page);
  });

  test('locks the link after three wrong confirmations', async ({ page }) => {
    await page.goto(`/o/${PORTAL_TOKEN}`);
    await confirm(page, '000');
    await expect(page.getByText(text.wrongDigits(2))).toBeVisible();
    await confirm(page, '111');
    await expect(page.getByText(text.wrongDigits(1))).toBeVisible();
    await confirm(page, '222');
    await expect(page.getByTestId('portal-locked')).toBeVisible();
    await page.reload();
    await expect(page.getByTestId('portal-locked')).toBeVisible();
  });

  test('keeps the link usable when the confirmation is rate limited', async ({ page }) => {
    const detail = 'Muitas requisições. Aguarde um minuto e tente de novo.';
    await page.route(
      '**/api/public/portal/confirm',
      (route) =>
        route.fulfill({
          status: 429,
          contentType: 'application/problem+json',
          headers: { 'retry-after': '60' },
          body: JSON.stringify({
            title: 'Muitas requisições',
            status: 429,
            detail,
            code: 'common.tooManyRequests',
            traceId: '00-e2e-portal-throttle-00',
          }),
        }),
      { times: 1 },
    );
    await page.goto(`/o/${PORTAL_TOKEN}`);
    await confirm(page, '000');
    await expect(page.getByText(detail)).toBeVisible();
    await expect(page.getByTestId('portal-locked')).toHaveCount(0);

    await confirm(page);
    await expect(page.getByRole('heading', { name: text.hello('Joana') })).toBeVisible();
  });

  test('approves part of the quote and pays with Pix', async ({ page }) => {
    await page.goto(`/o/${PORTAL_TOKEN}`);
    await confirm(page);
    await expect(page.getByRole('heading', { name: text.hello('Joana') })).toBeVisible();
    await expect(page.locator('li[aria-current="step"]')).toContainText(text.steps.WaitingApproval);
    await expect(page.getByAltText('Pastilha gasta')).toBeVisible();
    await expectNoHorizontalScroll(page);
    await expectNoSeriousAxeViolations(page);

    const total = page.getByTestId('portal-total');
    const before = await total.textContent();
    await page.getByRole('checkbox', { name: /Alinhamento 3D/ }).uncheck();
    await expect(total).not.toHaveText(before ?? '');
    await page.getByRole('button', { name: text.approve }).click();
    await expect(page.getByText(text.approved)).toBeVisible();
    expect(order(state, 3324).quoteItems.map((item) => item.name)).not.toContain('Alinhamento 3D');

    await page.getByRole('button', { name: text.payPix }).click();
    await expect(page.getByAltText(text.pixQr)).toBeVisible();
    await expect(page.getByRole('textbox', { name: text.pixCode })).toHaveValue(
      /BR\.GOV\.BCB\.PIX/,
    );
    await expect(page.getByTestId('portal-notice')).toHaveText(text.pixPaid, {
      timeout: 20_000,
    });
    await expect(page.getByText(text.paymentStatus.Paid, { exact: true })).toBeVisible();
  });

  test('redirects the card payment to the provider and shows a declined card on return', async ({
    page,
    baseURL,
  }) => {
    await page.route(`${PROVIDER_CHECKOUT}**`, (route) =>
      route.fulfill({
        status: 302,
        headers: { location: new URL(`/o/${PORTAL_TOKEN}?payment=declined`, baseURL).href },
      }),
    );
    await page.goto(`/o/${PORTAL_TOKEN}`);
    await confirm(page);
    await page.getByRole('button', { name: text.approve }).click();
    await expect(page.getByText(text.approved)).toBeVisible();

    await page.getByRole('button', { name: text.payCard }).click();
    await expect(page).toHaveURL(/payment=declined/);
    await confirm(page);
    await expect(page.getByText(text.cardDeclined)).toBeVisible();
  });

  test('rejects the quote with a reason', async ({ page }) => {
    await page.goto(`/o/${PORTAL_TOKEN}`);
    await confirm(page);
    await page.getByRole('button', { name: text.reject }).click();
    await page.getByRole('button', { name: text.rejectSubmit }).click();
    await expect(page.getByText(text.rejectRequired)).toBeVisible();
    await page
      .getByRole('textbox', { name: new RegExp(text.rejectReason) })
      .fill('Vou fazer no mês que vem');
    await page.getByRole('button', { name: text.rejectSubmit }).click();
    await expect(page.getByText(text.rejected)).toBeVisible();
    await expect(page.getByText(text.answered.Rejected)).toBeVisible();
  });

  test('opens, downloads and verifies the receipt', async ({ page }) => {
    await page.goto(`/o/${PORTAL_DONE_TOKEN}`);
    await confirm(page);
    await page
      .getByRole('button', { name: documents.open(documents.kinds.Receipt, 'REC1-3310-M2XT') })
      .click();
    const dialog = page.getByRole('dialog', { name: documents.preview });
    await expect(dialog.getByTestId('verification-code')).toHaveText(
      documents.verification('REC1-3310-M2XT'),
    );
    await expect(dialog.getByText(documents.noFiscal)).toBeVisible();
    const download = page.waitForEvent('download');
    await dialog.getByRole('button', { name: documents.download }).click();
    expect((await download).suggestedFilename()).toBe('REC1-3310-M2XT.pdf');
    await expectNoHorizontalScroll(page);

    await page.goto('/v/REC1-3310-M2XT');
    await expect(page.getByTestId('verify-valid')).toContainText(documents.verify.valid);
    await page.goto('/v/FAKE-0000');
    await expect(page.getByTestId('verify-invalid')).toContainText(documents.verify.invalid);
    await expectNoSeriousAxeViolations(page);
  });
});
