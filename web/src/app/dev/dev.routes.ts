import { Routes } from '@angular/router';
import { messages, pageTitle } from '../core/i18n/messages.pt-BR';

export const devRoutes: Routes = [
  {
    path: 'dev/ui',
    title: pageTitle(messages.screens.uiCatalog),
    loadComponent: () =>
      import('./ui-catalog/ui-catalog-page').then((module) => module.UiCatalogPage),
  },
];
