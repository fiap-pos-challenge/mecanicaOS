import { expect, Page, test } from '@playwright/test';
import { messages } from '../src/app/core/i18n/messages.pt-BR';
import { FakeBackendState } from '../src/testing/fake-api/fake-api/fake-backend';
import { pushFakeNotification } from '../src/testing/fake-api/notifications/notifications.fake-api';
import { ACCOUNTS } from './accounts';
import { interceptFakeApis } from './fake-backend';
import { loginAs, mainNavigation, openInApp, trackPolicyErrors } from './support';

const XSS = '<img src=x onerror="window.__xss=1">';
const PERSONAL = [/@/, /\d{11}/, /\d{14}/, /[A-Z]{3}\d[A-Z0-9]\d{2}/];

async function storage(page: Page) {
  const saved = await page.context().storageState();
  const local = Object.fromEntries(
    saved.origins.flatMap((origin) => origin.localStorage.map((item) => [item.name, item.value])),
  );
  const session = await page.evaluate(() =>
    Object.fromEntries(Object.entries(Reflect.get(window, 'sessionStorage') as Storage)),
  );
  return { local, session };
}

async function expectNoXss(page: Page): Promise<void> {
  expect(await page.evaluate(() => (window as Window & { __xss?: number }).__xss)).toBeUndefined();
  await expect(page.locator('img[src="x"]')).toHaveCount(0);
}

test.describe('front-end security checks', () => {
  test.describe.configure({ timeout: 120_000 });

  let state: FakeBackendState;

  test.beforeEach(async ({ page }) => {
    state = await interceptFakeApis(page);
  });

  test('keeps tokens and personal data out of storage, URLs and titles', async ({ page }) => {
    const urls: string[] = [];
    page.on('framenavigated', (frame) => {
      if (frame === page.mainFrame()) {
        urls.push(frame.url());
      }
    });
    const titles: string[] = [];
    await loginAs(page, ACCOUNTS.admin);
    for (const screen of [
      messages.screens.customers,
      messages.screens.quotes,
      messages.screens.serviceOrders,
      messages.screens.settings,
    ]) {
      const nav = await mainNavigation(page);
      await nav.getByRole('link', { name: screen }).click();
      await expect(page.getByRole('heading', { level: 1, name: screen })).toBeVisible();
      titles.push(await page.title());
      urls.push(page.url());
    }

    const quote = state.database.quotes.quotes[0];
    await openInApp(page, `/quotes/${quote?.id ?? ''}`);
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
    titles.push(await page.title());
    urls.push(page.url());

    const saved = await storage(page);
    expect(Object.keys(saved.local).every((key) => key.startsWith('ui.'))).toBe(true);
    expect(saved.session).toEqual({});
    const text = JSON.stringify(saved);
    expect(text).not.toMatch(/eyJ[A-Za-z0-9_-]{10,}\./);
    for (const value of [...urls, ...titles]) {
      for (const pattern of PERSONAL) {
        expect(value.replace(/[0-9a-f]{8}(-[0-9a-f]{4}){3}-[0-9a-f]{12}/g, '')).not.toMatch(
          pattern,
        );
      }
    }

    const indexed = await page.evaluate(async () =>
      (await indexedDB.databases()).map((database) => database.name),
    );
    expect(indexed.filter((name) => name !== 'oficina-offline')).toEqual([]);
  });

  test('keeps the mechanic offline queue free of customer data', async ({ page, context }) => {
    await loginAs(page, ACCOUNTS.mechanic);
    await page.getByRole('link', { name: messages.mechanic.open(3327) }).click();
    await expect(page).toHaveURL(/\/mechanic\/.+/);
    await expect(
      page.getByRole('tab', { name: messages.mechanic.sections.diagnosis }),
    ).toBeVisible();
    await context.setOffline(true);
    await expect(page.getByText(messages.mechanic.sync.offline)).toBeVisible();
    await page.getByRole('tab', { name: messages.mechanic.sections.diagnosis }).click();
    await page
      .getByRole('textbox', { name: messages.mechanic.diagnosis.label })
      .fill('Ruído na suspensão dianteira');
    await page.getByRole('button', { name: messages.mechanic.diagnosis.save }).click();
    const queued = await page.evaluate(
      () =>
        new Promise<string>((resolve) => {
          const opening = indexedDB.open('oficina-offline');
          opening.onsuccess = () => {
            const database = opening.result;
            const reading = database.transaction('queues').objectStore('queues').get('mechanic');
            reading.onsuccess = () => {
              database.close();
              resolve(JSON.stringify(reading.result ?? null));
            };
          };
        }),
    );
    await context.setOffline(false);
    expect(queued).toContain('Ruído na suspensão dianteira');
    for (const customer of state.database.customers) {
      expect(queued).not.toContain(customer.name);
      expect(queued).not.toContain(customer.document);
      for (const value of [customer.email, customer.phone]) {
        if (value) {
          expect(queued).not.toContain(value);
        }
      }
    }

    const token = JSON.stringify(await storage(page));
    expect(token).not.toMatch(/eyJ[A-Za-z0-9_-]{10,}\./);
  });

  test('renders free text from users as text, never as HTML', async ({ page }) => {
    const policy = trackPolicyErrors(page);
    const quote = state.database.quotes.quotes.find((item) => item.status === 'Rejected');
    if (quote) {
      quote.rejectionReason = XSS;
    }

    const rule = state.database.automations.rules[0];
    if (rule) {
      rule.definition = { ...rule.definition, name: XSS };
    }

    pushFakeNotification(state, {
      type: 'StockLow',
      title: XSS,
      body: XSS,
      link: '/inventory',
      aiSuggested: false,
    });
    await loginAs(page, ACCOUNTS.admin);

    await page.getByRole('button', { name: messages.shell.notifications }).click();
    await expect(page.getByRole('list', { name: messages.notifications.label })).toContainText(XSS);
    await expectNoXss(page);
    await page.keyboard.press('Escape');

    await openInApp(page, `/quotes/${quote?.id ?? ''}`);
    await expect(page.getByText(XSS).first()).toBeVisible();
    await expectNoXss(page);

    const nav = await mainNavigation(page);
    await nav.getByRole('link', { name: messages.screens.automations }).click();
    await expect(page.getByRole('list', { name: messages.automations.rules.label })).toContainText(
      XSS,
    );
    await expectNoXss(page);

    await page.getByRole('button', { name: messages.automations.newRule }).click();
    const dialog = page.getByRole('dialog');
    await dialog
      .getByRole('textbox', { name: new RegExp(messages.automations.editor.name) })
      .fill(XSS);
    await dialog
      .getByRole('textbox', { name: new RegExp(messages.automations.editor.message) })
      .fill(XSS);
    await expectNoXss(page);
    await dialog.getByRole('button', { name: messages.automations.editor.cancel }).click();
    expect(policy).toEqual([]);
  });
});
