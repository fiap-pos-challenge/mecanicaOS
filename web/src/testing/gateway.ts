import { EnvironmentProviders, Provider } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthTokenStore } from '../app/core/auth/auth-token.store';
import { provideRealtime } from '../app/core/realtime/realtime.providers';
import { AuthGateway } from '../app/features/auth/data/auth.gateway';
import { provideAuthData } from '../app/features/auth/data/auth.providers';
import { provideCatalogData } from '../app/features/catalog/data/catalog.gateway';
import { provideCustomersData } from '../app/features/customers/data/customers.providers';
import { provideInventoryData } from '../app/features/inventory/data/inventory.gateway';
import { provideDocumentsData } from '../app/features/documents/data/documents.gateway';
import { provideMechanicData } from '../app/features/mechanic/data/mechanic.gateway';
import { providePortalData } from '../app/features/portal/data/portal.gateway';
import { provideQuoteData } from '../app/features/quotes/data/quote.gateway';
import { provideServiceOrderData } from '../app/features/service-orders/data/service-order.gateway';
import { provideTeamData } from '../app/features/team/data/team.providers';
import { FakeBackendState, resetFakeBackendState } from './fake-api/fake-api/fake-backend';
import { FAKE_ADMIN_EMAIL, FAKE_ADMIN_PASSWORD } from './fake-api/fake-api/fake-database';
import { FakeFeature, FakeScenario } from './fake-api/fake-api/fake-scenarios';
import { FakeBackend } from './fake-api/fake-backend.service';
import { createMockServer, provideTestApi } from './api';

export type ApiMode = 'http' | 'mock';

export const API_MODES: readonly ApiMode[] = ['http', 'mock'];

export interface RecordedRequest {
  readonly method: string;
  readonly url: string;
  readonly body: string;
}

export interface GatewayHarness {
  readonly requests: RecordedRequest[];
  readonly server: ReturnType<typeof createMockServer>['server'];
  configure(mode: ApiMode): { readonly navigate: ReturnType<typeof vi.spyOn> };
  providers(mode: ApiMode): (Provider | EnvironmentProviders)[];
  spyNavigation(): ReturnType<typeof vi.spyOn>;
  state(mode: ApiMode): FakeBackendState;
  scenario(mode: ApiMode, feature: FakeFeature, scenario: FakeScenario): void;
  login(email?: string): Promise<void>;
}

export function useGatewayHarness(): GatewayHarness {
  const { server, apis } = createMockServer();
  const requests: RecordedRequest[] = [];
  server.events.on('request:start', ({ request }) => {
    void request
      .clone()
      .text()
      .then((body) => requests.push({ method: request.method, url: request.url, body }));
  });
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => {
    resetFakeBackendState(apis.state);
    server.resetHandlers();
    requests.length = 0;
  });
  afterAll(() => server.close());

  const harness: GatewayHarness = {
    requests,
    server,
    providers: (mode) => [
      ...provideTestApi(mode),
      provideAuthData(),
      provideCustomersData(),
      ...provideTeamData(),
      provideCatalogData(),
      provideInventoryData(),
      provideQuoteData(),
      provideServiceOrderData(),
      provideMechanicData(),
      provideDocumentsData(),
      providePortalData(),
      provideRealtime(),
    ],
    spyNavigation: () => vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true),
    configure(mode) {
      TestBed.configureTestingModule({ providers: harness.providers(mode) });
      return {
        navigate: vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true),
      };
    },
    state: (mode) => (mode === 'http' ? apis.state : TestBed.inject(FakeBackend).state),
    scenario: (mode, feature, scenario) => harness.state(mode).scenarios.set(feature, scenario),
    async login(email = FAKE_ADMIN_EMAIL) {
      const result = await firstValueFrom(
        TestBed.inject(AuthGateway).login({ email, password: FAKE_ADMIN_PASSWORD }),
      );
      if (result.kind !== 'session') {
        throw new Error('Two-factor users cannot use the quick login helper.');
      }

      TestBed.inject(AuthTokenStore).set(result.session);
    },
  };
  return harness;
}
