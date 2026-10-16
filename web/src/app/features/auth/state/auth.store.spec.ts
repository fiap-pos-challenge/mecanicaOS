import { TestBed } from '@angular/core/testing';
import { throwError } from 'rxjs';
import { useGatewayHarness } from '../../../../testing/gateway';
import { AuthTokenStore } from '../../../core/auth/auth-token.store';
import { SessionService } from '../../../core/auth/session.service';
import { DraftStore } from '../../../core/drafts/draft.store';
import {
  FAKE_ADMIN_EMAIL,
  FAKE_ADMIN_PASSWORD,
} from '../../../../testing/fake-api/fake-api/fake-database';
import {
  FAKE_TWO_FACTOR_CODE,
  FAKE_TWO_FACTOR_EMAIL,
} from '../../../../testing/fake-api/fake-api/fake-auth-state';
import { AuthGateway } from '../data/auth.gateway';
import { AuthStore } from './auth.store';

const password = FAKE_ADMIN_PASSWORD;

describe('AuthStore', () => {
  const harness = useGatewayHarness();

  function setup(): {
    store: InstanceType<typeof AuthStore>;
    navigate: ReturnType<typeof vi.spyOn>;
  } {
    const { navigate } = harness.configure('http');
    return { store: TestBed.inject(AuthStore), navigate };
  }

  it('signs in, loads the user and opens the home of the profile', async () => {
    const { store, navigate } = setup();

    expect(await store.login({ email: FAKE_ADMIN_EMAIL, password })).toBe('authenticated');

    expect(store.isAuthenticated()).toBe(true);
    expect(store.role()).toBe('Admin');
    expect(store.user()?.name).toBe('Administrador');
    expect(store.can('platform.view')).toBe(true);
    expect(store.home()).toBe('/dashboard');
    expect(navigate).toHaveBeenCalledWith('/dashboard');
    expect(TestBed.inject(AuthTokenStore).token()).toContain('fake-token.');
  });

  it('opens the page the visitor asked for before signing in', async () => {
    const { store, navigate } = setup();
    store.rememberReturnUrl('/customers');

    await store.login({ email: FAKE_ADMIN_EMAIL, password });

    expect(navigate).toHaveBeenCalledWith('/customers');
  });

  it('sends the mechanic straight to the mechanic mode', async () => {
    const { store, navigate } = setup();

    await store.login({ email: 'marcos@oficina.local', password });

    expect(navigate).toHaveBeenCalledWith('/mechanic');
    expect(store.can('customers.view')).toBe(false);
  });

  it('brings the same user back to the remembered page with the drafts kept', async () => {
    const { store, navigate } = setup();
    const drafts = TestBed.inject(DraftStore);
    await store.login({ email: FAKE_ADMIN_EMAIL, password });
    drafts.save('customer-new', { name: 'Rascunho' });

    TestBed.inject(SessionService).expire();
    store.rememberReturnUrl('/customers');
    expect(store.isAuthenticated()).toBe(false);
    await store.login({ email: FAKE_ADMIN_EMAIL, password });

    expect(navigate).toHaveBeenLastCalledWith('/customers');
    expect(drafts.restore('customer-new')).toEqual({ name: 'Rascunho' });
    expect(store.returnUrl()).toBeNull();
  });

  it('drops the remembered page and drafts when another user signs in', async () => {
    const { store, navigate } = setup();
    const drafts = TestBed.inject(DraftStore);
    await store.login({ email: FAKE_ADMIN_EMAIL, password });
    drafts.save('customer-new', { name: 'Rascunho' });

    TestBed.inject(SessionService).expire();
    store.rememberReturnUrl('/team');
    await store.login({ email: 'ana@oficina.local', password });

    expect(navigate).toHaveBeenLastCalledWith('/dashboard');
    expect(drafts.restore('customer-new')).toBeNull();
  });

  it('asks for the second factor and finishes the login with the right code', async () => {
    const { store, navigate } = setup();

    expect(await store.login({ email: FAKE_TWO_FACTOR_EMAIL, password })).toBe('twoFactor');
    expect(store.status()).toBe('twoFactor');
    expect(navigate).toHaveBeenCalledWith('/login/verify');

    await expect(store.verifyTwoFactor('000000')).rejects.toMatchObject({ status: 401 });
    expect(await store.verifyTwoFactor(FAKE_TWO_FACTOR_CODE)).toBe('authenticated');
    expect(store.challengeId()).toBeNull();
  });

  it('goes back to the login when there is no challenge or it is cancelled', async () => {
    const { store, navigate } = setup();

    await store.verifyTwoFactor(FAKE_TWO_FACTOR_CODE);
    expect(navigate).toHaveBeenCalledWith('/login');

    await store.login({ email: FAKE_TWO_FACTOR_EMAIL, password });
    store.cancelTwoFactor();
    expect(store.status()).toBe('anonymous');
  });

  it('clears the token when the user cannot be loaded', async () => {
    const { store } = setup();
    vi.spyOn(TestBed.inject(AuthGateway), 'me').mockReturnValue(
      throwError(() => new Error('down')),
    );

    await expect(store.login({ email: FAKE_ADMIN_EMAIL, password })).rejects.toThrow('down');

    expect(TestBed.inject(AuthTokenStore).token()).toBeNull();
    expect(store.isAuthenticated()).toBe(false);
  });

  it('restores the session from the refresh cookie and fails safe without it', async () => {
    const { store } = setup();
    expect(await store.restore()).toBe(false);

    await store.login({ email: FAKE_ADMIN_EMAIL, password });
    TestBed.inject(SessionService).expire();
    expect(await store.restore()).toBe(true);
    expect(store.user()?.email).toBe(FAKE_ADMIN_EMAIL);

    vi.spyOn(TestBed.inject(AuthGateway), 'me').mockReturnValue(
      throwError(() => new Error('down')),
    );
    expect(await store.restore()).toBe(false);
  });

  it('logs out clearing the user, the drafts and the remembered page', async () => {
    const { store, navigate } = setup();
    await store.login({ email: FAKE_ADMIN_EMAIL, password });
    TestBed.inject(DraftStore).save('form', { a: 1 });
    store.rememberReturnUrl('/team');

    store.logout();

    expect(store.isAuthenticated()).toBe(false);
    expect(store.returnUrl()).toBeNull();
    expect(TestBed.inject(DraftStore).restore('form')).toBeNull();
    expect(TestBed.inject(SessionService).endReason()).toBe('logout');
    expect(navigate).toHaveBeenLastCalledWith('/login');
    expect(await store.restore()).toBe(false);
  });
});
