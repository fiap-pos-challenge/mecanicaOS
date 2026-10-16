import { FakeRequest, FakeResult } from '../fake-request';
import { messages } from '../../../app/core/i18n/messages.pt-BR';
import { problem } from './fake-http';

export type FakeRole = 'Admin' | 'Manager' | 'Attendant' | 'Mechanic';

export const FAKE_ROLES: readonly FakeRole[] = ['Admin', 'Manager', 'Attendant', 'Mechanic'];

export function readFakeRole(value: unknown): FakeRole | null {
  return FAKE_ROLES.find((role) => role === value) ?? null;
}

export interface FakeUserRecord {
  id: string;
  name: string;
  email: string;
  role: FakeRole;
  password: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export const FAKE_TOKEN_PREFIX = 'fake-token.';
export const FAKE_TOKEN_LIFETIME_MS = 60 * 60 * 1000;

export function issueToken(
  userId: string,
  now: Date = new Date(),
): { accessToken: string; expiresAt: string } {
  const expiresAt = new Date(now.getTime() + FAKE_TOKEN_LIFETIME_MS).toISOString();
  return { accessToken: `${FAKE_TOKEN_PREFIX}${userId}.${Date.parse(expiresAt)}`, expiresAt };
}

export function authenticate(
  request: FakeRequest,
  users: readonly FakeUserRecord[],
  roles: readonly FakeRole[],
): FakeUserRecord | FakeResult {
  const token = request.authorization?.replace(/^Bearer /, '') ?? '';
  const [, userId = '', expiresAt = '0'] = token.startsWith(FAKE_TOKEN_PREFIX)
    ? token.split('.')
    : [];
  const user = users.find((candidate) => candidate.id === userId && candidate.isActive);
  if (!user || Number(expiresAt) <= Date.now()) {
    return problem(401, 'common.unauthorized', messages.fake.sessionExpired);
  }

  return roles.includes(user.role)
    ? user
    : problem(403, 'common.forbidden', messages.fake.forbidden);
}

export interface FakePrincipal {
  readonly id: string;
  readonly role: FakeRole;
}

function readJwtPrincipal(token: string): FakePrincipal | null {
  const [, payload] = token.split('.');
  if (!payload) {
    return null;
  }

  try {
    const claims = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/'))) as Record<
      string,
      unknown
    >;
    const role = claims['role'];
    const subject = claims['sub'];
    const expiresAt = Number(claims['exp']) * 1000;
    const fakeRole = readFakeRole(role);
    const valid = typeof subject === 'string' && fakeRole !== null && expiresAt > Date.now();
    return valid && fakeRole ? { id: subject, role: fakeRole } : null;
  } catch {
    return null;
  }
}

export function authorize(
  request: FakeRequest,
  users: readonly FakeUserRecord[],
  roles: readonly FakeRole[],
): FakePrincipal | FakeResult {
  const token = request.authorization?.replace(/^Bearer /, '') ?? '';
  if (!token.startsWith(FAKE_TOKEN_PREFIX)) {
    const principal = readJwtPrincipal(token);
    if (!principal) {
      return problem(401, 'common.unauthorized', messages.fake.sessionExpired);
    }

    return roles.includes(principal.role)
      ? principal
      : problem(403, 'common.forbidden', messages.fake.forbidden);
  }

  const user = authenticate(request, users, roles);
  return isUser(user) ? { id: user.id, role: user.role } : user;
}

export function isPrincipal(value: FakePrincipal | FakeResult): value is FakePrincipal {
  return 'role' in value && 'id' in value;
}

export function isUser(value: FakeUserRecord | FakeResult): value is FakeUserRecord {
  return 'role' in value;
}
