import { expect, Page, test } from '@playwright/test';
import { messages } from '../src/app/core/i18n/messages.pt-BR';
import { FakeBackendState } from '../src/testing/fake-api/fake-api/fake-backend';
import { PORTAL_TOKEN } from '../src/testing/fake-api/portal/portal.fake-api';
import { Account, ACCOUNTS } from './accounts';
import { interceptFakeApis } from './fake-backend';
import { loginAs, mainNavigation, trackPolicyErrors } from './support';

const orders = messages.serviceOrders;
const mechanic = messages.mechanic;
const quotes = messages.quotes;
const portal = messages.portal;

function order(state: FakeBackendState, number: number) {
  const found = state.database.serviceOrders.orders.find((item) => item.number === number);
  if (!found) {
    throw new Error(`Order ${number} not found.`);
  }

  return found;
}

async function logout(page: Page, account: Account): Promise<void> {
  await page.getByRole('button', { name: messages.auth.user.menu(account.name) }).click();
  await page.getByRole('button', { name: messages.auth.user.logout }).click();
  await expect(page).toHaveURL(/\/login/);
}

async function openScreen(page: Page, screen: string): Promise<void> {
  const nav = await mainNavigation(page);
  await nav.getByRole('link', { name: screen }).click();
  await expect(page.getByRole('heading', { level: 1, name: screen })).toBeVisible();
}

async function move(page: Page, number: number, status: keyof typeof orders.statuses) {
  await page.getByRole('button', { name: orders.board.moveOf(number) }).click();
  await page.getByRole('button', { name: orders.board.moveTo(orders.statuses[status]) }).click();
}

test.describe('main journey from the plan, end to end', () => {
  test.describe.configure({ timeout: 180_000 });

  test('receives the car, diagnoses, quotes, gets approval, executes, collects and delivers', async ({
    page,
  }) => {
    const state = await interceptFakeApis(page);
    const policy = trackPolicyErrors(page);

    await loginAs(page, ACCOUNTS.attendant);
    await openScreen(page, messages.screens.serviceOrders);
    await move(page, 3326, 'InDiagnosis');
    await expect.poll(() => order(state, 3326).status).toBe('InDiagnosis');
    await logout(page, ACCOUNTS.attendant);

    await loginAs(page, ACCOUNTS.mechanic);
    await page.getByRole('link', { name: mechanic.open(3327) }).click();
    await expect(page.getByText(orders.number(3327))).toBeVisible();
    await page.getByRole('group', { name: 'Freios' }).getByText(mechanic.states.Replace).click();
    await page.getByRole('tab', { name: mechanic.sections.diagnosis }).click();
    await page
      .getByRole('textbox', { name: mechanic.diagnosis.label })
      .fill('Bieleta dianteira com folga');
    await page.getByRole('button', { name: mechanic.diagnosis.save }).click();
    await page.getByRole('button', { name: mechanic.diagnosis.suggest }).click();
    await expect(page.getByText(messages.ui.ai.badge)).toBeVisible();
    await page.getByRole('button', { name: mechanic.actions.sendToQuote }).click();
    await expect.poll(() => order(state, 3327).sentToQuote).toBe(true);
    await logout(page, ACCOUNTS.mechanic);

    await loginAs(page, ACCOUNTS.attendant);
    await openScreen(page, messages.screens.quotes);
    await page.getByRole('button', { name: quotes.newQuote }).click();
    const create = page.getByRole('dialog', { name: quotes.create.title });
    await create.getByRole('radio').first().check();
    await create.getByRole('button', { name: quotes.create.submit }).click();
    await page.getByRole('tab', { name: quotes.builder.sections.packages }).click();
    await page.getByRole('button', { name: quotes.builder.addItem('Revisão 40 mil km') }).click();
    await page.getByRole('button', { name: quotes.editor.save, exact: true }).click();
    await expect(page.getByTestId('save-state')).toHaveText(quotes.editor.saveStates.saved);
    await page.getByRole('button', { name: quotes.editor.send }).click();
    const send = page.getByRole('dialog');
    await send.getByRole('radio', { name: quotes.channels.Link }).check();
    await send.getByRole('button', { name: quotes.send.submit }).click();
    await expect(page.getByTestId('delivery')).toBeVisible({ timeout: 15_000 });

    await page.goto(`/o/${PORTAL_TOKEN}`);
    await page.getByRole('textbox', { name: new RegExp(portal.digits) }).fill('321');
    await page.getByRole('button', { name: portal.confirm }).click();
    await expect(page.getByAltText('Pastilha gasta')).toBeVisible();
    await page.getByRole('button', { name: portal.approve }).click();
    await expect(page.getByText(portal.approved)).toBeVisible();
    expect(order(state, 3324).status).toBe('InProgress');
    await page.getByRole('button', { name: portal.payPix }).click();
    await expect(page.getByTestId('portal-notice')).toHaveText(portal.pixPaid, { timeout: 20_000 });

    await loginAs(page, ACCOUNTS.attendant);
    await openScreen(page, messages.screens.serviceOrders);
    await move(page, 3324, 'Completed');
    await expect.poll(() => order(state, 3324).status).toBe('Completed');
    await move(page, 3324, 'Delivered');
    await expect.poll(() => order(state, 3324).status).toBe('Delivered');
    expect(policy).toEqual([]);
  });
});
