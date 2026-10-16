import { messages } from '../../../app/core/i18n/messages.pt-BR';
import { isValidEmail, normalizeEmail } from '../../../app/shared/util/email';
import { FakeBackendState, FakeFeatureApi } from '../fake-api/fake-backend';
import { FAKE_INVITATION_LIFETIME_MS, FakeInvitationRecord } from '../fake-api/fake-database';
import {
  created,
  fieldError,
  FakeRouter,
  noContent,
  ok,
  paginate,
  problem,
  readBody,
  readText,
  validation,
} from '../fake-api/fake-http';
import { fakeId, fakeNow } from '../fake-api/fake-ids';
import {
  authorize,
  FakeRole,
  FakeUserRecord,
  isPrincipal,
  readFakeRole,
} from '../fake-api/fake-session';

export const USER_PATHS = {
  collection: '/api/admin/users',
  item: '/api/admin/users/:id',
  invite: '/api/admin/users/invite',
  resend: '/api/admin/users/:id/invite/resend',
  deactivate: '/api/admin/users/:id/deactivate',
  reactivate: '/api/admin/users/:id/reactivate',
  search: '/api/admin/users/search',
} as const;

export type FakeUserStatus = 'Active' | 'Inactive' | 'InvitationPending';

const STATUSES: readonly FakeUserStatus[] = ['Active', 'Inactive', 'InvitationPending'];
const readers: readonly FakeRole[] = ['Admin', 'Manager'];
const admins: readonly FakeRole[] = ['Admin'];
const text = messages.fake;

export function statusOf(user: FakeUserRecord): FakeUserStatus {
  if (!user.isActive) {
    return 'Inactive';
  }

  return user.password === null ? 'InvitationPending' : 'Active';
}

export function openInvitationOf(
  invitations: readonly FakeInvitationRecord[],
  userId: string,
): FakeInvitationRecord | undefined {
  return invitations.find(
    (invitation) =>
      invitation.userId === userId &&
      invitation.acceptedAt === null &&
      invitation.revokedAt === null,
  );
}

export function issueInvitation(state: FakeBackendState, userId: string): FakeInvitationRecord {
  const now = fakeNow();
  for (const invitation of state.database.invitations) {
    if (invitation.userId === userId && invitation.acceptedAt === null) {
      invitation.revokedAt ??= now;
    }
  }

  const invitation: FakeInvitationRecord = {
    token: `convite-${fakeId()}`,
    userId,
    expiresAt: new Date(Date.now() + FAKE_INVITATION_LIFETIME_MS).toISOString(),
    acceptedAt: null,
    revokedAt: null,
  };
  state.database.invitations.push(invitation);
  return invitation;
}

function notFound(): ReturnType<typeof problem> {
  return problem(404, 'identity.notFound', text.userNotFound);
}

function lastAdmin(): ReturnType<typeof problem> {
  return problem(409, 'identity.lastActiveAdmin', text.lastActiveAdmin);
}

function ownAccount(): ReturnType<typeof problem> {
  return problem(422, 'identity.cannotManageOwnAccount', text.cannotManageOwnAccount);
}

function nameErrors(name: string): ReturnType<typeof fieldError>[] {
  return name.length >= 2 && name.length <= 120
    ? []
    : [fieldError('name', 'identity.invalidName', text.invalidField)];
}

export function createUsersFakeApi(state: FakeBackendState): FakeFeatureApi {
  const users = (): FakeUserRecord[] => state.database.users;
  const find = (id: string): FakeUserRecord | undefined => users().find((user) => user.id === id);
  const isLastActiveAdmin = (user: FakeUserRecord): boolean =>
    user.role === 'Admin' &&
    user.isActive &&
    users().filter((other) => other.role === 'Admin' && other.isActive).length === 1;

  const toDto = (user: FakeUserRecord): Record<string, unknown> => {
    const status = statusOf(user);
    return {
      id: user.id,
      name: user.name,
      email: user.email,
      role: user.role,
      isActive: user.isActive,
      status,
      invitationExpiresAt:
        status === 'InvitationPending'
          ? (openInvitationOf(state.database.invitations, user.id)?.expiresAt ?? null)
          : null,
      createdAt: user.createdAt,
      updatedAt: user.updatedAt,
    };
  };

  const removeAccess = (
    actingId: string,
    user: FakeUserRecord,
  ): ReturnType<typeof problem> | null => {
    if (user.id === actingId) {
      return ownAccount();
    }

    return isLastActiveAdmin(user) ? lastAdmin() : null;
  };

  const router = new FakeRouter()
    .on('POST', USER_PATHS.search, (request) => {
      const admin = authorize(request, users(), admins);
      if (!isPrincipal(admin)) {
        return admin;
      }

      const email = normalizeEmail(readText(readBody(request), 'email'));
      if (!email) {
        return validation([fieldError('email', 'common.required', text.required)]);
      }

      const user = users().find((candidate) => candidate.email === email);
      return user ? ok(toDto(user)) : notFound();
    })
    .on('GET', USER_PATHS.collection, (request) => {
      const reader = authorize(request, users(), readers);
      if (!isPrincipal(reader)) {
        return reader;
      }

      const role = request.query.get('role');
      const status = request.query.get('status');
      if ((role && !readFakeRole(role)) || (status && !STATUSES.some((item) => item === status))) {
        return validation([fieldError('role', 'common.validationFailed', text.invalidField)]);
      }

      const items = users()
        .filter((user) => !role || user.role === role)
        .filter((user) => !status || statusOf(user) === status)
        .sort((left, right) => left.name.localeCompare(right.name, 'pt-BR'))
        .map(toDto);
      return paginate(items, request.query);
    })
    .on('POST', USER_PATHS.invite, (request) => {
      const admin = authorize(request, users(), admins);
      if (!isPrincipal(admin)) {
        return admin;
      }

      const body = readBody(request);
      const name = readText(body, 'name').trim();
      const email = normalizeEmail(readText(body, 'email'));
      const role = readFakeRole(body['role']);
      const errors = [
        ...nameErrors(name),
        ...(isValidEmail(email)
          ? []
          : [fieldError('email', 'common.invalidEmail', text.invalidField)]),
        ...(role ? [] : [fieldError('role', 'identity.invalidRole', text.invalidField)]),
      ];
      if (errors.length > 0 || !role) {
        return validation(errors);
      }

      if (users().some((user) => user.email === email)) {
        return problem(409, 'identity.duplicateEmail', text.emailInUse);
      }

      const user: FakeUserRecord = {
        id: fakeId(),
        name,
        email,
        role,
        password: null,
        isActive: true,
        createdAt: fakeNow(),
        updatedAt: null,
      };
      users().push(user);
      issueInvitation(state, user.id);
      return created(toDto(user));
    })
    .on('POST', USER_PATHS.resend, (request, params) => {
      const admin = authorize(request, users(), admins);
      if (!isPrincipal(admin)) {
        return admin;
      }

      const user = find(params['id'] ?? '');
      if (!user) {
        return notFound();
      }

      if (user.password !== null) {
        return problem(409, 'identity.invitationNotPending', text.invitationNotPending);
      }

      issueInvitation(state, user.id);
      return ok(toDto(user));
    })
    .on('GET', USER_PATHS.item, (request, params) => {
      const reader = authorize(request, users(), readers);
      if (!isPrincipal(reader)) {
        return reader;
      }

      const user = find(params['id'] ?? '');
      return user ? ok(toDto(user)) : notFound();
    })
    .on('PUT', USER_PATHS.item, (request, params) => {
      const admin = authorize(request, users(), admins);
      if (!isPrincipal(admin)) {
        return admin;
      }

      const user = find(params['id'] ?? '');
      if (!user) {
        return notFound();
      }

      const body = readBody(request);
      const name = readText(body, 'name').trim();
      const role = readFakeRole(body['role']);
      const errors = [
        ...nameErrors(name),
        ...(role ? [] : [fieldError('role', 'identity.invalidRole', text.invalidField)]),
      ];
      if (errors.length > 0 || !role) {
        return validation(errors);
      }

      const blocked =
        user.role === 'Admin' && role !== 'Admin' ? removeAccess(admin.id, user) : null;
      if (blocked) {
        return blocked;
      }

      Object.assign(user, { name, role, updatedAt: fakeNow() });
      return ok(toDto(user));
    })
    .on('DELETE', USER_PATHS.item, (request, params) => {
      const admin = authorize(request, users(), admins);
      if (!isPrincipal(admin)) {
        return admin;
      }

      const user = find(params['id'] ?? '');
      if (!user) {
        return notFound();
      }

      const blocked = removeAccess(admin.id, user);
      if (blocked) {
        return blocked;
      }

      users().splice(users().indexOf(user), 1);
      return noContent();
    })
    .on('POST', USER_PATHS.deactivate, (request, params) => {
      const admin = authorize(request, users(), admins);
      if (!isPrincipal(admin)) {
        return admin;
      }

      const user = find(params['id'] ?? '');
      if (!user) {
        return notFound();
      }

      const blocked = removeAccess(admin.id, user);
      if (blocked) {
        return blocked;
      }

      if (!user.isActive) {
        return problem(409, 'identity.alreadyInactive', text.alreadyInactive);
      }

      Object.assign(user, { isActive: false, updatedAt: fakeNow() });
      return noContent();
    })
    .on('POST', USER_PATHS.reactivate, (request, params) => {
      const admin = authorize(request, users(), admins);
      if (!isPrincipal(admin)) {
        return admin;
      }

      const user = find(params['id'] ?? '');
      if (!user) {
        return notFound();
      }

      if (user.isActive) {
        return problem(409, 'identity.alreadyActive', text.alreadyActive);
      }

      Object.assign(user, { isActive: true, updatedAt: fakeNow() });
      return noContent();
    });

  return new FakeFeatureApi('users', router, state.scenarios);
}
