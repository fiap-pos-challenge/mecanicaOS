import {
  HttpErrorResponse,
  HttpHeaders,
  HttpInterceptorFn,
  HttpResponse,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { NEVER, of, throwError } from 'rxjs';
import { FakeFeatureApi } from './fake-api/fake-backend';
import { FakeBackend } from './fake-backend.service';
import { FakeRequest, isFakeTimeout } from './fake-request';

export type FakeHandlerLookup = (path: string, method: string) => FakeFeatureApi | null;

export function createFakeInterceptor(
  lookup: () => FakeHandlerLookup,
  passThrough: boolean,
): HttpInterceptorFn {
  return (request, next) => {
    const url = new URL(request.urlWithParams, 'http://fake.local');
    if (!url.pathname.startsWith('/api/')) {
      return next(request);
    }

    const handler = lookup()(url.pathname, request.method);
    if (!handler) {
      return passThrough
        ? next(request)
        : throwError(() => new HttpErrorResponse({ status: 404, url: request.url }));
    }

    return answer(handler, request, url);
  };
}

export const fakeApiInterceptor: HttpInterceptorFn = createFakeInterceptor(() => {
  const backend = inject(FakeBackend);
  return (path, method) => backend.handlerFor(path, method);
}, false);

function answer(
  handler: FakeFeatureApi,
  request: Parameters<HttpInterceptorFn>[0],
  url: URL,
): ReturnType<HttpInterceptorFn> {
  const fakeRequest: FakeRequest = {
    method: request.method,
    path: url.pathname,
    query: url.searchParams,
    body: request.body,
    authorization: request.headers.get('Authorization'),
    portalSession: request.headers.get('X-Portal-Session'),
    reauthentication: request.headers.get('X-Reauthentication'),
  };
  const result = handler.handle(fakeRequest);
  if (result === null) {
    return throwError(() => new HttpErrorResponse({ status: 404, url: request.url }));
  }

  if (isFakeTimeout(result)) {
    return NEVER;
  }

  const headers = new HttpHeaders(result.headers ?? {});
  return result.status < 400
    ? of(new HttpResponse({ status: result.status, body: result.body, headers, url: request.url }))
    : throwError(
        () =>
          new HttpErrorResponse({
            status: result.status,
            error: result.body,
            headers,
            url: request.url,
          }),
      );
}
