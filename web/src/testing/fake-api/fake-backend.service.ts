import { inject, Injectable, InjectionToken } from '@angular/core';
import { createAddressesFakeApi } from './addresses/addresses.fake-api';
import { createAuthFakeApi } from './auth/auth.fake-api';
import { createCatalogFakeApi } from './catalog/catalog.fake-api';
import { createCustomersFakeApi } from './customers/customers.fake-api';
import { createInsightsFakeApi } from './customers/insights.fake-api';
import {
  createFakeBackendState,
  FakeBackendState,
  FakeFeatureApi,
  resetFakeBackendState,
} from './fake-api/fake-backend';
import { FAKE_FEATURES, FakeFeature, FakeScenario } from './fake-api/fake-scenarios';
import { createInventoryFakeApi } from './inventory/inventory.fake-api';
import { createJobsFakeApi } from './jobs/jobs.fake-api';
import { createPortalFakeApi } from './portal/portal.fake-api';
import { createDashboardFakeApi } from './dashboard/dashboard.fake-api';
import { createReportsFakeApi } from './reports/reports.fake-api';
import { createAutomationsFakeApi } from './automations/automations.fake-api';
import { createNotificationsFakeApi } from './notifications/notifications.fake-api';
import { createAssistantFakeApi } from './assistant/assistant.fake-api';
import { createSettingsFakeApi } from './settings/settings.fake-api';
import { createFinanceFakeApi } from './finance/finance.fake-api';
import { createQuotesFakeApi } from './quotes/quotes.fake-api';
import { createServiceOrdersFakeApi } from './service-orders/service-orders.fake-api';
import { createRolesFakeApi } from './roles/roles.fake-api';
import { createTeamFakeApi } from './team/team.fake-api';
import { createUsersFakeApi } from './users/users.fake-api';
import { createVehiclesFakeApi } from './vehicles/vehicles.fake-api';

export const FAKE_API_MARKER = 'mecanicaos-fake-api';

export function createFakeApis(state = createFakeBackendState()): Readonly<
  Record<FakeFeature, FakeFeatureApi>
> & {
  readonly state: typeof state;
  readonly marker: typeof FAKE_API_MARKER;
} {
  return {
    marker: FAKE_API_MARKER,
    state,
    auth: createAuthFakeApi(state),
    customers: createCustomersFakeApi(state),
    users: createUsersFakeApi(state),
    vehicles: createVehiclesFakeApi(state),
    insights: createInsightsFakeApi(state),
    addresses: createAddressesFakeApi(state),
    jobs: createJobsFakeApi(state),
    team: createTeamFakeApi(state),
    roles: createRolesFakeApi(state),
    catalog: createCatalogFakeApi(state),
    inventory: createInventoryFakeApi(state),
    quotes: createQuotesFakeApi(state),
    serviceOrders: createServiceOrdersFakeApi(state),
    portal: createPortalFakeApi(state),
    dashboard: createDashboardFakeApi(state),
    reports: createReportsFakeApi(state),
    automations: createAutomationsFakeApi(state),
    notifications: createNotificationsFakeApi(state),
    assistant: createAssistantFakeApi(state),
    settings: createSettingsFakeApi(state),
    finance: createFinanceFakeApi(state),
  };
}

export type FakeApis = ReturnType<typeof createFakeApis>;

export const FAKE_APIS = new InjectionToken<FakeApis>('FAKE_APIS', {
  providedIn: 'root',
  factory: () => createFakeApis(),
});

@Injectable({ providedIn: 'root' })
export class FakeBackend {
  private readonly apis = inject(FAKE_APIS);

  get state(): FakeBackendState {
    return this.apis.state;
  }

  api(feature: FakeFeature): FakeFeatureApi {
    return this.apis[feature];
  }

  handlerFor(path: string, method: string): FakeFeatureApi | null {
    return (
      FAKE_FEATURES.map((feature) => this.apis[feature]).find((api) => api.handles(path, method)) ??
      null
    );
  }

  scenario(feature: FakeFeature): FakeScenario {
    return this.state.scenarios.get(feature);
  }

  setScenario(feature: FakeFeature, scenario: FakeScenario): void {
    this.state.scenarios.set(feature, scenario);
  }

  reset(): void {
    resetFakeBackendState(this.state);
  }
}
