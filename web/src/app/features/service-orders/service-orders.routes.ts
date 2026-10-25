import { Routes } from '@angular/router';
import { messages, pageTitle } from '../../core/i18n/messages.pt-BR';
import { permissionGuard } from '../auth/auth.guards';

export const SERVICE_ORDER_ROUTES: Routes = [
  {
    path: 'service-orders',
    title: pageTitle(messages.screens.serviceOrders),
    canActivate: [permissionGuard('serviceOrders.view')],
    loadComponent: () =>
      import('./pages/service-orders-page').then((module) => module.ServiceOrdersPage),
  },
  {
    path: 'service-orders/:id',
    title: pageTitle(messages.screens.serviceOrders),
    canActivate: [permissionGuard('serviceOrders.view')],
    loadComponent: () =>
      import('./pages/service-order-detail-page').then((module) => module.ServiceOrderDetailPage),
  },
];
