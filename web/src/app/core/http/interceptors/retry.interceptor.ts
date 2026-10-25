import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { retry, throwError, timer, TimeoutError } from 'rxjs';
import { RETRY_ENABLED } from '../http-context';

export const MAX_RETRIES = 2;
export const BASE_DELAY_MS = 300;
const retryableStatuses = new Set([0, 408, 429, 502, 503, 504]);

export function isRetryable(error: unknown): boolean {
  return (
    error instanceof TimeoutError ||
    (error instanceof HttpErrorResponse && retryableStatuses.has(error.status))
  );
}

export function retryAfterSeconds(error: unknown): number | null {
  if (!(error instanceof HttpErrorResponse)) {
    return null;
  }

  const value = Number(error.headers.get('Retry-After'));
  return Number.isFinite(value) && value > 0 ? value : null;
}

export function backoffMs(attempt: number, retryAfter: number | null): number {
  return retryAfter === null ? BASE_DELAY_MS * 2 ** (attempt - 1) : retryAfter * 1000;
}

export const retryInterceptor: HttpInterceptorFn = (request, next) => {
  if (request.method !== 'GET' || !request.context.get(RETRY_ENABLED)) {
    return next(request);
  }

  return next(request).pipe(
    retry({
      count: MAX_RETRIES,
      delay: (error: unknown, attempt) =>
        isRetryable(error)
          ? timer(backoffMs(attempt, retryAfterSeconds(error)))
          : throwError(() => error),
    }),
  );
};
