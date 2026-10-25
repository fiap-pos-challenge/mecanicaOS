import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { API_MODES, useGatewayHarness } from '../../../../testing/gateway';
import { AuthTokenStore } from '../../../core/auth/auth-token.store';
import { SessionService } from '../../../core/auth/session.service';
import { ContractError } from '../../../core/data/contract-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import {
  FAKE_ADMIN_EMAIL,
  FAKE_ADMIN_PASSWORD,
} from '../../../../testing/fake-api/fake-api/fake-database';
import { toAuthSession, toCurrentUser } from './auth.mappers';
import { AuthGateway, HttpAuthGateway } from './auth.gateway';
import { toLoginResult } from './auth.mappers';
import { TwoFactorGateway } from './two-factor.gateway';
import { SessionGateway } from '../../../core/auth/session.gateway';
import {
  FAKE_TWO_FACTOR_CODE,
  FAKE_TWO_FACTOR_EMAIL,
} from '../../../../testing/fake-api/fake-api/fake-auth-state';

describe('auth mappers', () => {
  it('rejects responses without required fields', () => {
    expect(() => toAuthSession({ expiresAt: '2026-10-04T12:00:00Z' })).toThrow(ContractError);
    expect(() => toCurrentUser({ id: '1', name: 'A', email: 'a@b.c' })).toThrow(ContractError);
    expect(() => toLoginResult({ requiresTwoFactor: true })).toThrow(ContractError);
  });

  it('maps a two-factor challenge and a plain session', () => {
    expect(toLoginResult({ requiresTwoFactor: true, challengeId: 'c1' })).toEqual({
      kind: 'twoFactor',
      challengeId: 'c1',
    });
    expect(toLoginResult({ accessToken: 't', expiresAt: '2026-10-04T12:00:00Z' })).toEqual({
      kind: 'session',
      session: { accessToken: 't', expiresAt: '2026-10-04T12:00:00Z' },
    });
  });
});

describe.each(API_MODES)('AuthGateway (%s)', (mode) => {
  const harness = useGatewayHarness();

  it('resolves the adapter from the environment', () => {
    harness.configure(mode);

    expect(TestBed.inject(AuthGateway)).toBeInstanceOf(HttpAuthGateway);
  });

  it('logs in and reads the current user with the token', async () => {
    harness.configure(mode);
    await harness.login();

    const user = await firstValueFrom(TestBed.inject(AuthGateway).me());

    expect(user).toMatchObject({ name: 'Administrador', email: FAKE_ADMIN_EMAIL, role: 'Admin' });
    expect(TestBed.inject(AuthTokenStore).isExpired()).toBe(false);
  });

  it('reports invalid credentials without ending the session flow', async () => {
    harness.configure(mode);

    await expect(
      firstValueFrom(
        TestBed.inject(AuthGateway).login({ email: FAKE_ADMIN_EMAIL, password: 'wrong' }),
      ),
    ).rejects.toMatchObject({
      status: 401,
      code: 'identity.invalidCredentials',
      detail: messages.fake.invalidCredentials,
    });
    expect(TestBed.inject(SessionService).endReason()).toBeNull();
  });

  it('maps missing fields to field errors', async () => {
    harness.configure(mode);

    await expect(
      firstValueFrom(TestBed.inject(AuthGateway).login({ email: '', password: '' })),
    ).rejects.toMatchObject({
      status: 400,
      fieldErrors: [
        { field: 'email', code: 'common.required', message: messages.fake.required },
        { field: 'password', code: 'common.required', message: messages.fake.required },
      ],
    });
  });

  it('expires the session when the token is missing', async () => {
    const { navigate } = harness.configure(mode);

    await expect(firstValueFrom(TestBed.inject(AuthGateway).me())).rejects.toMatchObject({
      status: 401,
    });
    expect(TestBed.inject(SessionService).endReason()).toBe('expired');
    expect(navigate).toHaveBeenCalledWith('/login');
  });

  it('surfaces the too many requests scenario on login', async () => {
    harness.configure(mode);
    harness.scenario(mode, 'auth', 'tooManyRequests');

    await expect(
      firstValueFrom(
        TestBed.inject(AuthGateway).login({
          email: FAKE_ADMIN_EMAIL,
          password: FAKE_ADMIN_PASSWORD,
        }),
      ),
    ).rejects.toMatchObject({ status: 429, code: 'common.tooManyRequests' });
  });

  it('answers 403 accountDisabled only when the password is right', async () => {
    harness.configure(mode);
    const gateway = TestBed.inject(AuthGateway);

    await expect(
      firstValueFrom(gateway.login({ email: 'rita@oficina.local', password: FAKE_ADMIN_PASSWORD })),
    ).rejects.toMatchObject({ status: 403, code: 'identity.accountDisabled' });
    await expect(
      firstValueFrom(gateway.login({ email: 'rita@oficina.local', password: 'wrong' })),
    ).rejects.toMatchObject({ status: 401, code: 'identity.invalidCredentials' });
    await expect(
      firstValueFrom(gateway.login({ email: 'nobody@oficina.local', password: 'wrong' })),
    ).rejects.toMatchObject({ status: 401 });
  });

  it('locks the account after five wrong passwords with the time left', async () => {
    harness.configure(mode);
    const gateway = TestBed.inject(AuthGateway);
    const wrong = { email: 'ana@oficina.local', password: 'wrong' };

    for (let attempt = 1; attempt < 5; attempt++) {
      await expect(firstValueFrom(gateway.login(wrong))).rejects.toMatchObject({ status: 401 });
    }

    await expect(firstValueFrom(gateway.login(wrong))).rejects.toMatchObject({
      status: 429,
      code: 'identity.accountLocked',
      retryAfterSeconds: 900,
    });
    await expect(
      firstValueFrom(gateway.login({ ...wrong, password: FAKE_ADMIN_PASSWORD })),
    ).rejects.toMatchObject({ status: 429, code: 'identity.accountLocked' });
  });

  it('asks for the second factor and accepts only the right code', async () => {
    harness.configure(mode);
    const result = await firstValueFrom(
      TestBed.inject(AuthGateway).login({
        email: FAKE_TWO_FACTOR_EMAIL,
        password: FAKE_ADMIN_PASSWORD,
      }),
    );
    if (result.kind !== 'twoFactor') {
      throw new Error('Expected a two-factor challenge.');
    }

    const twoFactor = TestBed.inject(TwoFactorGateway);
    await expect(
      firstValueFrom(twoFactor.verify(result.challengeId, '000000')),
    ).rejects.toMatchObject({ status: 401, code: 'identity.invalidTwoFactorCode' });
    const session = await firstValueFrom(
      twoFactor.verify(result.challengeId, FAKE_TWO_FACTOR_CODE),
    );

    expect(session.accessToken).toContain('fake-token.');
  });

  it('renews the session from the refresh cookie until logout', async () => {
    harness.configure(mode);
    const sessions = TestBed.inject(SessionGateway);

    expect(await firstValueFrom(sessions.refresh())).toBeNull();
    await harness.login();
    expect((await firstValueFrom(sessions.refresh()))?.accessToken).toContain('fake-token.');
    await firstValueFrom(sessions.logout());
    expect(await firstValueFrom(sessions.refresh())).toBeNull();
  });
});
