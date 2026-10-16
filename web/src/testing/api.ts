import { EnvironmentProviders, Provider } from '@angular/core';
import { provideRouter, Routes } from '@angular/router';
import { setupServer } from 'msw/node';
import {
  API_ORIGIN,
  ApiMode,
  APP_ENVIRONMENT,
  AppEnvironment,
  FeatureName,
} from '../app/core/config/app-environment';
import { environment } from '../environments/environment';
import { provideSessionData } from '../app/core/auth/session.gateway';
import { provideApi } from '../app/core/http/provide-api';
import { provideFakeCharts } from './charts';
import { createFakeApis } from './fake-api/fake-backend.service';
import { fakeApiInterceptor } from './fake-api/fake-api.interceptor';
import { createHandlers } from './fake-api/handlers';

export const TEST_ORIGIN = 'http://localhost';

export type TestApiMode = 'http' | 'mock' | 'pending';

export function testEnvironment(mode: TestApiMode = 'http'): AppEnvironment {
  const features = Object.fromEntries(
    (Object.keys(environment.features) as FeatureName[]).map((feature) => [
      feature,
      {
        api:
          mode === 'pending'
            ? environment.features[feature].api
            : feature === 'realtime'
              ? 'pending'
              : 'http',
      },
    ]),
  ) as Record<FeatureName, { api: ApiMode }>;
  return {
    apiBaseUrl: '/api',
    realtimeHubPath: '/api/hubs/realtime',
    paymentProviderOrigins: ['https://pagamento.provedor.example'],
    session: {
      idleWarningMs: 25 * 60_000,
      idleLogoutMs: 30 * 60_000,
      versionCheckIntervalMs: 60_000,
    },
    features,
  };
}

export function provideTestApi(
  mode: TestApiMode = 'http',
  routes: Routes = [],
): (Provider | EnvironmentProviders)[] {
  return [
    provideRouter(routes),
    provideApi(mode === 'http' ? [] : [fakeApiInterceptor]),
    provideSessionData(),
    { provide: API_ORIGIN, useValue: TEST_ORIGIN },
    { provide: APP_ENVIRONMENT, useValue: testEnvironment(mode) },
    provideFakeCharts(),
  ];
}

export function createMockServer(): {
  readonly server: ReturnType<typeof setupServer>;
  readonly apis: ReturnType<typeof createFakeApis>;
} {
  const apis = createFakeApis();
  return { server: setupServer(...createHandlers(apis, { latencyMs: 0 })), apis };
}
