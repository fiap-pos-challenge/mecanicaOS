import { inject } from '@angular/core';
import { NavigationError, RedirectCommand, Router } from '@angular/router';
import { isChunkLoadError, VersionService } from '../version/version.service';
import { ERROR_ROUTE, ErrorState, UPDATE_ROUTE } from './error-state';

export function handleNavigationError(navigationError: NavigationError): RedirectCommand {
  const router = inject(Router);
  if (isChunkLoadError(navigationError.error)) {
    inject(VersionService).markOutdated();
    return new RedirectCommand(router.parseUrl(UPDATE_ROUTE));
  }

  inject(ErrorState).capture(navigationError.error, navigationError.url);
  return new RedirectCommand(router.parseUrl(ERROR_ROUTE));
}
