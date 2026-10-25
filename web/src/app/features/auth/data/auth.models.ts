import { StoredSession } from '../../../core/auth/auth-token.store';
import { BackendRole } from '../../../core/auth/permissions';

export type Role = BackendRole;

export interface Credentials {
  readonly email: string;
  readonly password: string;
}

export type AuthSession = StoredSession;

export type LoginResult =
  | { readonly kind: 'session'; readonly session: AuthSession }
  | { readonly kind: 'twoFactor'; readonly challengeId: string };

export interface CurrentUser {
  readonly id: string;
  readonly name: string;
  readonly email: string;
  readonly role: Role;
}
