import { AppRole } from '../../../core/auth/permissions';
import { StatusTone } from '../../../shared/ui/status-pill/status-pill';
import { parseUtc } from '../../../shared/util/date';
import { User, UserStatus } from '../data/user.models';

export const ROLE_TONES: Readonly<Record<AppRole, StatusTone>> = {
  Admin: 'violet',
  Manager: 'ok',
  Attendant: 'neutral',
  Mechanic: 'info',
  Customer: 'neutral',
};

export const STATUS_TONES: Readonly<Record<UserStatus, StatusTone>> = {
  Active: 'ok',
  Inactive: 'bad',
  InvitationPending: 'warn',
};

export type AccessBlock = 'self' | 'lastAdmin';

export function accessBlockOf(
  user: User,
  currentUserId: string | null,
  activeAdmins: number,
): AccessBlock | null {
  if (user.id === currentUserId) {
    return 'self';
  }

  return user.role === 'Admin' && user.status === 'Active' && activeAdmins <= 1
    ? 'lastAdmin'
    : null;
}

export function invitationExpired(user: User, now: number = Date.now()): boolean {
  const expiresAt = user.invitationExpiresAt ? parseUtc(user.invitationExpiresAt) : null;
  return user.status === 'InvitationPending' && (expiresAt === null || expiresAt.getTime() <= now);
}
