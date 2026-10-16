import { HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { isApiUrl } from '../../api/api-url';
import { API_ORIGIN, APP_ENVIRONMENT } from '../../config/app-environment';

export const TRACEPARENT_HEADER = 'traceparent';

function toHex(bytes: Uint8Array): string {
  return Array.from(bytes, (byte) => byte.toString(16).padStart(2, '0')).join('');
}

export function createTraceparent(
  random: (length: number) => Uint8Array = (length) =>
    crypto.getRandomValues(new Uint8Array(length)),
): string {
  return `00-${toHex(random(16))}-${toHex(random(8))}-01`;
}

export function traceIdOf(request: HttpRequest<unknown>): string | null {
  return request.headers.get(TRACEPARENT_HEADER);
}

export const correlationInterceptor: HttpInterceptorFn = (request, next) => {
  if (!isApiUrl(request.url, inject(API_ORIGIN), inject(APP_ENVIRONMENT).apiBaseUrl)) {
    return next(request);
  }

  return next(request.clone({ setHeaders: { [TRACEPARENT_HEADER]: createTraceparent() } }));
};
