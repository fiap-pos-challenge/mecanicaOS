import type { LoginResponse } from '../api/generated/index';

export interface RefreshSessionData {
  readonly url: '/api/auth/refresh';
}

export interface LogoutData {
  readonly url: '/api/auth/logout';
}

export type RefreshSessionResponse = LoginResponse;
