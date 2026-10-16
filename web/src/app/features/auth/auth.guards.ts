import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Permission } from '../../core/auth/permissions';
import { LOGIN_ROUTE } from '../../core/auth/session.service';
import { FORBIDDEN_ROUTE } from '../../core/http/interceptors/error.interceptor';
import { AuthStore } from './state/auth.store';

export const authGuard: CanActivateFn = (_route, state) => {
  const store = inject(AuthStore);
  if (store.isAuthenticated()) {
    return true;
  }

  store.rememberReturnUrl(state.url);
  return inject(Router).parseUrl(LOGIN_ROUTE);
};

export const guestGuard: CanActivateFn = () => {
  const store = inject(AuthStore);
  return store.isAuthenticated() ? inject(Router).parseUrl(store.home()) : true;
};

export const twoFactorGuard: CanActivateFn = () =>
  inject(AuthStore).status() === 'twoFactor' ? true : inject(Router).parseUrl(LOGIN_ROUTE);

export function permissionGuard(permission: Permission): CanActivateFn {
  return () =>
    inject(AuthStore).can(permission) ? true : inject(Router).parseUrl(FORBIDDEN_ROUTE);
}

export function homeRedirect(): string {
  return inject(AuthStore).home();
}
