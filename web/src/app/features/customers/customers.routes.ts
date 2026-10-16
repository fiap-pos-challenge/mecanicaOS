import { Routes } from '@angular/router';
import { messages, pageTitle } from '../../core/i18n/messages.pt-BR';
import { permissionGuard } from '../auth/auth.guards';

const text = messages.customers;

export const CUSTOMER_ROUTES: Routes = [
  {
    path: 'customers/new',
    title: pageTitle(text.form.newScreen),
    canActivate: [permissionGuard('customers.manage')],
    loadComponent: () =>
      import('./pages/customer-form-page').then((module) => module.CustomerFormPage),
  },
  {
    path: 'customers/:id/edit',
    title: pageTitle(text.form.editScreen),
    canActivate: [permissionGuard('customers.manage')],
    loadComponent: () =>
      import('./pages/customer-form-page').then((module) => module.CustomerFormPage),
  },
  {
    path: 'customers',
    title: pageTitle(messages.screens.customers),
    canActivate: [permissionGuard('customers.view')],
    loadComponent: () => import('./pages/customers-page').then((module) => module.CustomersPage),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/customer-empty-page').then((module) => module.CustomerEmptyPage),
      },
      {
        path: ':id',
        title: pageTitle(text.detail.screen),
        loadComponent: () =>
          import('./pages/customer-detail-page').then((module) => module.CustomerDetailPage),
      },
    ],
  },
];
