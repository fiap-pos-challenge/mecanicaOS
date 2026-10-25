import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { patchState } from '@ngrx/signals';
import { unprotected } from '@ngrx/signals/testing';
import { provideTestApi } from '../../../testing/api';
import { signInAs } from '../../../testing/auth';
import { provideAuthData } from './data/auth.providers';
import {
  authGuard,
  guestGuard,
  homeRedirect,
  permissionGuard,
  twoFactorGuard,
} from './auth.guards';
import { AuthStore } from './state/auth.store';

function run(guard: typeof authGuard, url = '/customers'): boolean | UrlTree {
  return TestBed.runInInjectionContext(
    () => guard({} as ActivatedRouteSnapshot, { url } as RouterStateSnapshot) as boolean | UrlTree,
  );
}

function path(result: boolean | UrlTree): string | boolean {
  return result instanceof UrlTree ? TestBed.inject(Router).serializeUrl(result) : result;
}

describe('auth guards', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({ providers: [...provideTestApi(), provideAuthData()] }),
  );

  it('sends visitors to the login and remembers where they were going', () => {
    expect(path(run(authGuard, '/customers?page=2'))).toBe('/login');
    expect(TestBed.inject(AuthStore).returnUrl()).toBe('/customers?page=2');

    signInAs('Attendant');
    expect(run(authGuard)).toBe(true);
  });

  it('keeps signed-in users out of the login screen', () => {
    expect(run(guestGuard)).toBe(true);
    signInAs('Mechanic');
    expect(path(run(guestGuard))).toBe('/mechanic');
  });

  it('opens the two-factor screen only with a pending challenge', () => {
    expect(path(run(twoFactorGuard))).toBe('/login');
    patchState(unprotected(TestBed.inject(AuthStore)), { status: 'twoFactor', challengeId: 'c1' });
    expect(run(twoFactorGuard)).toBe(true);
  });

  it('blocks routes outside the profile with the access denied screen', () => {
    signInAs('Mechanic');

    expect(run(permissionGuard('mechanic.view'))).toBe(true);
    expect(path(run(permissionGuard('customers.view')))).toBe('/forbidden');
    expect(TestBed.runInInjectionContext(() => homeRedirect())).toBe('/mechanic');
  });
});
