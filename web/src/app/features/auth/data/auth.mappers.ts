import type { CurrentUserDto, LoginResponse } from '../../../core/api/generated/index';
import { required } from '../../../core/data/contract-error';
import { LoginResponseWithChallenge } from './auth.contracts';
import { AuthSession, CurrentUser, LoginResult } from './auth.models';

export function toAuthSession(dto: LoginResponse): AuthSession {
  return {
    accessToken: required(dto.accessToken, 'accessToken'),
    expiresAt: required(dto.expiresAt, 'expiresAt'),
  };
}

export function toLoginResult(dto: LoginResponseWithChallenge): LoginResult {
  if (dto.requiresTwoFactor === true) {
    return { kind: 'twoFactor', challengeId: required(dto.challengeId, 'challengeId') };
  }

  return { kind: 'session', session: toAuthSession(dto) };
}

export function toCurrentUser(dto: CurrentUserDto): CurrentUser {
  return {
    id: required(dto.id, 'id'),
    name: required(dto.name, 'name'),
    email: required(dto.email, 'email'),
    role: required(dto.role, 'role'),
  };
}
