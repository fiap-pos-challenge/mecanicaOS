import { HttpInterceptorFn, provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { EnvironmentProviders, Provider } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideSessionData } from '../app/core/auth/session.gateway';
import { API_ORIGIN, APP_ENVIRONMENT } from '../app/core/config/app-environment';
import { TEST_ORIGIN, testEnvironment } from './api';

export const API_URL = `${TEST_ORIGIN}/api/admin/customers`;

export function provideInterceptorTest(
  ...interceptors: HttpInterceptorFn[]
): (Provider | EnvironmentProviders)[] {
  return [
    provideRouter([]),
    provideHttpClient(withInterceptors(interceptors)),
    provideHttpClientTesting(),
    provideSessionData(),
    { provide: API_ORIGIN, useValue: TEST_ORIGIN },
    { provide: APP_ENVIRONMENT, useValue: testEnvironment() },
  ];
}
