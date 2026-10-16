import { expect, Locator, Page, test } from '@playwright/test';
import { messages } from '../src/app/core/i18n/messages.pt-BR';
import { FakeBackendState } from '../src/testing/fake-api/fake-api/fake-backend';
import { moveOrder } from '../src/testing/fake-api/service-orders/service-orders.fake-api';
import { ACCOUNTS } from './accounts';
import { interceptFakeApis } from './fake-backend';
import {
  expectNoHorizontalScroll,
  expectNoSeriousAxeViolations,
  loginAs,
  mainNavigation,
  queuedOfflineActions,
} from './support';

const text = messages.serviceOrders;
const mechanic = messages.mechanic;
const PNG = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==',
  'base64',
);

function order(state: FakeBackendState, number: number) {
  const found = state.database.serviceOrders.orders.find((item) => item.number === number);
  if (!found) {
    throw new Error(`Order ${number} not found.`);
  }

  return found;
}

function column(page: Page, status: keyof typeof text.statuses): Locator {
  return page.locator(`section[data-status="${status}"]`);
}

function card(page: Page, number: number): Locator {
  return page.getByRole('article', { name: text.number(number) });
}

async function touchDrag(
  page: Page,
  start: { readonly x: number; readonly y: number },
  target: Locator,
): Promise<void> {
  const cdp = await page.context().newCDPSession(page);
  const touch = (type: string, x: number, y: number) =>
    cdp.send('Input.dispatchTouchEvent', {
      type,
      touchPoints: type === 'touchEnd' ? [] : [{ x, y }],
    });
  await touch('touchStart', start.x, start.y);
  for (let step = 1; step <= 4; step++) {
    await touch('touchMove', start.x + step * 2, start.y + step * 2);
  }

  const to = await target.boundingBox();
  if (!to) {
    throw new Error('Target not visible.');
  }

  const endX = to.x + to.width / 2;
  const endY = to.y + Math.min(60, to.height / 2);
  for (let step = 1; step <= 20; step++) {
    await touch(
      'touchMove',
      start.x + ((endX - start.x) * step) / 20,
      start.y + ((endY - start.y) * step) / 20,
    );
  }

  await touch('touchEnd', endX, endY);
  await cdp.detach();
}

async function drag(page: Page, source: Locator, target: Locator): Promise<void> {
  await source.scrollIntoViewIfNeeded();
  const from = await source.boundingBox();
  if (!from) {
    throw new Error('Source not visible.');
  }

  const startX = from.x + from.width / 2;
  const startY = from.y + 24;
  if (test.info().project.name === 'mobile') {
    await touchDrag(page, { x: startX, y: startY }, target);
    return;
  }

  await page.mouse.move(startX, startY);
  await page.mouse.down();
  await page.mouse.move(startX + 8, startY + 8, { steps: 4 });
  const to = await target.boundingBox();
  if (!to) {
    throw new Error('Target not visible.');
  }

  await page.mouse.move(to.x + to.width / 2, to.y + Math.min(60, to.height / 2), { steps: 20 });
  await page.mouse.up();
}

async function openBoard(page: Page): Promise<void> {
  const nav = await mainNavigation(page);
  await nav.getByRole('link', { name: messages.screens.serviceOrders }).click();
  await expect(page.getByRole('group', { name: text.board.label })).toBeVisible();
}

test.describe('service orders with the contract API intercepted', () => {
  test.describe.configure({ timeout: 120_000 });

  let state: FakeBackendState;

  test.beforeEach(async ({ page }) => {
    state = await interceptFakeApis(page);
  });

  test('drags cards between columns, blocks invalid moves and moves by keyboard', async ({
    page,
  }) => {
    await loginAs(page, ACCOUNTS.attendant);
    await openBoard(page);

    await expect(column(page, 'InProgress').getByText(text.tags.Late)).toBeVisible();
    await expect(column(page, 'InProgress').getByText(text.tags.MissingPart)).toBeVisible();
    await expect(column(page, 'WaitingApproval').getByText(text.tags.ReminderSent)).toBeVisible();

    await drag(page, card(page, 3326), column(page, 'InDiagnosis'));
    await expect(
      column(page, 'InDiagnosis').getByRole('article', { name: text.number(3326) }),
    ).toBeVisible();
    expect(order(state, 3326).status).toBe('InDiagnosis');

    await drag(page, card(page, 3326), column(page, 'Received'));
    await expect(page.getByTestId('move-blocked')).toContainText(text.board.blocked.NotAllowed);
    await expect(
      column(page, 'InDiagnosis').getByRole('article', { name: text.number(3326) }),
    ).toBeVisible();

    await page.getByRole('button', { name: text.board.moveOf(3324) }).click();
    await expect(
      page.getByRole('button', { name: text.board.moveTo(text.statuses.Delivered) }),
    ).toBeDisabled();
    await page.getByRole('button', { name: text.board.moveTo(text.statuses.InProgress) }).focus();
    await page.keyboard.press('Enter');
    await expect(
      column(page, 'InProgress').getByRole('article', { name: text.number(3324) }),
    ).toBeVisible();

    await expectNoHorizontalScroll(page);
    await expectNoSeriousAxeViolations(page);
  });

  test('filters the list, shows the agenda and opens the order detail', async ({ page }) => {
    await loginAs(page, ACCOUNTS.attendant);
    await openBoard(page);

    await page.getByRole('radio', { name: text.views.list }).click();
    const table = page.getByRole('table', { name: text.list.caption });
    await expect(table.getByRole('row')).toHaveCount(11);
    await page.getByRole('switch', { name: text.filters.lateOnly }).click();
    await expect(table.getByRole('row')).toHaveCount(2);
    await page.getByRole('button', { name: text.filters.clear }).click();

    await page.getByRole('radio', { name: text.views.agenda }).click();
    await expect(page.getByRole('heading', { name: text.agenda.overdue })).toBeVisible();
    await expect(page.getByRole('link', { name: text.open(3318) })).toBeVisible();
    await expect(page.getByRole('link', { name: text.open(3315) })).toHaveCount(0);

    await page.getByRole('radio', { name: text.views.list }).click();
    await page.getByRole('link', { name: text.open(3315) }).click();
    await expect(
      page.getByRole('heading', { level: 1, name: text.detail.title(3315) }),
    ).toBeVisible();
    await expect(page.getByText(text.detail.missing(3))).toBeVisible();
    await expect(page.getByRole('heading', { name: text.detail.timeline })).toBeVisible();
    await expectNoHorizontalScroll(page);
    await expectNoSeriousAxeViolations(page);
  });

  test('shows a version conflict and changes made by another user', async ({ page }) => {
    await loginAs(page, ACCOUNTS.attendant);
    await openBoard(page);

    moveOrder(order(state, 3326), 'InDiagnosis', 'Gabi Gestora', null);
    await page.getByRole('button', { name: text.board.moveOf(3326) }).click();
    await page.getByRole('button', { name: text.board.moveTo(text.statuses.InDiagnosis) }).click();
    await expect(page.getByText(text.changes.conflict(3326))).toBeVisible();

    moveOrder(order(state, 3319), 'Completed', 'Gabi Gestora', null);
    await expect(page.getByText(text.changes.remote(3319))).toBeVisible({ timeout: 30_000 });
    await expect(
      column(page, 'Completed').getByRole('article', { name: text.number(3319) }),
    ).toBeVisible();
  });

  test('runs the mechanic mode offline and syncs when the network returns', async ({
    page,
    context,
  }) => {
    await loginAs(page, ACCOUNTS.mechanic);
    await expect(
      page.getByRole('heading', { level: 1, name: messages.screens.mechanic }),
    ).toBeVisible();
    await page.getByRole('link', { name: mechanic.open(3327) }).click();
    await expect(page.getByText(text.number(3327))).toBeVisible();

    await page.getByRole('group', { name: 'Freios' }).getByText(mechanic.states.Replace).click();
    await expect.poll(() => order(state, 3327).checklist[1]?.state).toBe('Replace');

    await page.getByRole('tab', { name: mechanic.sections.photos }).click();
    await page
      .getByLabel(mechanic.photos.take)
      .setInputFiles({ name: 'roda.png', mimeType: 'image/png', buffer: PNG });
    await expect(page.getByAltText(mechanic.photos.alt(3327, 1))).toBeVisible();
    await expect.poll(() => order(state, 3327).photos.length).toBe(1);

    await context.setOffline(true);
    await expect(page.getByText(mechanic.sync.offline)).toBeVisible();
    await page.getByRole('tab', { name: mechanic.sections.checklist }).click();
    await page.getByRole('group', { name: 'Pneus' }).getByText(mechanic.states.Attention).click();
    await page.getByRole('tab', { name: mechanic.sections.diagnosis }).click();
    await page
      .getByRole('textbox', { name: mechanic.diagnosis.label })
      .fill('bieleta dianteira com folga');
    await page.getByRole('button', { name: mechanic.diagnosis.save }).click();
    await expect(page.getByTestId('sync-banner')).toContainText(mechanic.sync.pending(2));
    await expect.poll(() => queuedOfflineActions(page)).toBe(2);
    await expect(page.getByRole('button', { name: mechanic.diagnosis.suggest })).toBeDisabled();
    expect(order(state, 3327).diagnosis).toBe('');

    await context.setOffline(false);
    await expect(page.getByTestId('sync-banner')).toBeHidden({ timeout: 15_000 });
    await expect.poll(() => queuedOfflineActions(page)).toBe(0);
    expect(order(state, 3327).diagnosis).toBe('bieleta dianteira com folga');
    expect(order(state, 3327).checklist[3]?.state).toBe('Attention');

    await page.getByRole('button', { name: mechanic.diagnosis.suggest }).click();
    await expect(page.getByRole('checkbox', { name: /Alinhamento 3D/ })).toBeChecked();
    await page.getByRole('button', { name: mechanic.actions.sendToQuote }).click();
    await expect.poll(() => order(state, 3327).sentToQuote).toBe(true);
    await expectNoHorizontalScroll(page);
    await expectNoSeriousAxeViolations(page);
  });
});
