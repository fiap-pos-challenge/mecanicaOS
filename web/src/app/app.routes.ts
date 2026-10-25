import { Routes } from '@angular/router';
import { pageTitle, messages } from './core/i18n/messages.pt-BR';
import { devRoutes } from './dev/dev.routes';
import {
  authGuard,
  guestGuard,
  homeRedirect,
  permissionGuard,
  twoFactorGuard,
} from './features/auth/auth.guards';
import { CATALOG_ROUTES } from './features/catalog/catalog.routes';
import { CUSTOMER_ROUTES } from './features/customers/customers.routes';
import { INVENTORY_ROUTES } from './features/inventory/inventory.routes';
import { QUOTE_ROUTES } from './features/quotes/quotes.routes';
import { SERVICE_ORDER_ROUTES } from './features/service-orders/service-orders.routes';
import { TEAM_ROUTES } from './features/team/team.routes';

export const routes: Routes = [
  {
    path: 'login',
    title: pageTitle(messages.auth.login.screen),
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/pages/login-page').then((module) => module.LoginPage),
  },
  {
    path: 'login/verify',
    title: pageTitle(messages.auth.twoFactor.screen),
    canActivate: [twoFactorGuard],
    loadComponent: () =>
      import('./features/auth/pages/two-factor-page').then((module) => module.TwoFactorPage),
  },
  {
    path: 'invite',
    title: pageTitle(messages.auth.invitation.screen),
    loadComponent: () =>
      import('./features/auth/pages/invite-page').then((module) => module.InvitePage),
  },
  {
    path: 'error',
    title: pageTitle(messages.pages.serverError.screen),
    loadComponent: () =>
      import('./layout/pages/server-error-page').then((module) => module.ServerErrorPage),
  },
  {
    path: 'update',
    title: pageTitle(messages.pages.update.screen),
    loadComponent: () => import('./layout/pages/update-page').then((module) => module.UpdatePage),
  },
  {
    path: 'o/:token',
    title: pageTitle(messages.portal.screen),
    loadComponent: () =>
      import('./features/portal/pages/portal-page').then((module) => module.PortalPage),
  },
  {
    path: 'v/:code',
    title: pageTitle(messages.documents.verify.screen),
    loadComponent: () =>
      import('./features/documents/pages/verify-page').then((module) => module.VerifyPage),
  },
  {
    path: 'portal',
    title: pageTitle(messages.screens.portal),
    loadComponent: () =>
      import('./features/portal/pages/track-page').then((module) => module.TrackPage),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell/app-shell').then((module) => module.AppShell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: homeRedirect },
      {
        path: 'dashboard',
        title: pageTitle(messages.screens.dashboard),
        canActivate: [permissionGuard('dashboard.view')],
        loadChildren: () =>
          import('./features/dashboard/dashboard.routes').then(
            (module) => module.DASHBOARD_CHILD_ROUTES,
          ),
      },
      ...CUSTOMER_ROUTES,
      ...TEAM_ROUTES,
      ...CATALOG_ROUTES,
      ...INVENTORY_ROUTES,
      ...QUOTE_ROUTES,
      ...SERVICE_ORDER_ROUTES,
      {
        path: 'settings',
        title: pageTitle(messages.screens.settings),
        canActivate: [permissionGuard('settings.view')],
        loadChildren: () =>
          import('./features/settings/settings.routes').then(
            (module) => module.SETTINGS_CHILD_ROUTES,
          ),
      },
      {
        path: 'automations',
        title: pageTitle(messages.screens.automations),
        canActivate: [permissionGuard('automations.view')],
        loadChildren: () =>
          import('./features/automations/automations.routes').then(
            (module) => module.AUTOMATION_CHILD_ROUTES,
          ),
      },
      {
        path: 'reports',
        title: pageTitle(messages.screens.reports),
        canActivate: [permissionGuard('reports.view')],
        loadChildren: () =>
          import('./features/reports/reports.routes').then((module) => module.REPORT_CHILD_ROUTES),
      },
      {
        path: 'mechanic',
        title: pageTitle(messages.screens.mechanic),
        canActivate: [permissionGuard('mechanic.view')],
        loadChildren: () =>
          import('./features/mechanic/mechanic.routes').then(
            (module) => module.MECHANIC_CHILD_ROUTES,
          ),
      },
      {
        path: 'finance',
        title: pageTitle(messages.screens.finance),
        canActivate: [permissionGuard('finance.view')],
        loadComponent: () =>
          import('./features/finance/pages/finance-page').then((module) => module.FinancePage),
      },
      {
        path: 'documents',
        title: pageTitle(messages.screens.documents),
        canActivate: [permissionGuard('documents.view')],
        loadComponent: () =>
          import('./features/documents/pages/documents-page').then(
            (module) => module.DocumentsPage,
          ),
      },
      {
        path: 'platform',
        title: pageTitle(messages.screens.platform),
        canActivate: [permissionGuard('platform.view')],
        loadComponent: () =>
          import('./features/finance/pages/platform-page').then((module) => module.PlatformPage),
      },
      {
        path: 'forbidden',
        title: pageTitle(messages.pages.forbidden.screen),
        loadComponent: () =>
          import('./layout/pages/forbidden-page').then((module) => module.ForbiddenPage),
      },
      ...devRoutes,
      {
        path: '**',
        title: pageTitle(messages.pages.notFound.screen),
        loadComponent: () =>
          import('./layout/pages/not-found-page').then((module) => module.NotFoundPage),
      },
    ],
  },
];
