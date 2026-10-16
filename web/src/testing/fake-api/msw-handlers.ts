import { delay, http, HttpResponse, type HttpHandler } from 'msw';
import { FakeBackendHandler, isFakeTimeout } from './fake-request';

export interface MswOptions {
  readonly latencyMs: number;
}

async function readRequestBody(request: Request): Promise<unknown> {
  const text = await request.text();
  return text ? (JSON.parse(text) as unknown) : null;
}

export function toMswHandler(
  api: FakeBackendHandler & { handles(path: string, method?: string): boolean },
  options: MswOptions,
): HttpHandler {
  return http.all('*/api/*', async ({ request }) => {
    const url = new URL(request.url);
    if (!api.handles(url.pathname, request.method)) {
      return undefined;
    }

    const result = api.handle({
      method: request.method,
      path: url.pathname,
      query: url.searchParams,
      body: await readRequestBody(request),
      authorization: request.headers.get('Authorization'),
      portalSession: request.headers.get('X-Portal-Session'),
      reauthentication: request.headers.get('X-Reauthentication'),
    });
    if (result !== null && isFakeTimeout(result)) {
      await delay('infinite');
    }

    if (options.latencyMs > 0) {
      await delay(options.latencyMs);
    }

    if (result === null || isFakeTimeout(result)) {
      return new HttpResponse(null, { status: 405 });
    }

    const headers = new Headers(result.headers ?? {});
    if (result.status === 204 || result.body === null) {
      return new HttpResponse(null, { status: result.status, headers });
    }

    headers.set(
      'Content-Type',
      result.status >= 400 ? 'application/problem+json' : 'application/json',
    );
    return new HttpResponse(JSON.stringify(result.body), { status: result.status, headers });
  });
}
