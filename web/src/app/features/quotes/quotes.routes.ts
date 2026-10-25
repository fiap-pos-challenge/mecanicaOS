import { Routes } from '@angular/router';
import { messages, pageTitle } from '../../core/i18n/messages.pt-BR';
import { permissionGuard } from '../auth/auth.guards';

export const QUOTE_ROUTES: Routes = [
  {
    path: 'quotes',
    title: pageTitle(messages.screens.quotes),
    canActivate: [permissionGuard('quotes.view')],
    loadComponent: () => import('./pages/quotes-page').then((module) => module.QuotesPage),
  },
  {
    path: 'quotes/:id',
    title: pageTitle(messages.screens.quotes),
    canActivate: [permissionGuard('quotes.view')],
    loadComponent: () =>
      import('./pages/quote-editor-page').then((module) => module.QuoteEditorPage),
  },
];
