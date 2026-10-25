import { TestBed } from '@angular/core/testing';
import { NavigationError, provideRouter, RedirectCommand, Router } from '@angular/router';
import { VersionService } from '../version/version.service';
import { ERROR_ROUTE, ErrorState, UPDATE_ROUTE } from './error-state';
import { handleNavigationError } from './navigation-error';

describe('ErrorState and navigation errors', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideRouter([])] }));

  it('captures unknown errors as api errors and shows the error screen', () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const state = TestBed.inject(ErrorState);

    state.show(new Error('boom'));

    expect(state.last()?.code).toBe('http.unknown');
    expect(state.failedUrl()).toBe('/');
    expect(navigate).toHaveBeenCalledWith(ERROR_ROUTE);
  });

  it('sends lazy chunk failures to the new version screen', () => {
    const redirect = TestBed.runInInjectionContext(() =>
      handleNavigationError(
        new NavigationError(
          1,
          '/customers',
          new TypeError('Failed to fetch dynamically imported module'),
        ),
      ),
    );

    expect(redirect).toBeInstanceOf(RedirectCommand);
    expect(TestBed.inject(Router).serializeUrl(redirect.redirectTo)).toBe(UPDATE_ROUTE);
    expect(TestBed.inject(VersionService).outdated()).toBe(true);
  });

  it('sends other navigation failures to the error screen keeping the address', () => {
    const redirect = TestBed.runInInjectionContext(() =>
      handleNavigationError(new NavigationError(1, '/reports', new Error('resolver failed'))),
    );

    expect(TestBed.inject(Router).serializeUrl(redirect.redirectTo)).toBe(ERROR_ROUTE);
    expect(TestBed.inject(ErrorState).failedUrl()).toBe('/reports');
  });
});
