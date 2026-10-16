import {
  HttpInterceptorFn,
  provideHttpClient,
  withFetch,
  withInterceptors,
} from '@angular/common/http';
import { EnvironmentProviders } from '@angular/core';
import { authInterceptor } from './interceptors/auth.interceptor';
import { correlationInterceptor } from './interceptors/correlation.interceptor';
import { errorInterceptor } from './interceptors/error.interceptor';
import { retryInterceptor } from './interceptors/retry.interceptor';
import { timeoutInterceptor } from './interceptors/timeout.interceptor';

export const API_INTERCEPTORS = [
  correlationInterceptor,
  authInterceptor,
  errorInterceptor,
  retryInterceptor,
  timeoutInterceptor,
] as const;

export function provideApi(extra: readonly HttpInterceptorFn[] = []): EnvironmentProviders {
  return provideHttpClient(withFetch(), withInterceptors([...API_INTERCEPTORS, ...extra]));
}
