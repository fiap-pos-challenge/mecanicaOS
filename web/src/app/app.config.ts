import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import {
  provideRouter,
  withComponentInputBinding,
  withNavigationErrorHandler,
} from '@angular/router';
import { routes } from './app.routes';
import { DEMO_INTERCEPTORS } from './demo/demo-mode';
import { handleNavigationError } from './core/errors/navigation-error';
import { provideApi } from './core/http/provide-api';
import { PwaService } from './core/pwa/pwa.service';
import { provideRealtime } from './core/realtime/realtime.providers';
import { provideAuthData } from './features/auth/data/auth.providers';
import { AuthStore } from './features/auth/state/auth.store';
import { provideCatalogData } from './features/catalog/data/catalog.gateway';
import { provideCustomersData } from './features/customers/data/customers.providers';
import { provideInventoryData } from './features/inventory/data/inventory.gateway';
import { provideDocumentsData } from './features/documents/data/documents.gateway';
import { provideQuoteData } from './features/quotes/data/quote.gateway';
import { provideTeamData } from './features/team/data/team.providers';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(
      routes,
      withComponentInputBinding(),
      withNavigationErrorHandler(handleNavigationError),
    ),
    provideApi(DEMO_INTERCEPTORS),
    provideAuthData(),
    provideCustomersData(),
    provideTeamData(),
    provideCatalogData(),
    provideInventoryData(),
    provideQuoteData(),
    provideDocumentsData(),
    provideRealtime(),
    provideAppInitializer(() => inject(AuthStore).restore()),
    provideAppInitializer(() => void inject(PwaService).register()),
  ],
};
