import { Route, Routes } from '@angular/router';
import { messages, pageTitle } from '../../core/i18n/messages.pt-BR';
import { permissionGuard } from '../auth/auth.guards';
import { CatalogKind } from './data/catalog.models';

const text = messages.catalog;

const forms: Readonly<Record<CatalogKind, NonNullable<Route['loadComponent']>>> = {
  services: () => import('./pages/service-form-page').then((module) => module.ServiceFormPage),
  packages: () => import('./pages/package-form-page').then((module) => module.PackageFormPage),
  parts: () => import('./pages/part-form-page').then((module) => module.PartFormPage),
};

const formRoutes = (kind: CatalogKind): Routes => [
  {
    path: `catalog/${kind}/new`,
    title: pageTitle(text.form.newScreen),
    canActivate: [permissionGuard('catalog.manage')],
    loadComponent: forms[kind],
  },
  {
    path: `catalog/${kind}/:id`,
    title: pageTitle(text.form.editScreen),
    canActivate: [permissionGuard('catalog.view')],
    loadComponent: forms[kind],
  },
];

const listPage = () => import('./pages/catalog-list-page').then((module) => module.CatalogListPage);

export const CATALOG_ROUTES: Routes = [
  ...formRoutes('services'),
  ...formRoutes('packages'),
  ...formRoutes('parts'),
  {
    path: 'catalog',
    title: pageTitle(messages.screens.catalog),
    canActivate: [permissionGuard('catalog.view')],
    loadComponent: () => import('./pages/catalog-page').then((module) => module.CatalogPage),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'services' },
      { path: 'services', data: { kind: 'services' }, loadComponent: listPage },
      { path: 'packages', data: { kind: 'packages' }, loadComponent: listPage },
      { path: 'parts', data: { kind: 'parts' }, loadComponent: listPage },
    ],
  },
];
