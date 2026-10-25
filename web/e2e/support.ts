import AxeBuilder from '@axe-core/playwright';
import { expect, Locator, Page } from '@playwright/test';
import { messages } from '../src/app/core/i18n/messages.pt-BR';
import { Account } from './accounts';

export function trackPolicyErrors(page: Page): string[] {
  const errors: string[] = [];
  page.on('console', (message) => {
    if (
      message.type() === 'error' &&
      /Content Security Policy|Trusted Type/i.test(message.text())
    ) {
      errors.push(message.text());
    }
  });
  return errors;
}

export async function expectNoSeriousAxeViolations(page: Page): Promise<void> {
  await page.evaluate(() =>
    Promise.all(document.getAnimations().map((animation) => animation.finished.catch(() => null))),
  );
  const results = await new AxeBuilder({ page }).analyze();
  const serious = results.violations.filter(
    (violation) => violation.impact === 'serious' || violation.impact === 'critical',
  );
  expect(
    serious.map(
      (violation) =>
        `${violation.id}: ${violation.nodes.map((node) => node.target.join(' ')).join(', ')}`,
    ),
  ).toEqual([]);
}

export async function expectNoHorizontalScroll(page: Page): Promise<void> {
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
  );
  expect(overflow).toBeLessThanOrEqual(0);
}

export async function signIn(page: Page, account: Account): Promise<void> {
  await page.getByRole('textbox', { name: messages.auth.login.email }).fill(account.email);
  await page.locator('input[autocomplete="current-password"]').fill(account.password);
  await page.getByRole('button', { name: messages.auth.login.submit }).click();
}

export async function loginAs(page: Page, account: Account, path = '/login'): Promise<void> {
  await page.context().clearCookies();
  await page.goto(path);
  await expect(page.getByRole('heading', { level: 1 })).toHaveText(messages.auth.login.title);
  await signIn(page, account);
  await expect(page).not.toHaveURL(/\/login/, { timeout: 20_000 });
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
}

export async function openInApp(page: Page, path: string): Promise<void> {
  await page.evaluate((target) => {
    history.pushState(null, '', target);
    dispatchEvent(new PopStateEvent('popstate', { state: null }));
  }, path);
}

export async function mainNavigation(page: Page): Promise<Locator> {
  const openMenu = page.getByRole('button', { name: messages.shell.openMenu });
  if (await openMenu.isVisible()) {
    await openMenu.click();
  }

  return page.getByRole('navigation', { name: messages.shell.mainNavigation });
}

export function offlineDatabaseExists(page: Page): Promise<boolean> {
  return page.evaluate(async () =>
    (await indexedDB.databases()).some((database) => database.name === 'oficina-offline'),
  );
}

export async function queuedOfflineActions(page: Page): Promise<number> {
  if (!(await offlineDatabaseExists(page))) {
    return 0;
  }

  return page.evaluate(
    () =>
      new Promise<number>((resolve) => {
        const opening = indexedDB.open('oficina-offline');
        opening.onerror = () => resolve(-1);
        opening.onsuccess = () => {
          const database = opening.result;
          if (!database.objectStoreNames.contains('queues')) {
            database.close();
            resolve(0);
            return;
          }

          const reading = database.transaction('queues').objectStore('queues').get('mechanic');
          reading.onerror = () => resolve(-1);
          reading.onsuccess = () => {
            const stored = reading.result as { readonly items: readonly unknown[] } | undefined;
            database.close();
            resolve(stored?.items.length ?? 0);
          };
        };
      }),
  );
}
