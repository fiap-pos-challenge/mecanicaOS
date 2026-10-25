import { expect, test } from '@playwright/test';
import { messages } from '../src/app/core/i18n/messages.pt-BR';
import { ACCOUNTS } from './accounts';
import { loginAs, offlineDatabaseExists } from './support';

test.use({ serviceWorkers: 'allow' });

test.describe('installable app', () => {
  test('serves the manifest and the service worker without long cache', async ({ request }) => {
    const manifest = await request.get('/manifest.webmanifest');
    expect(manifest.ok()).toBe(true);
    expect(manifest.headers()['cache-control']).toBe('no-cache');
    expect(await manifest.json()).toMatchObject({ display: 'standalone', start_url: '/mechanic' });
    const worker = await request.get('/sw.js');
    expect(worker.headers()['cache-control']).toBe('no-cache');
    expect(worker.headers()['content-security-policy']).toContain('oficina#sw');
  });

  test('registers the service worker, opens offline and clears caches on logout', async ({
    page,
    context,
  }) => {
    await loginAs(page, ACCOUNTS.mechanic);
    await expect(page.locator('link[rel="manifest"]')).toHaveAttribute(
      'href',
      'manifest.webmanifest',
    );
    await expect
      .poll(() => page.evaluate(async () => Boolean((await navigator.serviceWorker.ready).active)))
      .toBe(true);
    await expect
      .poll(() => page.evaluate(async () => (await caches.keys()).length))
      .toBeGreaterThan(0);

    await context.setOffline(true);
    await page.reload();
    await expect(page.getByRole('heading', { level: 1 })).toHaveText(messages.auth.login.title);
    await context.setOffline(false);

    await loginAs(page, ACCOUNTS.mechanic);
    await page
      .getByRole('button', { name: messages.auth.user.menu(ACCOUNTS.mechanic.name) })
      .click();
    await page.getByRole('button', { name: messages.auth.user.logout }).click();
    await expect(page).toHaveURL('/login');
    await expect.poll(() => offlineDatabaseExists(page)).toBe(false);
    const cached = await page.evaluate(async () => {
      const urls: string[] = [];
      for (const key of await caches.keys()) {
        const cache = await caches.open(key);
        urls.push(...(await cache.keys()).map((request) => new URL(request.url).pathname));
      }

      return urls;
    });
    expect(cached.filter((path) => path.startsWith('/api/') || path.startsWith('/o/'))).toEqual([]);
    expect(
      cached.every((path) =>
        /^\/(index\.html|manifest\.webmanifest|icons\/.+|.+\.(js|css|woff2?|svg|ico|png))?$/.test(
          path,
        ),
      ),
    ).toBe(true);
  });
});
