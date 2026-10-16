import type { HttpHandler } from 'msw';
import { addressesHandlers } from './addresses/addresses.handlers';
import { authHandlers } from './auth/auth.handlers';
import { catalogHandlers } from './catalog/catalog.handlers';
import { customersHandlers } from './customers/customers.handlers';
import { insightsHandlers } from './customers/insights.handlers';
import { createFakeApis } from './fake-backend.service';
import { MswOptions } from './msw-handlers';
import { inventoryHandlers } from './inventory/inventory.handlers';
import { jobsHandlers } from './jobs/jobs.handlers';
import { portalHandlers } from './portal/portal.handlers';
import { dashboardHandlers } from './dashboard/dashboard.handlers';
import { reportsHandlers } from './reports/reports.handlers';
import { automationsHandlers } from './automations/automations.handlers';
import { notificationsHandlers } from './notifications/notifications.handlers';
import { assistantHandlers } from './assistant/assistant.handlers';
import { settingsHandlers } from './settings/settings.handlers';
import { quotesHandlers } from './quotes/quotes.handlers';
import { serviceOrdersHandlers } from './service-orders/service-orders.handlers';
import { rolesHandlers } from './roles/roles.handlers';
import { teamHandlers } from './team/team.handlers';
import { usersHandlers } from './users/users.handlers';
import { vehiclesHandlers } from './vehicles/vehicles.handlers';

export function createHandlers(
  apis: ReturnType<typeof createFakeApis>,
  options: MswOptions,
): readonly HttpHandler[] {
  return [
    ...authHandlers(apis.auth, options),
    ...customersHandlers(apis.customers, options),
    ...usersHandlers(apis.users, options),
    ...vehiclesHandlers(apis.vehicles, options),
    ...insightsHandlers(apis.insights, options),
    ...addressesHandlers(apis.addresses, options),
    ...jobsHandlers(apis.jobs, options),
    ...teamHandlers(apis.team, options),
    ...rolesHandlers(apis.roles, options),
    ...catalogHandlers(apis.catalog, options),
    ...inventoryHandlers(apis.inventory, options),
    ...quotesHandlers(apis.quotes, options),
    ...serviceOrdersHandlers(apis.serviceOrders, options),
    ...portalHandlers(apis.portal, options),
    ...dashboardHandlers(apis.dashboard, options),
    ...reportsHandlers(apis.reports, options),
    ...automationsHandlers(apis.automations, options),
    ...notificationsHandlers(apis.notifications, options),
    ...assistantHandlers(apis.assistant, options),
    ...settingsHandlers(apis.settings, options),
  ];
}
