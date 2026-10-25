import { HttpEvent, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, Observable, switchMap, throwError } from 'rxjs';
import { SessionService } from '../../auth/session.service';
import { messages } from '../../i18n/messages.pt-BR';
import { ToastService } from '../../../shared/ui/toast/toast';
import { ApiError, NETWORK_ERROR_CODE, TIMEOUT_ERROR_CODE, toApiError } from '../api-error';
import { SESSION_RENEWED, SKIP_GLOBAL_ERROR_HANDLING } from '../http-context';
import { traceIdOf } from './correlation.interceptor';

export const FORBIDDEN_ROUTE = '/forbidden';

function shouldToast(error: ApiError): boolean {
  return (
    error.status >= 500 || error.code === NETWORK_ERROR_CODE || error.code === TIMEOUT_ERROR_CODE
  );
}

export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const session = inject(SessionService);
  const router = inject(Router);
  const toast = inject(ToastService);

  const renewAndRetry = (
    current: HttpRequest<unknown>,
    apiError: ApiError,
    handler: HttpHandlerFn,
  ): Observable<HttpEvent<unknown>> => {
    if (current.context.get(SESSION_RENEWED)) {
      session.expire();
      return throwError(() => apiError);
    }

    return session.renew().pipe(
      switchMap((renewed) => {
        if (!renewed) {
          session.expire();
          return throwError(() => apiError);
        }

        const retry = current.clone({
          setHeaders: { Authorization: `Bearer ${renewed.accessToken}` },
          context: current.context.set(SESSION_RENEWED, true),
        });
        return handler(retry).pipe(catchError((retryError: unknown) => handle(retry, retryError)));
      }),
    );
  };

  const handle = (
    current: HttpRequest<unknown>,
    error: unknown,
  ): Observable<HttpEvent<unknown>> => {
    const apiError = toApiError(error, traceIdOf(current));
    if (current.context.get(SKIP_GLOBAL_ERROR_HANDLING)) {
      return throwError(() => apiError);
    }

    if (apiError.status === 401) {
      return renewAndRetry(current, apiError, next);
    }

    if (apiError.status === 403) {
      void router.navigateByUrl(FORBIDDEN_ROUTE);
    } else if (shouldToast(apiError)) {
      toast.show(apiError.detail, {
        tone: 'error',
        durationMs: 0,
        ...(apiError.traceId ? { detail: messages.errors.supportCode(apiError.traceId) } : {}),
      });
    }

    return throwError(() => apiError);
  };

  return next(request).pipe(catchError((error: unknown) => handle(request, error)));
};
