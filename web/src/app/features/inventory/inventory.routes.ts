import { Routes } from '@angular/router';
import { messages, pageTitle } from '../../core/i18n/messages.pt-BR';
import { permissionGuard } from '../auth/auth.guards';

const text = messages.inventory;

export const INVENTORY_ROUTES: Routes = [
  {
    path: 'inventory/parts/:partId',
    title: pageTitle(text.movements.screen),
    canActivate: [permissionGuard('inventory.view')],
    loadComponent: () =>
      import('./pages/part-movements-page').then((module) => module.PartMovementsPage),
  },
  {
    path: 'inventory/counts/:id',
    title: pageTitle(text.count.screen),
    canActivate: [permissionGuard('inventory.count')],
    loadComponent: () => import('./pages/count-page').then((module) => module.CountPage),
  },
  {
    path: 'inventory',
    title: pageTitle(messages.screens.inventory),
    canActivate: [permissionGuard('inventory.view')],
    loadComponent: () => import('./pages/inventory-page').then((module) => module.InventoryPage),
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () =>
          import('./pages/stock-overview-page').then((module) => module.StockOverviewPage),
      },
      {
        path: 'counts',
        title: pageTitle(text.sections.counts),
        canActivate: [permissionGuard('inventory.count')],
        loadComponent: () => import('./pages/counts-page').then((module) => module.CountsPage),
      },
      {
        path: 'purchase-orders',
        title: pageTitle(text.sections.orders),
        canActivate: [permissionGuard('inventory.manage')],
        loadComponent: () =>
          import('./pages/purchase-orders-page').then((module) => module.PurchaseOrdersPage),
      },
      {
        path: 'suppliers',
        title: pageTitle(text.sections.suppliers),
        loadComponent: () =>
          import('./pages/suppliers-page').then((module) => module.SuppliersPage),
      },
    ],
  },
];
