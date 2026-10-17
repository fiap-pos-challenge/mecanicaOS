import { Page } from '@playwright/test';
import {
  createFakeBackendState,
  FakeBackendState,
} from '../src/testing/fake-api/fake-api/fake-backend';
import { createAssistantFakeApi } from '../src/testing/fake-api/assistant/assistant.fake-api';
import { createAutomationsFakeApi } from '../src/testing/fake-api/automations/automations.fake-api';
import { createDashboardFakeApi } from '../src/testing/fake-api/dashboard/dashboard.fake-api';
import { isFakeTimeout } from '../src/testing/fake-api/fake-request';
import { createNotificationsFakeApi } from '../src/testing/fake-api/notifications/notifications.fake-api';
import { createSettingsFakeApi } from '../src/testing/fake-api/settings/settings.fake-api';
import { createPortalFakeApi } from '../src/testing/fake-api/portal/portal.fake-api';
import { createQuotesFakeApi } from '../src/testing/fake-api/quotes/quotes.fake-api';
import {
  createReportsFakeApi,
  FAKE_REPORT_FILES,
} from '../src/testing/fake-api/reports/reports.fake-api';
import { createServiceOrdersFakeApi } from '../src/testing/fake-api/service-orders/service-orders.fake-api';

export const INTERCEPTED_ROUTES = [
  '**/api/admin/quotes**',
  '**/api/admin/service-orders**',
  '**/api/admin/documents/**',
  '**/api/public/portal/**',
  '**/api/public/documents/**',
  '**/api/admin/dashboard**',
  '**/api/admin/reports/**',
  '**/api/admin/automations/**',
  '**/api/admin/notifications**',
  '**/api/admin/assistant/**',
  '**/api/admin/settings/**',
  '**/api/auth/reauthenticate',
];

export const SAMPLE_PDF = Buffer.from(
  ['%PDF-1.4', '1 0 obj<<>>endobj', 'trailer<<>>', '%%EOF', ''].join('\n'),
);

export async function interceptFakeApis(page: Page): Promise<FakeBackendState> {
  const state = createFakeBackendState();
  const apis = [
    createQuotesFakeApi(state),
    createServiceOrdersFakeApi(state),
    createPortalFakeApi(state),
    createDashboardFakeApi(state),
    createReportsFakeApi(state),
    createAutomationsFakeApi(state),
    createNotificationsFakeApi(state),
    createAssistantFakeApi(state),
    createSettingsFakeApi(state),
  ];
  for (const file of Object.values(FAKE_REPORT_FILES)) {
    await page.route(`**${file}`, (route) =>
      route.fulfill({ status: 200, contentType: 'application/octet-stream', body: SAMPLE_PDF }),
    );
  }

  await page.route('**/api/hubs/**', (route) => route.abort());
  await page.route('**/documents/exemplo.pdf', (route) =>
    route.fulfill({ status: 200, contentType: 'application/pdf', body: SAMPLE_PDF }),
  );
  for (const pattern of INTERCEPTED_ROUTES) {
    await page.route(pattern, async (route) => {
      const request = route.request();
      const url = new URL(request.url());
      const api = apis.find((candidate) => candidate.handles(url.pathname, request.method()));
      const result = api?.handle({
        method: request.method(),
        path: url.pathname,
        query: url.searchParams,
        body: request.postData() ? (request.postDataJSON() as unknown) : null,
        authorization: await request.headerValue('authorization'),
        portalSession: await request.headerValue('x-portal-session'),
        reauthentication: await request.headerValue('x-reauthentication'),
      });
      if (!result || isFakeTimeout(result)) {
        await route.fallback();
        return;
      }

      await route.fulfill({
        status: result.status,
        headers: {
          ...(result.headers ?? {}),
          'content-type': result.status >= 400 ? 'application/problem+json' : 'application/json',
        },
        body: result.body === null ? '' : JSON.stringify(result.body),
      });
    });
  }

  return state;
}
