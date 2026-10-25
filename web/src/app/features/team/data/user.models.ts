import { PageRequest } from '../../../core/data/page';
import { Role } from '../../auth/data/auth.models';

export type UserStatus = 'Active' | 'Inactive' | 'InvitationPending';

export const USER_STATUSES: readonly UserStatus[] = ['Active', 'Inactive', 'InvitationPending'];

export interface User {
  readonly id: string;
  readonly name: string;
  readonly email: string;
  readonly role: Role;
  readonly isActive: boolean;
  readonly status: UserStatus;
  readonly invitationExpiresAt: string | null;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface UserInvite {
  readonly name: string;
  readonly email: string;
  readonly role: Role;
}

export interface UserUpdate {
  readonly name: string;
  readonly role: Role;
}

export interface UserListQuery extends PageRequest {
  readonly role?: Role | null;
  readonly status?: UserStatus | null;
}
