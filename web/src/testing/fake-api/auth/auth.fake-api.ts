import { messages } from '../../../app/core/i18n/messages.pt-BR';
import {
  FAKE_LOCKOUT_MS,
  FAKE_MAX_FAILED_LOGINS,
  FAKE_TWO_FACTOR_CODE,
} from '../fake-api/fake-auth-state';
import { FakeBackendState, FakeFeatureApi } from '../fake-api/fake-backend';
import { FakeInvitationRecord } from '../fake-api/fake-database';
import {
  fieldError,
  FakeRouter,
  noContent,
  ok,
  problem,
  readBody,
  readText,
  validation,
} from '../fake-api/fake-http';
import { fakeId } from '../fake-api/fake-ids';
import {
  authenticate,
  FAKE_ROLES,
  FakeUserRecord,
  issueToken,
  isUser,
} from '../fake-api/fake-session';

export const AUTH_PATHS = {
  login: '/api/auth/login',
  me: '/api/auth/me',
  verifyTwoFactor: '/api/auth/2fa/verify',
  refresh: '/api/auth/refresh',
  logout: '/api/auth/logout',
  inspectInvitation: '/api/auth/invitations/inspect',
  acceptInvitation: '/api/auth/invitations/accept',
} as const;

const STRONG_PASSWORD = /^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[^A-Za-z\d\s]).{8,64}$/;

export function isStrongPassword(password: string): boolean {
  return STRONG_PASSWORD.test(password);
}

export function maskEmail(email: string): string {
  const at = email.indexOf('@');
  return `${email[0] ?? ''}•••${email.slice(at)}`;
}

function locked(retryAfterMs: number): ReturnType<typeof problem> {
  const seconds = Math.ceil(retryAfterMs / 1000);
  return problem(
    429,
    'identity.accountLocked',
    messages.fake.accountLocked,
    [],
    { 'Retry-After': String(seconds) },
    { retryAfterSeconds: seconds },
  );
}

export function createAuthFakeApi(state: FakeBackendState): FakeFeatureApi {
  const users = (): FakeBackendState['database']['users'] => state.database.users;
  const auth = (): FakeBackendState['database']['auth'] => state.database.auth;

  const startSession = (user: FakeUserRecord): ReturnType<typeof ok> => {
    auth().refreshCookie = user.id;
    return ok(issueToken(user.id));
  };

  const findInvitation = (
    token: string,
  ):
    | { readonly invitation: FakeInvitationRecord; readonly user: FakeUserRecord }
    | ReturnType<typeof problem> => {
    const invitation = state.database.invitations.find((candidate) => candidate.token === token);
    const user = users().find((candidate) => candidate.id === invitation?.userId);
    if (!invitation || !user) {
      return problem(404, 'identity.invitationNotFound', messages.fake.invitationNotFound);
    }

    if (invitation.acceptedAt !== null || invitation.revokedAt !== null || user.password !== null) {
      return problem(409, 'identity.invitationAlreadyUsed', messages.fake.invitationAlreadyUsed);
    }

    return Date.parse(invitation.expiresAt) <= Date.now()
      ? problem(422, 'identity.invitationExpired', messages.fake.invitationExpired)
      : { invitation, user };
  };

  const router = new FakeRouter()
    .on('POST', AUTH_PATHS.login, (request) => {
      const body = readBody(request);
      const email = readText(body, 'email').toLowerCase();
      const password = readText(body, 'password');
      const missing = [
        ...(email ? [] : [fieldError('email', 'common.required', messages.fake.required)]),
        ...(password ? [] : [fieldError('password', 'common.required', messages.fake.required)]),
      ];
      if (missing.length > 0) {
        return validation(missing);
      }

      const user = users().find((candidate) => candidate.email === email);
      if (!user || user.password === null) {
        return problem(401, 'identity.invalidCredentials', messages.fake.invalidCredentials);
      }

      const lockoutEnd = auth().lockouts.get(user.id) ?? 0;
      if (lockoutEnd > Date.now()) {
        return locked(lockoutEnd - Date.now());
      }

      if (user.password !== password) {
        const failures = (auth().failures.get(user.id) ?? 0) + 1;
        if (failures >= FAKE_MAX_FAILED_LOGINS) {
          auth().failures.delete(user.id);
          auth().lockouts.set(user.id, Date.now() + FAKE_LOCKOUT_MS);
          return locked(FAKE_LOCKOUT_MS);
        }

        auth().failures.set(user.id, failures);
        return problem(401, 'identity.invalidCredentials', messages.fake.invalidCredentials);
      }

      if (!user.isActive) {
        return problem(403, 'identity.accountDisabled', messages.fake.accountDisabled);
      }

      auth().failures.delete(user.id);
      auth().lockouts.delete(user.id);
      if (auth().twoFactorUsers.has(user.email)) {
        const challengeId = fakeId();
        auth().challenges.set(challengeId, user.id);
        return ok({ requiresTwoFactor: true, challengeId });
      }

      return startSession(user);
    })
    .on('POST', AUTH_PATHS.verifyTwoFactor, (request) => {
      const body = readBody(request);
      const userId = auth().challenges.get(readText(body, 'challengeId'));
      const user = users().find((candidate) => candidate.id === userId && candidate.isActive);
      if (!user || readText(body, 'code') !== FAKE_TWO_FACTOR_CODE) {
        return problem(401, 'identity.invalidTwoFactorCode', messages.fake.invalidTwoFactor);
      }

      auth().challenges.delete(readText(body, 'challengeId'));
      return startSession(user);
    })
    .on('POST', AUTH_PATHS.refresh, () => {
      const user = users().find(
        (candidate) => candidate.id === auth().refreshCookie && candidate.isActive,
      );
      return user
        ? ok(issueToken(user.id))
        : problem(401, 'common.unauthorized', messages.fake.sessionExpired);
    })
    .on('POST', AUTH_PATHS.inspectInvitation, (request) => {
      const found = findInvitation(readText(readBody(request), 'token'));
      if ('status' in found) {
        return found;
      }

      return ok({
        name: found.user.name,
        maskedEmail: maskEmail(found.user.email),
        role: found.user.role,
        expiresAt: found.invitation.expiresAt,
      });
    })
    .on('POST', AUTH_PATHS.acceptInvitation, (request) => {
      const body = readBody(request);
      const password = readText(body, 'password');
      if (!isStrongPassword(password)) {
        return validation([
          fieldError('password', 'identity.weakPassword', messages.fake.weakPassword),
        ]);
      }

      const found = findInvitation(readText(body, 'token'));
      if ('status' in found) {
        return found;
      }

      found.invitation.acceptedAt = new Date().toISOString();
      found.user.password = password;
      return noContent();
    })
    .on('POST', AUTH_PATHS.logout, () => {
      auth().refreshCookie = null;
      return noContent();
    })
    .on('GET', AUTH_PATHS.me, (request) => {
      const user = authenticate(request, users(), FAKE_ROLES);
      return isUser(user)
        ? ok({ id: user.id, name: user.name, email: user.email, role: user.role })
        : user;
    });

  return new FakeFeatureApi('auth', router, state.scenarios);
}
